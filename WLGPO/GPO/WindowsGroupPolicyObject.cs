using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace WLGPO.GPO;

public class WindowsGroupPolicyObject
{
    private readonly Guid _registryExtensionGuid = new("35378EAC-683F-11D2-A89A-00C04FBBCFA2");
    private readonly Guid _localGuid = new("D2EF96B1-41C1-4E7F-A595-329F9B1FCC3C");
    private readonly GPO_SECTIONS _section;
    private readonly string _subKey;
    private readonly uint _flag = 0x00000000;
    private readonly string _machineName;

    public WindowsGroupPolicyObject(string registryKey, bool openReadOnly = false) 
        : this(string.Empty, registryKey, openReadOnly)
    { }

    public WindowsGroupPolicyObject(string machineName,string registryKey, bool openReadOnly = false)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException();
        
        if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
            throw new UnauthorizedAccessException("administrator privileges are required");

        var assemblyGuid = Assembly.GetExecutingAssembly().GetCustomAttribute<GuidAttribute>()?.Value;
        if (!string.IsNullOrWhiteSpace(assemblyGuid))
            _localGuid = new(assemblyGuid);
        
        var index = registryKey.IndexOf('\\');
        if (index < 0)
            throw new ArgumentException($"invalid registry key: {registryKey}");
        
        var hive = registryKey.Substring(0, registryKey.IndexOf('\\'));
        
        if (hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase) || hive.Equals("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
            _section = GPO_SECTIONS.GPO_SECTION_MACHINE;
        else if (hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase) || hive.Equals("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
            _section = GPO_SECTIONS.GPO_SECTION_USER;
        else
            throw new ArgumentException($"invalid registry key: {registryKey}");
        
        if (registryKey.Length < (index + 1))
            throw new ArgumentException($"invalid registry key: {registryKey}");
        
        _subKey = registryKey.Substring(index + 1);
        
        _flag |= (uint)GPO_OPEN_FLAGS.GPO_OPEN_LOAD_REGISTRY;
        if (openReadOnly) 
            _flag |= (uint)GPO_OPEN_FLAGS.GPO_OPEN_READ_ONLY;

        _machineName = machineName;
        if (Environment.MachineName.Equals(machineName.TrimStart('\\'), StringComparison.OrdinalIgnoreCase))
            _machineName = string.Empty;
    }

    public bool TryGetPathToSection(out string path, out Exception? exception)
    {
        exception = null;
        path = string.Empty;
        
        try
        {
            path = RunOnStaThread(gpo =>
            {
                const int maxLength = 1024;
                var builder = new StringBuilder(maxLength);
                
                if (gpo.Instance.GetFileSysPath((uint)_section, builder, maxLength) != 0) 
                    throw new Exception($"unable to retrieve path to section '{_section}'");

                return builder.ToString();
            });
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        return false;
    }

    public bool TrySetNotConfigured(string valueName, out Exception? exception)
    {
        exception = null;
        
        try
        {
            Set(valueName, null, RegistryValueKind.None, true);
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }

        return true;
    }
    
    public bool TrySet(string valueName, object? data, RegistryValueKind dataType, out Exception? exception)
    { 
        exception = null;
        
        try
        {
            Set(valueName, data, dataType);
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }

        return true;
    }
    
    public bool TryGet(string field, out object? value, out Exception? exception)
    {
        exception = null;
        value = null;

        try
        {
            value = RunOnStaThread(gpo =>
            {
                if (gpo.Instance.GetRegistryKey((uint)_section, out var pointer) != 0)
                    throw new Exception($"unable to get registry section: {_section}");

                using var handle = new SafeRegistryHandle(pointer, true);
                using var root = RegistryKey.FromHandle(handle);
                using var key = root.CreateSubKey(_subKey);
                
                return key?.GetValue(field);
            });
            return true;
        }
        catch (Exception e)
        {
            exception = e;
        }
        
        return false;
    }
    
    private void Set(string valueName, object? data, RegistryValueKind dataType, bool setNotConfigured = false)
    {
        if (string.IsNullOrWhiteSpace(valueName))
            throw new Exception("name cannot be null or empty");

        RunOnStaThread(gpo =>
        {
            if (gpo.Instance.GetRegistryKey((uint)_section, out var pointer) != 0)
                throw new Exception($"unable to get registry section: {_section}");
                
            using var handle = new SafeRegistryHandle(pointer, true);
            using var root = RegistryKey.FromHandle(handle);
            using var key = root.CreateSubKey(_subKey);
                
            var add = !setNotConfigured;

            if (data == null || !add) //delete the key if the value is null or 'setNotConfigured' is true
                key?.DeleteValue(valueName,false);
            else
                key?.SetValue(valueName, data, dataType);
                    
            /*
                HKLM\Software\Policies\... → machine policy → bMachine: true
                HKCU\Software\Policies\... → user policy → bMachine: false
                add → true for 'not configured'
            */
            var machineLevel = (_section == GPO_SECTIONS.GPO_SECTION_MACHINE);
            var hResult = gpo.Instance.Save(machineLevel,add,_registryExtensionGuid,_localGuid);
            if (hResult != HRESULT.S_OK)
                throw new Exception($"unable to save GPO. {hResult}");
        });
    }
    
    private void RunOnStaThread(Action<GroupPolicyObject> action)
    {
        RunOnStaThread<object?>(gpo => { action(gpo); return null; });
    }
    
    private T RunOnStaThread<T>(Func<GroupPolicyObject, T> action)
    {
        T result = default!;
        ExceptionDispatchInfo? capturedException = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var gpo = new GroupPolicyObject();
                
                HRESULT hResult;
                if (string.IsNullOrWhiteSpace(_machineName))
                    hResult = gpo.Instance.OpenLocalMachineGPO(_flag);
                else
                    hResult = gpo.Instance.OpenRemoteMachineGPO(_machineName,_flag);
            
                if (hResult != HRESULT.S_OK)
                    throw new Exception($"unable to open GPO {hResult}");
                
                result = action(gpo);
            }
            catch (Exception e)
            {
                capturedException = ExceptionDispatchInfo.Capture(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        capturedException?.Throw();
        return result;
    }
    
}
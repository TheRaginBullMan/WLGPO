using System;
using System.Runtime.InteropServices;

namespace WLGPO.GPO;

public class GroupPolicyObjectManaged : IDisposable
{
    private IGroupPolicyObject2? _instance;
    private bool _disposed;

    public GroupPolicyObjectManaged()
    {
        //ReSharper disable once SuspiciousTypeConversion.Global
        _instance = (IGroupPolicyObject2) new GroupPolicyObject();
    }
    
    public IGroupPolicyObject2 Instance {
        get
        {   
            if (_disposed || _instance == null)
                throw new ObjectDisposedException("IGroupPolicyObject");
            
            return _instance;
        }
    }
    
    public void Dispose()
    {
        if (!_disposed && _instance != null)
        {
            try
            {
                Marshal.ReleaseComObject(_instance);
            }
            catch (Exception) { /* not a valid COM or has already released. safe to ignore */ }
            finally { _instance = null; }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
    
    ~GroupPolicyObjectManaged()
    {
        Dispose();
    }
}
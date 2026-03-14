using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace WLGPO.GPO;

public readonly struct LocalPrincipalSid
{
    public string Value { get; }

    private LocalPrincipalSid(string value) => Value = value;

    // BUILTIN aliases — fixed on every Windows machine
    public static readonly LocalPrincipalSid Administrators        = new("S-1-5-32-544");
    public static readonly LocalPrincipalSid Users                 = new("S-1-5-32-545");
    public static readonly LocalPrincipalSid Guests                = new("S-1-5-32-546");
    public static readonly LocalPrincipalSid PowerUsers            = new("S-1-5-32-547");
    public static readonly LocalPrincipalSid BackupOperators       = new("S-1-5-32-551");
    public static readonly LocalPrincipalSid RemoteDesktopUsers    = new("S-1-5-32-555");
    public static readonly LocalPrincipalSid NetworkConfigOps      = new("S-1-5-32-556");
    public static readonly LocalPrincipalSid HyperVAdministrators  = new("S-1-5-32-578");
    public static readonly LocalPrincipalSid RemoteManagementUsers = new("S-1-5-32-580");
    
    // Virtual / service accounts — fixed on every Windows machine
    public static readonly LocalPrincipalSid System                = new("S-1-5-18");
    public static readonly LocalPrincipalSid LocalService          = new("S-1-5-19");
    public static readonly LocalPrincipalSid NetworkService        = new("S-1-5-20");
    public static readonly LocalPrincipalSid Iusr                  = new("S-1-5-17");
    
    // Everyone (universal well-known — not a local principal, included for completeness)
    public static readonly LocalPrincipalSid Everyone              = new("S-1-1-0");

    // Machine-relative SIDs are the one escape hatch — runtime only
    public static LocalPrincipalSid FromMachineRelative(WellKnownSidType sidType)
    {
        var machineSid = new SecurityIdentifier(
            WellKnownSidType.AccountAdministratorSid, null).AccountDomainSid;
        
        if (machineSid == null)                                                                                                                                                                                                                                                                                                                                                                                         
            throw new InvalidOperationException(
                "cannot resolve a machine-relative SID on a workgroup machine. " +                                                                                                                                                                                                                                                                                                                                      
                "use a well-known SID constant (e.g. LocalPrincipalSid.Administrators) instead.");  
        
        return new(new SecurityIdentifier(sidType, machineSid).ToString());
    }

    private static readonly Dictionary<string, LocalPrincipalSid> _namedSids =                                                                                                                                                                                                                                                                                                    
      new(StringComparer.OrdinalIgnoreCase)                                                                                                                                                                                                                                                                                                                                     
      {                                                                                                                                                                                                                                                                                                                                                                         
          ["Administrators"]        = Administrators,                                                                                                                                                                                                                                                                                                                           
          ["Users"]                 = Users,                                                                                                                                                                                                                                                                                                                                    
          ["Guests"]                = Guests,                                                                                                                                                                                                                                                                                                                                   
          ["PowerUsers"]            = PowerUsers,                                                                                                                                                                                                                                                                                                                               
          ["BackupOperators"]       = BackupOperators,
          ["RemoteDesktopUsers"]    = RemoteDesktopUsers,                                                                                                                                                                                                                                                                                                                       
          ["NetworkConfigOps"]      = NetworkConfigOps,                                                                                                                                                                                                                                                                                                                         
          ["HyperVAdministrators"]  = HyperVAdministrators,
          ["RemoteManagementUsers"] = RemoteManagementUsers,                                                                                                                                                                                                                                                                                                                    
          ["System"]                = System,
          ["LocalService"]          = LocalService,                                                                                                                                                                                                                                                                                                                             
          ["NetworkService"]        = NetworkService,
          ["Iusr"]                  = Iusr,                                                                                                                                                                                                                                                                                                                                     
          ["Everyone"]              = Everyone,                                                                                                                                                                                                                                                                                                                                 
      };         
    
    public static bool TryParse(string input, out LocalPrincipalSid result)
    { 
        result = default;                                                                                                                                                                                                                                                                                                                                                         
   
        if (string.IsNullOrWhiteSpace(input)) 
            return false;

        // Match by friendly name first
        if (_namedSids.TryGetValue(input.Trim(), out result))
            return true;                                                                                                                                                                                                                                                                                                                                                          
   
        // Fall back to raw SID string validation                                                                                                                                                                                                                                                                                                                                 
        try         
        { 
            var sid = new SecurityIdentifier(input.Trim());
            result = new(sid.Value);                                                                                                                                                                                                                                                                                                                                              
            return true;                                                                                                                                                                                                                                                                                                                                                          
        }                                                                                                                                                                                                                                                                                                                                                                         
        catch (Exception)                                                                                                                                                                                                                                                                                                                                                 
        { 
            return false;
        }                                                                                                                                                                                                                                                                                                                                                                         
    }
    
    public override string ToString() => Value;
}
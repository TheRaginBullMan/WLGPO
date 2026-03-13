namespace WLGPO.GPO;

public enum GROUP_POLICY_OBJECT_TYPE : uint
{
    GPOTypeLocal = 0,       // Default GPO on the local machine
    GPOTypeRemote = 1,      // GPO on a remote machine
    GPOTypeDS = 2,          // GPO in the Active Directory
    GPOTypeLocalUser = 3,   // User-specific GPO on the local machine 
    GPOTypeLocalGroup = 4   // Group-specific GPO on the local machine 
}

using System;
using System.Runtime.InteropServices;

namespace WLGPO.GPO;

public class GroupPolicyObject : IDisposable
{
    private IGroupPolicyObject? _instance;
    private bool _disposed;

    public GroupPolicyObject()
    {
        //ReSharper disable once SuspiciousTypeConversion.Global
        _instance = (IGroupPolicyObject) new GPOClass();
    }
    
    public IGroupPolicyObject Instance {
        get
        {   
            if (_disposed || _instance == null)
                throw new ObjectDisposedException("IGroupPolicyObject");
            
            return _instance;
        }
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (!_disposed)
        {
            _disposed = true;
            if (_instance != null)
            {
                Marshal.ReleaseComObject(_instance);
                _instance = null;
            }
        }
    }
    
    ~GroupPolicyObject()
    {
        Dispose();
    }
}
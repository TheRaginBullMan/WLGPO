using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using WLGPO.GPO;

namespace WLGPO;

class Program
{
    static int Main(string[] args)
        => (int)MainRoutine(args);
    
    static ReturnCodes MainRoutine(string[] args)
    {
        Console.Title = "WLGPO";
        
        try
        { 
            var options = new CmdLineOptions(args);
        
            if (options.HasError)
            {
                StandardErrorWriteLine(options.Error);
                StandardErrorWriteLine(options.Help);
                return ReturnCodes.Error;
            }
            
            if (options.Verbose)
                StandardOutWriteLine($"{options}");
            
            WindowsGroupPolicyObject gpo;
            if (!string.IsNullOrWhiteSpace(options.Sid.Value))
                gpo = new WindowsGroupPolicyObject(options.RegistryKey,options.Sid);
            else
                gpo = new WindowsGroupPolicyObject(options.RegistryKey);
            
            Exception? gpoError;
            switch (options.Action)
            {
                case ActionTask.Get:
                    if (!gpo.TryGet(options.ValueName, out var dataGet, out gpoError))
                        throw new Exception(gpoError?.Message ?? "unknown error");
                    StandardOutWriteLine($"{dataGet}");
                    break;
                case ActionTask.Set:
                    if (!gpo.TryGet(options.ValueName, out var dataSet, out gpoError))
                        throw new Exception(gpoError?.Message ?? "unknown error");
                    
                    Func<object, object?, bool> areEqual = (a, b) =>                                                                                                                                                                                                                                                                                                                              
                        a is byte[] bytesA && b is byte[] bytesB ? bytesA.SequenceEqual(bytesB) : a.Equals(b); 
                    
                    if (dataSet != null && areEqual(dataSet,options.ConvertedData))
                    {   
                        StandardOutWriteLine("group policy already set to value");
                        return ReturnCodes.AlreadySet;
                    }

                    if (!gpo.TrySet(options.ValueName, options.ConvertedData, options.DataType, out gpoError))
                    {
                        StandardErrorWriteLine($"group policy set failed.  reason:{gpoError?.Message ?? "unknown error"}");
                        return ReturnCodes.Failed;
                    }
                    
                    StandardOutWriteLine("group policy set successful");
                    break;
                case ActionTask.Delete:
                    if (!gpo.TrySetNotConfigured(options.ValueName, out gpoError))
                    {
                        StandardErrorWriteLine($"group policy set failed.  reason:{gpoError?.Message ?? "unknown error"}");
                        return ReturnCodes.Failed;
                    }
                    
                    StandardOutWriteLine("group policy set successful");
                    break;
                case ActionTask.Path:
                    if (!gpo.TryGetPathToSection(out var path, out gpoError))
                        throw new Exception(gpoError?.Message ?? "unknown error");
                    
                    StandardOutWriteLine(path);
                    break;
            }
            
        }
        catch (Exception ex)
        {
            StandardErrorWriteLine(ex.Message);
            return ReturnCodes.Error;
        }
        
        return ReturnCodes.Success;
    }
    
    private static void StandardOutWriteLine(string text)
        => Console.Out.WriteLine(text);
    
    private static void StandardErrorWriteLine(string text)
        => Console.Error.WriteLine(text);
    
}

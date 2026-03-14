using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using WLGPO.GPO;

namespace WLGPO;

class Program
{
    static int Main(string[] args)
    {
        Console.Title = "WLGPO";
        
        try
        { 
            var options = new CmdLineOptions(args);
            if (options.HasError)
            {
                ConsoleWriteLine(options.Error);
                ConsoleWriteLine(options.Help);
                return 1;
            }
            
            ConsoleWriteLine($"{options}");

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
                    ConsoleWriteLine($"{dataGet}");
                    break;
                case ActionTask.Set:
                    if (!gpo.TryGet(options.ValueName, out var dataSet, out gpoError))
                        throw new Exception(gpoError?.Message ?? "unknown error");
                    
                    Func<object, object?, bool> areEqual = (a, b) =>                                                                                                                                                                                                                                                                                                                              
                        a is byte[] bytesA && b is byte[] bytesB ? bytesA.SequenceEqual(bytesB) : a.Equals(b); 
                    
                    if (dataSet != null && areEqual(dataSet,options.ConvertedData))
                    {   
                        ConsoleWriteLine("GPO value already set to value", ConsoleColor.Green);
                        return 3;
                    }

                    if (!gpo.TrySet(options.ValueName, options.ConvertedData, options.DataType, out gpoError))
                    {
                        ConsoleWriteLine($"GPO set options failed.  Error:{gpoError?.Message ?? "unknown error"}", ConsoleColor.Red);
                        return 2;
                    }
                    
                    ConsoleWriteLine("GPO value set successful", ConsoleColor.Green);
                    break;
                case ActionTask.Delete:
                    if (!gpo.TrySetNotConfigured(options.ValueName, out gpoError))
                    {
                        ConsoleWriteLine($"GPO set options failed.  Error:{gpoError?.Message ?? "unknown error"}", ConsoleColor.Red);
                        return 2;
                    }
                    
                    ConsoleWriteLine("GPO value set successful", ConsoleColor.Green);
                    break;
                case ActionTask.Path:
                    if (!gpo.TryGetPathToSection(out var path, out gpoError))
                        throw new Exception(gpoError?.Message ?? "unknown error");
                    
                    ConsoleWriteLine(path);
                    break;
            }
            
            if (options.Pause)
            {    
                ConsoleWriteLine("Press Any Key To Continue"); 
                Console.ReadKey();
            }
            
        }
        catch (Exception ex)
        {
            ConsoleWriteLine(ex.Message,  ConsoleColor.Red);
            return 1;
        }

        return 0;
    }
    
    private static void ConsoleWriteLine(string sText)
    {
        ConsoleWriteLine(sText, Console.ForegroundColor);
    }

    private static void ConsoleWriteLine(string sText, ConsoleColor oForeground)
    {
        Console.ForegroundColor = oForeground;
        Console.WriteLine(sText);
        Console.ResetColor();
    }
}
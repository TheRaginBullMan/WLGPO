using System;
using System.Linq;
using Microsoft.Win32;
using WLGPO.GPO;

namespace WLGPO;

public class CmdLineOptions
{
    const string _help = """

                         Usage: (Set,Get,Delete,Path)
                         
                           WLGPO Set <RegistryKey> [/v ValueName] [/d Data] [/t DataType]
                           WLGPO Set <RegistryKey>!<ValueName> [/d Data] [/t DataType]
                           
                           WLGPO Get <RegistryKey> [/v ValueName]
                           WLGPO Get <RegistryKey>!<ValueName>

                           WLGPO Delete <RegistryKey> [/v ValueName]
                           WLGPO Delete <RegistryKey>!<ValueName>
                         
                           WLGPO Path <RegistryKey>
                         
                         Options:
                           /v    Value name
                           /d    Data to set
                           /t    Registry data type
                           /sid  LocalPrincipalSid name
                           /p    Pause before exit
                           --help Display this help message

                         LocalPrincipalSid:
                           Administrators, Users, Guests, PowerUsers, BackupOperators, RemoteDesktopUsers,
                           NetworkConfigOps, HyperVAdministrators, RemoteManagementUsers, System,
                           LocalService, NetworkService, Iusr, Everyone
                           
                         Data Types:
                           REG_SZ, REG_MULTI_SZ, REG_EXPAND_SZ, REG_DWORD,
                           REG_QWORD, REG_BINARY, REG_NONE

                         Notes:
                           REG_MULTI_SZ values are \0 separated
                           REG_BINARY values are comma separated

                         Return Codes:
                           0  Success
                           1  Error
                           2  Set Failed
                           3  Value Already Set
                         """;
    
    public ActionTask Action { get; } =  ActionTask.Unknown;
    public string RegistryKey { get; } = string.Empty;
    public string ValueName { get; } = string.Empty;
    public string Data { get; } = string.Empty;
    public LocalPrincipalSid Sid { get; }
    public object? ConvertedData { get; } = null;
    public RegistryValueKind DataType { get; } = RegistryValueKind.Unknown;
    public bool Pause { get; } = false;
    public string Error { get; } = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public string Help => _help;
    
    public CmdLineOptions(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "/?")
                throw new ArgumentException("available options");
            
            if (args.Length < 2)
                throw new ArgumentException("insufficient arguments");

            switch (args[0].ToUpperInvariant())
            {
                case "GET":
                    Action = ActionTask.Get;
                    break;
                case "SET":
                    Action = ActionTask.Set;
                    break;
                case "DELETE":
                    Action = ActionTask.Delete;
                    break;
                case "PATH":
                    Action = ActionTask.Path;
                    break;
                default:
                    Action = ActionTask.Unknown;
                    break;
            }

            if (Action == ActionTask.Unknown)
                throw new ArgumentException("invalid action. (GET,SET,DELETE,PATH)");

            RegistryKey = args[1];

            if (string.IsNullOrWhiteSpace(RegistryKey))
                throw new ArgumentException("RegistryKey is required.");
            
            if (Action == ActionTask.Path)
                return;
            
            var bangIndex = RegistryKey.IndexOf('!');
            if (bangIndex >= 0)
            {
                ValueName = RegistryKey.Substring(bangIndex + 1);
                RegistryKey = RegistryKey.Substring(0, bangIndex);
            }

            for (var i = 2; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "/v":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("/v requires a value");
                        ValueName = args[++i];
                        break;
                    case "/d":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("/d requires a value");
                        Data = args[++i];
                        break;
                    case "/t":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("/t requires a value");
                        DataType = ParseDataType(args[++i]);
                        break;
                    case "/p":
                        Pause = true;
                        break;
                    case "/sid":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("/sid requires a value");
                        if (!LocalPrincipalSid.TryParse(args[++i],out var sid))
                            throw new ArgumentException("invalid SID value");
                        Sid = sid;
                        break;
                    default:
                        throw new ArgumentException($"Unknown switch: {args[i]}");
                }
            }
            
            if (string.IsNullOrWhiteSpace(ValueName))
                throw new ArgumentException("ValueName is required.");

            if (Action == ActionTask.Get || Action == ActionTask.Delete)
                return;
        
            if (Action == ActionTask.Set && string.IsNullOrWhiteSpace(Data))
                throw new ArgumentException("Data (/d) is required for Set.");

            switch (DataType)
            {
                case RegistryValueKind.DWord:
                    if (!UInt32.TryParse(Data, out var dWord))
                        throw new ArgumentException($"{Data} not a valid DWord");
                    ConvertedData = (int)dWord;
                    break;
                case RegistryValueKind.QWord:
                    if (!UInt64.TryParse(Data, out var qWord))
                        throw new ArgumentException($"{Data} not a valid QWord");
                    ConvertedData = (long)qWord;
                    break;
                case RegistryValueKind.Binary:
                    try
                    {
                        ConvertedData = Data.Split(',').Select(s =>
                        {
                            var token = s.Trim();                                                                                                                                                                                                                                                                                                                                                                               
                            return token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)                                                                                                                                                                                                                                                                                                                                   
                                ? Convert.ToByte(token.Substring(2), 16)                                                                                                                                                                                                                                                                                                                                                        
                                : Convert.ToByte(token);       
                        }).ToArray();
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException($"{Data} not a valid Binary");
                    }
                    break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.Unknown:
                case RegistryValueKind.None:
                    ConvertedData = Data;
                    break;
                case RegistryValueKind.MultiString:
                    ConvertedData = Data.Split(["\\0"], StringSplitOptions.None);
                    break;
                default:
                    throw new ArgumentException($"Unknown data type: {DataType}");
            }
            
        }
        catch (Exception e)
        {
            Error = e.Message;
        }
    }
    
    private static RegistryValueKind ParseDataType(string sType)
    {
        switch (sType.ToUpperInvariant())
        {
            case "REG_SZ": return RegistryValueKind.String;
            case "REG_MULTI_SZ": return RegistryValueKind.MultiString;
            case "REG_EXPAND_SZ": return RegistryValueKind.ExpandString;
            case "REG_DWORD": return RegistryValueKind.DWord;
            case "REG_QWORD": return RegistryValueKind.QWord;
            case "REG_BINARY": return RegistryValueKind.Binary;
            case "REG_NONE": return RegistryValueKind.None;
            default: return RegistryValueKind.Unknown;
        }
    }

    public override string ToString()
    {
        if (Action == ActionTask.Path)
            return $"Action:{Action} RegistryKey:{RegistryKey}";
            
        if (Action == ActionTask.Get || Action == ActionTask.Delete)
            return $"Action:{Action} RegistryKey:{RegistryKey} ValueName:{ValueName}";
        
        return $"Action:{Action} RegistryKey:{RegistryKey} ValueName:{ValueName} Data:{Data} DataType:{DataType}";
    }
} 
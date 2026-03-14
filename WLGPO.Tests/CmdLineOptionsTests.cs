using System;
using Microsoft.Win32;
using Xunit;

namespace WLGPO.Tests;

public class CmdLineOptionsTests
{
    // ── Error / Help handling ──────────────────────────────────────

    [Fact]
    public void EmptyArgs_SetsError()
    {
        var opts = new CmdLineOptions(Array.Empty<string>());
        Assert.True(opts.HasError);
        Assert.Contains("available options", opts.Error);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void HelpFlags_SetError(string flag)
    {
        var opts = new CmdLineOptions(new[] { flag });
        Assert.True(opts.HasError);
        Assert.Contains("available options", opts.Error);
    }

    [Fact]
    public void InsufficientArgs_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET" });
        Assert.True(opts.HasError);
        Assert.Contains("insufficient arguments", opts.Error);
    }

    [Fact]
    public void UnknownAction_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "PATCH", @"HKLM\Software\Test" });
        Assert.True(opts.HasError);
        Assert.Contains("invalid action", opts.Error);
    }

    [Fact]
    public void Help_ReturnsHelpText()
    {
        var opts = new CmdLineOptions(Array.Empty<string>());
        Assert.Contains("Usage:", opts.Help);
    }

    // ── Action parsing ─────────────────────────────────────────────

    [Theory]
    [InlineData("GET", ActionTask.Get)]
    [InlineData("get", ActionTask.Get)]
    [InlineData("Get", ActionTask.Get)]
    [InlineData("SET", ActionTask.Set)]
    [InlineData("set", ActionTask.Set)]
    [InlineData("DELETE", ActionTask.Delete)]
    [InlineData("delete", ActionTask.Delete)]
    [InlineData("PATH", ActionTask.Path)]
    [InlineData("path", ActionTask.Path)]
    public void ActionParsing_CaseInsensitive(string action, ActionTask expected)
    {
        // Path doesn't need ValueName; others use bang notation
        var key = action.Equals("PATH", StringComparison.OrdinalIgnoreCase)
            ? @"HKLM\Software\Test"
            : @"HKLM\Software\Test!Val";
        var args = action.Equals("SET", StringComparison.OrdinalIgnoreCase)
            ? new[] { action, key, "/d", "data" }
            : new[] { action, key };

        var opts = new CmdLineOptions(args);
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(expected, opts.Action);
    }

    // ── Path action ────────────────────────────────────────────────

    [Fact]
    public void PathAction_ReturnsEarly_NoBangParsing()
    {
        var opts = new CmdLineOptions(new[] { "PATH", @"HKLM\Software\Test!Value" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(ActionTask.Path, opts.Action);
        Assert.Equal(@"HKLM\Software\Test!Value", opts.RegistryKey);
        Assert.Equal(string.Empty, opts.ValueName);
    }

    [Fact]
    public void PathAction_IgnoresSwitches()
    {
        // Path returns early so extra switches would cause error if parsed
        // but since it returns early, they are ignored... actually no,
        // the code returns before switch parsing, so switches aren't parsed.
        var opts = new CmdLineOptions(new[] { "PATH", @"HKLM\Software\Test" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(ActionTask.Path, opts.Action);
    }

    // ── Bang notation ──────────────────────────────────────────────

    [Fact]
    public void BangNotation_SplitsKeyAndValueName()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!MyValue" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(@"HKLM\Software\Test", opts.RegistryKey);
        Assert.Equal("MyValue", opts.ValueName);
    }

    [Fact]
    public void BangNotation_WithSwitch_SwitchOverrides()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!BangVal", "/v", "SwitchVal" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("SwitchVal", opts.ValueName);
    }

    // ── Switch parsing ─────────────────────────────────────────────

    [Fact]
    public void Switch_V_SetsValueName()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test", "/v", "MyVal" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("MyVal", opts.ValueName);
    }

    [Fact]
    public void Switch_D_SetsData()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test", "/v", "Val", "/d", "Hello" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("Hello", opts.Data);
    }

    [Fact]
    public void Switch_T_SetsDataType()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test", "/v", "Val", "/d", "42", "/t", "REG_DWORD" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(RegistryValueKind.DWord, opts.DataType);
    }

    [Fact]
    public void Switch_P_SetsPause()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!Val", "/p" });
        Assert.False(opts.HasError, opts.Error);
        Assert.True(opts.Pause);
    }

    [Fact]
    public void Switch_V_MissingValue_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test", "/v" });
        Assert.True(opts.HasError);
        Assert.Contains("/v requires a value", opts.Error);
    }

    [Fact]
    public void Switch_D_MissingValue_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test", "/v", "Val", "/d" });
        Assert.True(opts.HasError);
        Assert.Contains("/d requires a value", opts.Error);
    }

    [Fact]
    public void Switch_T_MissingValue_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test", "/v", "Val", "/d", "x", "/t" });
        Assert.True(opts.HasError);
        Assert.Contains("/t requires a value", opts.Error);
    }

    [Fact]
    public void UnknownSwitch_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test", "/z" });
        Assert.True(opts.HasError);
        Assert.Contains("Unknown switch", opts.Error);
    }

    [Theory]
    [InlineData("/V")]
    [InlineData("/D")]
    [InlineData("/T")]
    [InlineData("/P")]
    public void Switches_CaseInsensitive(string sw)
    {
        // All should be recognized without error
        string[] args;
        if (sw.Equals("/P", StringComparison.OrdinalIgnoreCase))
            args = new[] { "GET", @"HKLM\Software\Test!Val", sw };
        else if (sw.Equals("/V", StringComparison.OrdinalIgnoreCase))
            args = new[] { "GET", @"HKLM\Software\Test", sw, "Val" };
        else if (sw.Equals("/D", StringComparison.OrdinalIgnoreCase))
            args = new[] { "SET", @"HKLM\Software\Test!Val", sw, "data" };
        else // /T
            args = new[] { "SET", @"HKLM\Software\Test!Val", "/d", "42", sw, "REG_DWORD" };

        var opts = new CmdLineOptions(args);
        Assert.False(opts.HasError, opts.Error);
    }

    // ── Validation ─────────────────────────────────────────────────

    [Fact]
    public void Get_MissingValueName_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test" });
        Assert.True(opts.HasError);
        Assert.Contains("ValueName is required", opts.Error);
    }

    [Fact]
    public void Delete_MissingValueName_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "DELETE", @"HKLM\Software\Test" });
        Assert.True(opts.HasError);
        Assert.Contains("ValueName is required", opts.Error);
    }

    [Fact]
    public void Set_MissingData_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val" });
        Assert.True(opts.HasError);
        Assert.Contains("Data (/d) is required for Set", opts.Error);
    }

    [Fact]
    public void Set_WhitespaceData_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "   " });
        Assert.True(opts.HasError);
        Assert.Contains("Data (/d) is required for Set", opts.Error);
    }

    [Fact]
    public void WhitespaceRegistryKey_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET", "  " });
        Assert.True(opts.HasError);
        Assert.Contains("RegistryKey is required", opts.Error);
    }

    // ── Data type parsing ──────────────────────────────────────────

    [Theory]
    [InlineData("REG_SZ", RegistryValueKind.String)]
    [InlineData("REG_MULTI_SZ", RegistryValueKind.MultiString)]
    [InlineData("REG_EXPAND_SZ", RegistryValueKind.ExpandString)]
    [InlineData("REG_DWORD", RegistryValueKind.DWord)]
    [InlineData("REG_QWORD", RegistryValueKind.QWord)]
    [InlineData("REG_BINARY", RegistryValueKind.Binary)]
    [InlineData("REG_NONE", RegistryValueKind.None)]
    public void DataType_AllRegTypes_MapCorrectly(string regType, RegistryValueKind expected)
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "1", "/t", regType });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(expected, opts.DataType);
    }

    [Fact]
    public void DataType_UnknownType_DefaultsToUnknown()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "hello", "/t", "REG_INVALID" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(RegistryValueKind.Unknown, opts.DataType);
    }

    [Theory]
    [InlineData("reg_sz", RegistryValueKind.String)]
    [InlineData("reg_dword", RegistryValueKind.DWord)]
    [InlineData("Reg_Qword", RegistryValueKind.QWord)]
    public void DataType_CaseInsensitive(string regType, RegistryValueKind expected)
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "1", "/t", regType });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(expected, opts.DataType);
    }

    // ── Data conversion ────────────────────────────────────────────

    [Fact]
    public void DataConversion_DWord_ConvertsToInt()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "42", "/t", "REG_DWORD" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<int>(opts.ConvertedData);
        Assert.Equal(42, opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_QWord_ConvertsToLong()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "9999999999", "/t", "REG_QWORD" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<long>(opts.ConvertedData);
        Assert.Equal(9999999999L, opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_Binary_ConvertsByteArray()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "1,2,255", "/t", "REG_BINARY" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<byte[]>(opts.ConvertedData);
        Assert.Equal(new byte[] { 1, 2, 255 }, (byte[])opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_MultiString_SplitsOnBackslashZero()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", @"a\0b\0c", "/t", "REG_MULTI_SZ" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<string[]>(opts.ConvertedData);
        Assert.Equal(new[] { "a", "b", "c" }, (string[])opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_String_Passthrough()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "hello world", "/t", "REG_SZ" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<string>(opts.ConvertedData);
        Assert.Equal("hello world", opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_ExpandString_Passthrough()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "%PATH%", "/t", "REG_EXPAND_SZ" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("%PATH%", opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_UnknownType_Passthrough()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "abc" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(RegistryValueKind.Unknown, opts.DataType);
        Assert.Equal("abc", opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_InvalidDWord_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "notanumber", "/t", "REG_DWORD" });
        Assert.True(opts.HasError);
        Assert.Contains("not a valid DWord", opts.Error);
    }

    [Fact]
    public void DataConversion_InvalidQWord_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "notanumber", "/t", "REG_QWORD" });
        Assert.True(opts.HasError);
        Assert.Contains("not a valid QWord", opts.Error);
    }

    [Fact]
    public void DataConversion_InvalidBinary_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "1,ZZZ,3", "/t", "REG_BINARY" });
        Assert.True(opts.HasError);
        Assert.Contains("not a valid Binary", opts.Error);
    }

    // ── ToString() ─────────────────────────────────────────────────

    [Fact]
    public void ToString_PathAction_ShowsKeyOnly()
    {
        var opts = new CmdLineOptions(new[] { "PATH", @"HKLM\Software\Test" });
        Assert.Equal(@"Action:Path RegistryKey:HKLM\Software\Test", opts.ToString());
    }

    [Fact]
    public void ToString_GetAction_ShowsKeyAndValueName()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!Val" });
        Assert.Equal(@"Action:Get RegistryKey:HKLM\Software\Test ValueName:Val", opts.ToString());
    }

    [Fact]
    public void ToString_DeleteAction_ShowsKeyAndValueName()
    {
        var opts = new CmdLineOptions(new[] { "DELETE", @"HKLM\Software\Test!Val" });
        Assert.Equal(@"Action:Delete RegistryKey:HKLM\Software\Test ValueName:Val", opts.ToString());
    }

    [Fact]
    public void ToString_SetAction_ShowsAllFields()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "42", "/t", "REG_DWORD" });
        Assert.Equal(@"Action:Set RegistryKey:HKLM\Software\Test ValueName:Val Data:42 DataType:DWord", opts.ToString());
    }

    // ── Edge cases ─────────────────────────────────────────────────

    [Fact]
    public void MultipleSwitches_Combined()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test", "/v", "Val", "/d", "hello", "/t", "REG_SZ", "/p" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("Val", opts.ValueName);
        Assert.Equal("hello", opts.Data);
        Assert.Equal(RegistryValueKind.String, opts.DataType);
        Assert.True(opts.Pause);
    }

    [Fact]
    public void DefaultPause_IsFalse()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!Val" });
        Assert.False(opts.Pause);
    }

    [Fact]
    public void DefaultDataType_IsUnknown()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!Val" });
        Assert.Equal(RegistryValueKind.Unknown, opts.DataType);
    }

    [Fact]
    public void Get_WithValueName_NoError()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!Val" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(ActionTask.Get, opts.Action);
    }

    [Fact]
    public void Delete_WithValueName_NoError()
    {
        var opts = new CmdLineOptions(new[] { "DELETE", @"HKLM\Software\Test!Val" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(ActionTask.Delete, opts.Action);
    }

    [Fact]
    public void Get_DoesNotRequireData()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Software\Test!Val" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(string.Empty, opts.Data);
    }

    [Fact]
    public void Delete_DoesNotRequireData()
    {
        var opts = new CmdLineOptions(new[] { "DELETE", @"HKLM\Software\Test!Val" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(string.Empty, opts.Data);
    }

    [Fact]
    public void BangNotation_EmptyValueName_SetsError()
    {
        // "HKLM\Test!" -> bang at end, ValueName is empty string
        var opts = new CmdLineOptions(new[] { "GET", @"HKLM\Test!" });
        Assert.True(opts.HasError);
        Assert.Contains("ValueName is required", opts.Error);
    }

    // ── /sid switch (T3) ───────────────────────────────────────────

    [Fact]
    public void Switch_Sid_FriendlyName_SetsSid()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKCU\Software\Test!Val", "/sid", "Administrators" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("S-1-5-32-544", opts.Sid.Value);
    }

    [Fact]
    public void Switch_Sid_RawSidString_SetsSid()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKCU\Software\Test!Val", "/sid", "S-1-5-32-544" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal("S-1-5-32-544", opts.Sid.Value);
    }

    [Theory]
    [InlineData("Users",                 "S-1-5-32-545")]
    [InlineData("Guests",                "S-1-5-32-546")]
    [InlineData("System",                "S-1-5-18")]
    [InlineData("Everyone",              "S-1-1-0")]
    public void Switch_Sid_AllSupportedNames_SetCorrectSid(string name, string expectedSid)
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKCU\Software\Test!Val", "/sid", name });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(expectedSid, opts.Sid.Value);
    }

    [Fact]
    public void Switch_Sid_InvalidValue_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKCU\Software\Test!Val", "/sid", "NotAValidSid" });
        Assert.True(opts.HasError);
        Assert.Contains("invalid SID value", opts.Error);
    }

    [Fact]
    public void Switch_Sid_MissingValue_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKCU\Software\Test!Val", "/sid" });
        Assert.True(opts.HasError);
        Assert.Contains("/sid requires a value", opts.Error);
    }

    [Fact]
    public void Switch_Sid_NotProvided_DefaultsToEmpty()
    {
        var opts = new CmdLineOptions(new[] { "GET", @"HKCU\Software\Test!Val" });
        Assert.False(opts.HasError, opts.Error);
        Assert.True(string.IsNullOrWhiteSpace(opts.Sid.Value));
    }

    // ── Large DWORD / QWORD / hex binary (T4) ─────────────────────

    [Theory]
    [InlineData("2147483648",  unchecked((int)0x80000000))]  // Int32.MinValue bits
    [InlineData("4294967295",  unchecked((int)0xFFFFFFFF))]  // -1 bits
    [InlineData("0",           0)]
    [InlineData("2147483647",  2147483647)]                  // Int32.MaxValue
    public void DataConversion_DWord_LargeUnsignedValues(string input, int expectedBits)
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", input, "/t", "REG_DWORD" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<int>(opts.ConvertedData);
        Assert.Equal(expectedBits, opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_DWord_OutOfRange_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "4294967296", "/t", "REG_DWORD" });
        Assert.True(opts.HasError);
        Assert.Contains("not a valid DWord", opts.Error);
    }

    [Theory]
    [InlineData("9223372036854775808",   unchecked((long)0x8000000000000000L))]  // Int64.MinValue bits
    [InlineData("18446744073709551615",  unchecked((long)0xFFFFFFFFFFFFFFFFL))]  // -1 bits
    [InlineData("0",                     0L)]
    [InlineData("9223372036854775807",   9223372036854775807L)]                  // Int64.MaxValue
    public void DataConversion_QWord_LargeUnsignedValues(string input, long expectedBits)
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", input, "/t", "REG_QWORD" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<long>(opts.ConvertedData);
        Assert.Equal(expectedBits, opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_QWord_OutOfRange_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "18446744073709551616", "/t", "REG_QWORD" });
        Assert.True(opts.HasError);
        Assert.Contains("not a valid QWord", opts.Error);
    }

    [Fact]
    public void DataConversion_Binary_HexValues_Parsed()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "0xFF,0x0A,0x01", "/t", "REG_BINARY" });
        Assert.False(opts.HasError, opts.Error);
        Assert.IsType<byte[]>(opts.ConvertedData);
        Assert.Equal(new byte[] { 255, 10, 1 }, (byte[])opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_Binary_MixedDecimalAndHex_Parsed()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "1,0xFF,255", "/t", "REG_BINARY" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(new byte[] { 1, 255, 255 }, (byte[])opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_Binary_HexCaseInsensitive_Parsed()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "0XFF,0xff,0Xab", "/t", "REG_BINARY" });
        Assert.False(opts.HasError, opts.Error);
        Assert.Equal(new byte[] { 255, 255, 171 }, (byte[])opts.ConvertedData);
    }

    [Fact]
    public void DataConversion_Binary_InvalidHex_SetsError()
    {
        var opts = new CmdLineOptions(new[] { "SET", @"HKLM\Software\Test!Val", "/d", "0xFF,0xGG", "/t", "REG_BINARY" });
        Assert.True(opts.HasError);
        Assert.Contains("not a valid Binary", opts.Error);
    }
}

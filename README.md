# WLGPO — Windows Local Group Policy Object CLI

A command-line utility for scripting **Windows Local Group Policy** registry modifications. WLGPO wraps the Windows COM-based Group Policy Object API, enabling automated Get, Set, and Delete operations on GPO-managed registry values — suitable for deployment scripts, configuration management, and IT automation.

> **Requires:** Windows x64, .NET Framework 4.8+, Administrator privileges

---

## Features

- Read and write GPO-controlled registry values (HKLM / HKCU)
- Supports all common registry data types: `REG_SZ`, `REG_EXPAND_SZ`, `REG_MULTI_SZ`, `REG_DWORD`, `REG_QWORD`, `REG_BINARY`, `REG_NONE`
- Bang (`!`) notation shorthand for combining key path and value name
- Predictable exit codes for scripting
- Lightweight, single-executable output via `dotnet publish`

---

## Usage

```
WLGPO <Action> <RegistryKey> [options]
```

### Actions

| Action | Description |
|--------|-------------|
| `Get` | Read a registry value from GPO |
| `Set` | Write a registry value to GPO |
| `Delete` | Remove a registry value from GPO |
| `Path` | Print the resolved GPO registry path |

### Options

| Switch | Description |
|--------|-------------|
| `/v <name>` | Registry value name |
| `/d <data>` | Data to write (used with `Set`) |
| `/t <type>` | Registry data type (default: `REG_SZ`) |
| `/p` | Pause before exit |
| `--help`, `-h`, `/?` | Display help |

### Bang Notation

The `!` character can be used to combine the registry key path and value name into a single argument:

```
HKLM\Software\Policies\MyApp!ValueName
```

is equivalent to:

```
HKLM\Software\Policies\MyApp /v ValueName
```

---

## Examples

```cmd
# Read a value
WLGPO Get HKLM\Software\Policies\MyApp /v MaxRetries

# Read using bang notation
WLGPO Get HKLM\Software\Policies\MyApp!MaxRetries

# Set a DWORD value
WLGPO Set HKLM\Software\Policies\MyApp /v MaxRetries /d 5 /t REG_DWORD

# Set a string value (default type)
WLGPO Set HKLM\Software\Policies\MyApp!ServerUrl /d "https://server.example.com"

# Set a multi-string value (values separated by \0)
WLGPO Set HKLM\Software\Policies\MyApp /v AllowedHosts /d "host1\0host2" /t REG_MULTI_SZ

# Set a binary value (comma-separated bytes)
WLGPO Set HKLM\Software\Policies\MyApp /v Flags /d "0x01,0x02,0xFF" /t REG_BINARY

# Delete a value
WLGPO Delete HKLM\Software\Policies\MyApp /v MaxRetries

# Print the resolved path
WLGPO Path HKCU\Software\Policies\MyApp
```

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Error (invalid arguments, access denied, etc.) |
| `2` | Set failed |
| `3` | Value already set (no change made) |

---

## Data Types

| Type | Description |
|------|-------------|
| `REG_SZ` | String (default) |
| `REG_EXPAND_SZ` | Expandable string (environment variables) |
| `REG_MULTI_SZ` | Multi-string, values separated by `\0` |
| `REG_DWORD` | 32-bit integer |
| `REG_QWORD` | 64-bit integer |
| `REG_BINARY` | Binary data as comma-separated byte values (decimal or hex) |
| `REG_NONE` | No data / "not configured" |

---

## Building

### Prerequisites

- .NET SDK 4.8 or later
- Windows x64

### Build

```cmd
dotnet build WLGPO.sln -c Release
```

### Publish (single output directory)

```cmd
publish.cmd
```

This runs `dotnet publish WLGPO.sln -c Release -o .\publish` and places the executable in `.\publish\`.

### Run Tests

```cmd
dotnet test WLGPO.sln
```

The test suite contains 65+ xUnit tests covering CLI parsing, data type conversion, bang notation, validation, and edge cases.

---

## Project Structure

```
WLGPO/
├── WLGPO/                        # Main application
│   ├── Program.cs                # Entry point and action dispatcher
│   ├── CmdLineOptions.cs         # Command-line argument parser
│   ├── ActionTask.cs             # Enum: Get, Set, Delete, Path
│   └── GPO/                      # Windows GPO COM abstraction layer
│       ├── IGroupPolicyObject.cs # COM interface (P/Invoke)
│       ├── GroupPolicyObject.cs  # COM wrapper with lifecycle management
│       ├── WindowsGroupPolicyObject.cs  # Core implementation (STA thread, registry)
│       ├── GPO_SECTIONS.cs       # Enum: User / Machine sections
│       ├── GPO_OPEN_FLAGS.cs     # Enum: GPO open flags
│       ├── GPO_OPTIONS.cs        # Enum: GPO disable options
│       ├── HRESULT.cs            # COM HRESULT error codes
│       └── GROUP_POLICY_OBJECT_TYPE.cs  # Enum: GPO types
├── WLGPO.Tests/                  # xUnit test project
│   └── CmdLineOptionsTests.cs    # CLI parsing tests
├── WLGPO.sln                     # Visual Studio solution
└── publish.cmd                   # Release publish script
```

---

## Requirements

- **OS:** Windows (uses Windows COM Group Policy APIs)
- **Architecture:** x64
- **Runtime:** .NET Framework 4.8+
- **Privileges:** Must be run as Administrator

---

## License

See repository for license details.

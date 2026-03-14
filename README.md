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
| `/sid <principal>` | Target a per-user local GPO for the specified principal (name or raw SID) |
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

### Per-Principal GPO (`/sid`)

The `/sid` switch targets the **per-user local GPO** for a specific Windows principal instead of the machine-level GPO. It is only valid with `HKCU` registry keys.

Accepted values are either a friendly name or a raw SID string:

| Friendly Name | SID |
|---------------|-----|
| `Administrators` | `S-1-5-32-544` |
| `Users` | `S-1-5-32-545` |
| `Guests` | `S-1-5-32-546` |
| `PowerUsers` | `S-1-5-32-547` |
| `BackupOperators` | `S-1-5-32-551` |
| `RemoteDesktopUsers` | `S-1-5-32-555` |
| `NetworkConfigOps` | `S-1-5-32-556` |
| `HyperVAdministrators` | `S-1-5-32-578` |
| `RemoteManagementUsers` | `S-1-5-32-580` |
| `System` | `S-1-5-18` |
| `LocalService` | `S-1-5-19` |
| `NetworkService` | `S-1-5-20` |
| `Iusr` | `S-1-5-17` |
| `Everyone` | `S-1-1-0` |

A raw SID string (e.g. `S-1-5-32-544`) is also accepted.

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

# Set a value in the per-user GPO for the Administrators group
WLGPO Set HKCU\Software\Policies\MyApp /v Setting /d "1" /sid Administrators

# Set a value using a raw SID
WLGPO Set HKCU\Software\Policies\MyApp!Setting /d "1" /sid S-1-5-32-544

# Get a value from the per-user GPO for the Users group
WLGPO Get HKCU\Software\Policies\MyApp!Setting /sid Users
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

The test suite contains 75+ xUnit tests covering CLI parsing, data type conversion, bang notation, validation, `LocalPrincipalSid` parsing, and edge cases.

---

## Project Structure

```
WLGPO/
├── WLGPO/                        # Main application
│   ├── Program.cs                # Entry point and action dispatcher
│   ├── CmdLineOptions.cs         # Command-line argument parser
│   ├── ActionTask.cs             # Enum: Get, Set, Delete, Path
│   └── GPO/                      # Windows GPO COM abstraction layer
│       ├── IGroupPolicyObject.cs          # Base COM interface (P/Invoke)
│       ├── IGroupPolicyObject2.cs         # Extended COM interface (adds OpenLocalMachineGPOForPrincipal)
│       ├── GroupPolicyObject.cs           # COM coclass registration
│       ├── GroupPolicyObjectManaged.cs    # IDisposable wrapper over IGroupPolicyObject2
│       ├── WindowsGroupPolicyObject.cs    # Core implementation (STA thread, registry, per-principal)
│       ├── LocalPrincipalSid.cs           # Well-known SID value type with friendly-name lookup
│       ├── GPO_SECTIONS.cs                # Enum: User / Machine sections
│       ├── GPO_OPEN_FLAGS.cs              # Enum: GPO open flags
│       ├── GPO_OPTIONS.cs                 # Enum: GPO disable options
│       ├── HRESULT.cs                     # COM HRESULT error codes
│       └── GROUP_POLICY_OBJECT_TYPE.cs    # Enum: GPO types
├── WLGPO.Tests/                  # xUnit test project
│   ├── CmdLineOptionsTests.cs    # CLI parsing tests
│   └── LocalPrincipalSidTests.cs # LocalPrincipalSid.TryParse tests
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

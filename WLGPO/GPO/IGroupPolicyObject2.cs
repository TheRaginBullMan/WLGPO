using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WLGPO.GPO;

[ComImport, Guid("7E37D5E7-263D-45CF-842B-96A95C63E46C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IGroupPolicyObject2
{
    [PreserveSig]
    HRESULT New(
        [MarshalAs(UnmanagedType.LPWStr)] string pszDomainName,
        [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
        uint dwFlags);

    [PreserveSig]
    HRESULT OpenDSGPO(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        uint dwFlags);

    [PreserveSig]
    HRESULT OpenLocalMachineGPO(
        uint dwFlags);

    [PreserveSig]
    HRESULT OpenRemoteMachineGPO(
        [MarshalAs(UnmanagedType.LPWStr)] string pszComputerName,
        uint dwFlags);

    [PreserveSig]
    HRESULT Save(
        [MarshalAs(UnmanagedType.Bool)] bool bMachine,
        [MarshalAs(UnmanagedType.Bool)] bool bAdd,
        [MarshalAs(UnmanagedType.LPStruct)] Guid pGuidExtension,
        [MarshalAs(UnmanagedType.LPStruct)] Guid pGuid);

    [PreserveSig]
    HRESULT Delete();

    [PreserveSig]
    HRESULT GetName(
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
        int cchMaxLength);

    [PreserveSig]
    HRESULT GetDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
        int cchMaxLength);

    [PreserveSig]
    HRESULT SetDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszName);

    [PreserveSig]
    HRESULT GetPath(
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
        int cchMaxPath);

    [PreserveSig]
    HRESULT GetDSPath(
        uint dwSection,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
        int cchMaxPath);

    [PreserveSig]
    HRESULT GetFileSysPath(
        uint dwSection,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
        int cchMaxPath);

    [PreserveSig]
    HRESULT GetRegistryKey(
        uint dwSection,
        out IntPtr hKey);

    [PreserveSig]
    HRESULT GetOptions(
        out uint dwOptions);

    [PreserveSig]
    HRESULT SetOptions(
        uint dwOptions,
        uint dwMask);

    [PreserveSig]
    HRESULT GetType(
        out GROUP_POLICY_OBJECT_TYPE gpoType);

    [PreserveSig]
    HRESULT GetMachineName(
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
        int cchMaxLength);

    [PreserveSig]
    HRESULT GetPropertySheetPages(
        out IntPtr hPages,
        out uint uPageCount);
    
    [PreserveSig]
    HRESULT OpenLocalMachineGPOForPrincipal(
        [MarshalAs(UnmanagedType.LPWStr)] string pszLocalUserOrGroupSID,
        uint dwFlags);
    
    [PreserveSig]
    HRESULT GetRegistryKeyPath(
        uint dwSection,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszRegistryKeyPath,
        int cchMaxLength);
}
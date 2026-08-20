using System.Runtime.InteropServices;

namespace Pofus.Platform;

/// <summary>
/// Minimal COM interop for creating a Windows shortcut (.lnk).
///
/// The shell exposes no managed API for this, and the usual shortcut
/// (WScript.Shell) depends on Windows Script Host, which hardened machines
/// sometimes disable. Talking to IShellLink directly has no such dependency.
/// </summary>
internal static class ShellLink
{
    /// <summary>Writes a shortcut to <paramref name="shortcutPath"/> pointing at <paramref name="targetPath"/>.</summary>
    public static void Create(string shortcutPath, string targetPath, string description)
    {
        // Cast through object: the coclass is only a CLSID placeholder and does
        // not declare the interface, so a direct cast will not compile.
        var link = (IShellLinkW)(object)new ShellLinkCoClass();
        link.SetPath(targetPath);
        link.SetDescription(description);

        // Without this, the app starts with whatever directory the shell
        // happens to be in, which breaks any relative path it resolves.
        var workingDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            link.SetWorkingDirectory(workingDirectory);
        }

        ((IPersistFile)link).Save(shortcutPath, fRemember: true);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private sealed class ShellLinkCoClass
    {
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] char[] file, int maxPath, nint findData, uint flags);

        void GetIDList(out nint idList);

        void SetIDList(nint idList);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] char[] name, int maxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] char[] dir, int maxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] char[] args, int maxArgs);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);

        void GetHotkey(out short hotkey);

        void SetHotkey(short hotkey);

        void GetShowCmd(out int showCmd);

        void SetShowCmd(int showCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] char[] iconPath, int iconPathLength, out int iconIndex);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string relativePath, uint reserved);

        void Resolve(nint hwnd, uint flags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
    }

    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid classId);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
    }
}

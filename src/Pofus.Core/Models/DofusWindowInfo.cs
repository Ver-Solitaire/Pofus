namespace Pofus.Core.Models;

/// <summary>
/// A single detected Dofus game window. <see cref="Handle"/> is an opaque Win32
/// HWND value — only <c>Pofus.Platform</c> interprets it via P/Invoke.
/// </summary>
public sealed record DofusWindowInfo(nint Handle, string Title);

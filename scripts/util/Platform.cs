using System.IO;
using Godot;

namespace util;

public static class Platform
{
    /// <summary>
    /// Path to the running .app bundle on macOS, the executable lives at Rhythia.app/Contents/MacOS/Rhythia
    /// </summary>
    public static string MacAppBundlePath => Path.GetFullPath(Path.Combine(OS.GetExecutablePath(), "../../.."));

    /// <summary>
    /// Wraps a value in single quotes so it is passed to /bin/sh as one literal argument
    /// </summary>
    public static string ShellQuote(string value) => $"'{value.Replace("'", "'\\''")}'";

    /// <summary>
    /// Shell snippet that blocks until the current process has exited
    /// </summary>
    public static string WaitForExitScript => $"while kill -0 {OS.GetProcessId()} 2>/dev/null; do sleep 0.1; done";
}

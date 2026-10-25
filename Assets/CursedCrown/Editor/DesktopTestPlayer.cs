using System;
using System.IO;
using UnityEditor;

namespace CursedCrown.Editor
{
    public static class DesktopTestPlayer
    {
        public static BuildTarget Target
        {
            get
            {
#if UNITY_EDITOR_WIN
                return BuildTarget.StandaloneWindows64;
#elif UNITY_EDITOR_OSX
                return BuildTarget.StandaloneOSX;
#else
                return BuildTarget.StandaloneLinux64;
#endif
            }
        }
        public static string BuildPath
        {
            get
            {
#if UNITY_EDITOR_WIN
                return "Builds/Windows/CursedCrownTest.exe";
#elif UNITY_EDITOR_OSX
                return "Builds/CursedCrownTest.app";
#else
                return "Builds/Linux/CursedCrownTest.x86_64";
#endif
            }
        }
        public static string BinaryPath()
        {
#if UNITY_EDITOR_OSX
            string directory = Path.Combine(BuildPath, "Contents/MacOS");
            if (!Directory.Exists(directory)) throw new InvalidOperationException("Build the desktop test player first.");
            var files = Directory.GetFiles(directory);
            if (files.Length != 1) throw new InvalidOperationException("Expected one executable in the test app.");
            return Path.GetFullPath(files[0]);
#else
            string binary = Path.GetFullPath(BuildPath);
            if (!File.Exists(binary)) throw new InvalidOperationException("Build the desktop test player first.");
            return binary;
#endif
        }
    }
}

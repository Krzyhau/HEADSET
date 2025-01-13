using Common;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HEADSET
{
    internal static class OpenVRNativeLoader
    {
        private const string ResourcePrefix = "HEADSET.OpenVR.bin.";
        private const string TempDirectoryIdentifier = "fez_headset_openvr";

        private static IntPtr LibraryPointer = IntPtr.Zero;

        public static bool TryToLoad()
        {
            if (LibraryPointer != IntPtr.Zero)
            {
                return true;
            }

            var resoureceName = GetNativeLibraryResourceName();
            if (resoureceName == string.Empty)
            {
                return false;
            }

            var tempLibraryDirectoryPath = GetTempLibraryDirectoryPath();
            if (!Directory.Exists(tempLibraryDirectoryPath))
            {
                Directory.CreateDirectory(tempLibraryDirectoryPath);
            }

            string tempLibraryPath = GetTempLibraryPath();
            using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resoureceName))
            {
                using var fileStream = File.Create(tempLibraryPath);
                resourceStream.CopyTo(fileStream);
            }

            LibraryPointer = NativeLibraryInterop.Load(tempLibraryPath);
            return LibraryPointer != IntPtr.Zero;
        }

        private static string GetNativeLibraryResourceName()
        {
            var platformIdentifier = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) => "osx32",
                Architecture.X86 when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => "win32",
                Architecture.X64 when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => "win64",
                Architecture.X86 when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => "linux32",
                Architecture.X64 when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => "linux64",
                Architecture.Arm64 when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => "linuxarm64",
                _ => string.Empty
            };

            return $"{ResourcePrefix}{platformIdentifier}.{GetNativeLibraryName()}";
        }

        private static string GetNativeLibraryName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "openvr_api.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "libopenvr_api.so";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "libopenvr_api.dylib";
            }
            return string.Empty;
        }

        private static string GetTempLibraryDirectoryPath()
        {
            return Path.Combine(Path.GetTempPath(), TempDirectoryIdentifier);
        }

        private static string GetTempLibraryPath()
        {
            return Path.Combine(GetTempLibraryDirectoryPath(), GetNativeLibraryName());
        }

        public static void Unload()
        {
            if(LibraryPointer != IntPtr.Zero)
            {
                NativeLibraryInterop.Free(LibraryPointer);
                DeleteTempLibrary();
                LibraryPointer = IntPtr.Zero;
            }
        }

        private static void DeleteTempLibrary()
        {
            var tempLibraryDirectoryPath = GetTempLibraryDirectoryPath();

            try
            {
                Directory.Delete(tempLibraryDirectoryPath, true);
            } 
            catch (Exception ex)
            {
                Logger.Log("HEADSET", $"Failed to delete temporary library directory: {ex.Message}");
            }
        }

        static class NativeLibraryInterop
        {
            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr Dlopen(string fileName, int flags);

            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            private static extern int Dlclose(IntPtr handle);

            public static IntPtr Load(string fileName)
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? LoadLibrary(fileName)
                    : Dlopen(fileName, 1);
            }

            public static bool Free(IntPtr libraryHandle)
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? FreeLibrary(libraryHandle)
                    : Dlclose(libraryHandle) == 0;
            }
        }
    }
}

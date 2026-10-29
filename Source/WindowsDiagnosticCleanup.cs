using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace AIGaming
{
    internal static class WindowsDiagnosticCleanup
    {
        private const uint ShareReadWrite = 0x00000003;
        private const uint GenericReadAndDelete = 0x80010000;
        private const uint GenericReadWrite = 0xC0000000;
        private const uint OpenExisting = 3;
        private const uint OpenAlways = 4;
        private const uint OpenReparsePoint = 0x00200000;
        private const uint BackupSemantics = 0x02000000;
        private const uint ReparsePoint = 0x00000400;
        private const uint DirectoryAttribute = 0x00000010;
        private const int FileDispositionInfo = 4;
        private const string StateMarker = "AI Gaming cleanup v1 ";

        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime { internal uint Low; internal uint High; }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileInformation
        {
            internal uint Attributes;
            internal FileTime CreationTime;
            internal FileTime LastAccessTime;
            internal FileTime LastWriteTime;
            internal uint VolumeSerialNumber;
            internal uint FileSizeHigh;
            internal uint FileSizeLow;
            internal uint NumberOfLinks;
            internal uint FileIndexHigh;
            internal uint FileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Disposition { internal byte DeleteFile; }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security,
            uint creation, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInformation information);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle file, int informationClass,
            ref Disposition information, uint bufferSize);

        internal static bool TryDelete(string directory, string name, DateTime nowUtc)
        {
            return InspectOrDelete(directory, name, nowUtc, true);
        }

        internal static bool InspectForCleanup(string directory, string name, DateTime nowUtc)
        {
            return InspectOrDelete(directory, name, nowUtc, false);
        }

        private static bool InspectOrDelete(string directory, string name, DateTime nowUtc, bool delete)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
            if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name) return false;
            var directories = new List<SafeFileHandle>();
            try
            {
                string fullDirectory;
                if (!PinDirectoryChain(directory, directories, false, out fullDirectory)) return false;

                string path = Path.Combine(fullDirectory, name);
                using (SafeFileHandle handle = CreateFileW(path, GenericReadAndDelete, 0, IntPtr.Zero, OpenExisting, OpenReparsePoint, IntPtr.Zero))
                {
                    if (handle.IsInvalid) return false;
                    FileInformation information;
                    if (!GetFileInformationByHandle(handle, out information)) return false;
                    if ((information.Attributes & (ReparsePoint | DirectoryAttribute)) != 0 || information.NumberOfLinks != 1) return delete;
                    long fileTime = ((long)information.LastWriteTime.High << 32) | information.LastWriteTime.Low;
                    DateTime modified = DateTime.FromFileTimeUtc(fileTime);
                    bool active = string.Equals(name, "ai-gaming-" + nowUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + ".log", StringComparison.Ordinal);
                    if (!DiagnosticLogPolicy.ShouldDelete(name, DiagnosticLogPolicy.Marker, modified, nowUtc, active)) return delete;
                    using (var stream = new FileStream(handle, FileAccess.Read))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        foreach (char expected in DiagnosticLogPolicy.Marker)
                            if (reader.Read() != expected) return delete;
                        int newline = reader.Read();
                        if (newline != '\n' && !(newline == '\r' && reader.Read() == '\n')) return delete;
                        if (!delete) return true;
                        var disposition = new Disposition { DeleteFile = 1 };
                        return SetFileInformationByHandle(handle, FileDispositionInfo, ref disposition, 1);
                    }
                }
            }
            catch { return false; }
            finally
            {
                for (int i = directories.Count - 1; i >= 0; i--) directories[i].Dispose();
            }
        }

        internal static bool EnsureDirectory(string directory)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
            var handles = new List<SafeFileHandle>();
            try { string ignored; return PinDirectoryChain(directory, handles, true, out ignored); }
            catch { return false; }
            finally { CloseDirectories(handles); }
        }

        internal static bool TryEnumerate(string directory, out string[] paths)
        {
            paths = new string[0];
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
            var handles = new List<SafeFileHandle>();
            try
            {
                string fullDirectory;
                if (!PinDirectoryChain(directory, handles, false, out fullDirectory)) return false;
                paths = Directory.GetFiles(fullDirectory, "ai-gaming-*.log", SearchOption.TopDirectoryOnly);
                return true;
            }
            catch { return false; }
            finally { CloseDirectories(handles); }
        }

        internal static bool TryAppend(string directory, string name, string line)
        {
            if (name == null || Path.GetFileName(name) != name || line == null) return false;
            return WithOwnedFile(directory, name, DiagnosticLogPolicy.Marker, true, delegate(FileStream file, bool created)
            {
                if (created) WriteUtf8(file, DiagnosticLogPolicy.Marker + "\n");
                file.Seek(0, SeekOrigin.End);
                WriteUtf8(file, line + "\n");
                return true;
            });
        }

        internal static bool TryReadState(string directory, out long ticks)
        {
            bool parsed;
            return TryReadStateStatus(directory, out ticks, out parsed) && parsed;
        }

        internal static bool TryReadStateStatus(string directory, out long ticks, out bool parsed)
        {
            long result = 0;
            bool valid = false;
            bool ok = WithOwnedFile(directory, "cleanup.state", null, false, delegate(FileStream file, bool created)
            {
                if (file == null) return true;
                if (file.Length > 128 || file.Length < StateMarker.Length) return true;
                var bytes = new byte[(int)file.Length];
                file.Seek(0, SeekOrigin.Begin);
                int count = file.Read(bytes, 0, bytes.Length);
                string value = Encoding.UTF8.GetString(bytes, 0, count);
                valid = value.StartsWith(StateMarker, StringComparison.Ordinal) &&
                    long.TryParse(value.Substring(StateMarker.Length), System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out result);
                return true;
            });
            ticks = result;
            parsed = valid;
            return ok;
        }

        internal static bool TryWriteState(string directory, long ticks)
        {
            return WithOwnedFile(directory, "cleanup.state", StateMarker, true, delegate(FileStream file, bool created)
            {
                file.Seek(0, SeekOrigin.Begin);
                file.SetLength(0);
                WriteUtf8(file, StateMarker + ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return true;
            });
        }

        private static bool WithOwnedFile(string directory, string name, string marker, bool create, Func<FileStream, bool, bool> action)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
            var directories = new List<SafeFileHandle>();
            try
            {
                string fullDirectory;
                if (!PinDirectoryChain(directory, directories, false, out fullDirectory)) return false;
                using (SafeFileHandle handle = CreateFileW(Path.Combine(fullDirectory, name), GenericReadWrite, 0, IntPtr.Zero,
                    create ? OpenAlways : OpenExisting, OpenReparsePoint, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (handle.IsInvalid)
                    {
                        if (!create && marker == null && error == 2) return action(null, false);
                        return false;
                    }
                    if (create && error != 0 && error != 183) return false;
                    bool created = create && error == 0;
                    FileInformation info;
                    if (!GetFileInformationByHandle(handle, out info) || (info.Attributes & (ReparsePoint | DirectoryAttribute)) != 0 || info.NumberOfLinks != 1) return false;
                    using (var file = new FileStream(handle, FileAccess.ReadWrite))
                    {
                        if (!created && marker != null)
                        {
                            foreach (byte expected in Encoding.UTF8.GetBytes(marker))
                                if (file.ReadByte() != expected) return false;
                            if (marker == DiagnosticLogPolicy.Marker)
                            {
                                int newline = file.ReadByte();
                                if (newline != '\n' && !(newline == '\r' && file.ReadByte() == '\n')) return false;
                            }
                        }
                        return action(file, created);
                    }
                }
            }
            catch { return false; }
            finally { CloseDirectories(directories); }
        }

        private static void WriteUtf8(FileStream file, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            file.Write(bytes, 0, bytes.Length);
            file.Flush();
        }

        private static bool PinDirectoryChain(string directory, IList<SafeFileHandle> handles, bool create, out string fullDirectory)
        {
            fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string root = Path.GetPathRoot(fullDirectory);
            if (string.IsNullOrEmpty(root)) return false;
            string current = root;
            if (!PinDirectory(current, handles)) return false;
            foreach (string segment in fullDirectory.Substring(root.Length).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (create && !Directory.Exists(current)) Directory.CreateDirectory(current);
                if (!PinDirectory(current, handles)) return false;
            }
            return true;
        }

        private static void CloseDirectories(IList<SafeFileHandle> handles)
        {
            for (int i = handles.Count - 1; i >= 0; i--) handles[i].Dispose();
        }

        private static bool PinDirectory(string path, IList<SafeFileHandle> handles)
        {
            SafeFileHandle handle = CreateFileW(path, 0, ShareReadWrite, IntPtr.Zero, OpenExisting,
                BackupSemantics | OpenReparsePoint, IntPtr.Zero);
            if (handle.IsInvalid) { handle.Dispose(); return false; }
            FileInformation information;
            if (!GetFileInformationByHandle(handle, out information) ||
                (information.Attributes & ReparsePoint) != 0 || (information.Attributes & DirectoryAttribute) == 0)
            {
                handle.Dispose();
                return false;
            }
            handles.Add(handle);
            return true;
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

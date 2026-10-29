using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AIGaming
{
    internal static class DiagnosticLog
    {
        private static readonly object Sync = new object();
        private static Timer _timer;
        private static string _directory;
        private static string _logsRoot;
        private static bool _reportedFailure;
        private static bool _reportedCleanupUnavailable;

        internal static void Start()
        {
            lock (Sync)
            {
                try
                {
                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    if (string.IsNullOrEmpty(local)) throw new IOException();
                    if (CompanionBuildInfo.Edition != "release" && CompanionBuildInfo.Edition != "development") throw new IOException();
                    string current = Path.GetFullPath(local);
                    string[] parts = { "SpaceSquare640", "AI Gaming", "Logs", CompanionBuildInfo.Edition };
                    foreach (string part in parts)
                    {
                        current = Path.Combine(current, part);
                    }
                    if (!WindowsDiagnosticCleanup.EnsureDirectory(current)) throw new IOException();
                    _directory = current;
                    _logsRoot = Path.GetDirectoryName(current);
                    if (_timer == null) _timer = new Timer(OnTimer, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
                }
                catch { ReportFailure(); }
            }
            Detail("Mod enabled.");
        }

        internal static void Stop()
        {
            Detail("Mod disabled.");
            lock (Sync)
            {
                if (_timer != null) { _timer.Dispose(); _timer = null; }
                _directory = null;
                _logsRoot = null;
            }
        }

        internal static void Detail(string message) { Write(false, message); }
        internal static void Warning(string message) { Write(true, message); }
        internal static void Error(string message) { Write(true, message, true); }

        private static void Write(bool warning, string message, bool error = false)
        {
            if (!DiagnosticLogPolicy.ShouldLog(CompanionBuildInfo.Edition, warning)) return;
            if (error) Debug.LogError("[AI Gaming] " + message);
            else if (warning) Debug.LogWarning("[AI Gaming] " + message);
            lock (Sync)
            {
                try
                {
                    if (_directory == null)
                    {
                        if (!warning) Debug.Log("[AI Gaming] " + message);
                        return;
                    }
                    string line = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " " + (error ? "ERROR " : warning ? "WARN " : "INFO ") + message;
                    if (!WindowsDiagnosticCleanup.TryAppend(_directory, FileName(DateTime.UtcNow), line)) throw new IOException();
                }
                catch
                {
                    ReportFailure();
                    if (!warning) Debug.Log("[AI Gaming] " + message);
                }
            }
        }

        private static void OnTimer(object state)
        {
            string root;
            Timer timer;
            lock (Sync) { root = _logsRoot; timer = _timer; }
            if (root == null || timer == null) return;
            TimeSpan delay = CleanupDirectories(root, timer);
            lock (Sync) { if (ReferenceEquals(timer, _timer)) timer.Change(delay, TimeSpan.FromMilliseconds(-1)); }
        }

        private static TimeSpan CleanupDirectories(string logsRoot, Timer timer)
        {
            TimeSpan next = TimeSpan.FromHours(48);
            foreach (string edition in new[] { "release", "development" })
            {
                string directory = Path.Combine(logsRoot, edition);
                if (!Directory.Exists(directory)) continue;
                try
                {
                    TimeSpan delay = CleanupIfDue(directory, timer);
                    if (delay < next) next = delay;
                }
                catch { ReportFailure(); }
            }
            return next;
        }

        private static TimeSpan CleanupIfDue(string directory, Timer timer)
        {
            DateTime now = DateTime.UtcNow;
            long ticks;
            bool parsed;
            if (!WindowsDiagnosticCleanup.TryReadStateStatus(directory, out ticks, out parsed)) throw new IOException();
            bool invalidState = !parsed;
            if (parsed)
            {
                TimeSpan delay = DiagnosticLogPolicy.GetCleanupDelay(ticks, now);
                if (delay > TimeSpan.Zero) return delay;
            }
            bool failed = false;
            string[] paths;
            if (!WindowsDiagnosticCleanup.TryEnumerate(directory, out paths)) throw new IOException();
            foreach (string path in paths)
            {
                string name = Path.GetFileName(path);
                bool active = string.Equals(name, FileName(now), StringComparison.Ordinal);
                if (!DiagnosticLogPolicy.ShouldDelete(name, DiagnosticLogPolicy.Marker, DateTime.MinValue, now, active)) continue;
                lock (Sync)
                {
                    if (!ReferenceEquals(timer, _timer)) return TimeSpan.FromHours(48);
                    if (!WindowsDiagnosticCleanup.TryDelete(directory, name, now))
                    {
                        failed = true;
                        if (!_reportedCleanupUnavailable)
                        {
                            _reportedCleanupUnavailable = true;
                            Debug.LogWarning("[AI Gaming] Automatic diagnostic cleanup could not safely remove an eligible file.");
                        }
                    }
                }
            }
            if (failed) return TimeSpan.FromHours(48);
            lock (Sync)
            {
                if (!ReferenceEquals(timer, _timer)) return TimeSpan.FromHours(48);
                if (!WindowsDiagnosticCleanup.TryWriteState(directory, now.Ticks))
                {
                    if (!invalidState) throw new IOException();
                    ReportFailure();
                }
            }
            return TimeSpan.FromHours(48);
        }

        private static string FileName(DateTime now) { return "ai-gaming-" + now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log"; }
        private static void ReportFailure()
        {
            if (_reportedFailure) return;
            _reportedFailure = true;
            Debug.LogWarning("[AI Gaming] Diagnostic logging or cleanup unavailable.");
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

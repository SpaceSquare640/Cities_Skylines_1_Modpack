using System;
using System.IO;
using System.Runtime.InteropServices;
using AIGaming;

internal static class Program
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLinkW(string newName, string existingName, IntPtr security);
    private static void Main()
    {
        DateTime now = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
        Check(DiagnosticLogPolicy.ShouldDelete("ai-gaming-2026-10-26.log", "# AI Gaming diagnostic log v1", now.AddHours(-49), now, false), "old owned file");
        Check(!DiagnosticLogPolicy.ShouldDelete("ai-gaming-2026-10-26.log", "# AI Gaming diagnostic log v1", now.AddHours(-48), now, false), "boundary");
        Check(!DiagnosticLogPolicy.ShouldDelete("ai-gaming-2026-10-26.log", "other", now.AddHours(-49), now, false), "unowned file");
        Check(!DiagnosticLogPolicy.ShouldDelete("ai-gaming-2026-10-26.log", "# AI Gaming diagnostic log v1", now.AddHours(-49), now, true), "active file");
        Check(!DiagnosticLogPolicy.ShouldDelete("ai-gaming-2026-02-30.log", "# AI Gaming diagnostic log v1", now.AddHours(-49), now, false), "invalid date");
        Check(!DiagnosticLogPolicy.ShouldDelete("other-ai-gaming-2026-10-26.log", "# AI Gaming diagnostic log v1", now.AddHours(-49), now, false), "foreign name");
        Check(DiagnosticLogPolicy.ShouldLog("release", false) == false && DiagnosticLogPolicy.ShouldLog("development", false), "edition verbosity");
        Check(DiagnosticLogPolicy.ShouldLog("release", true), "release warning");
        Check(DiagnosticLogPolicy.GetCleanupDelay(now.Ticks + TimeSpan.FromHours(1).Ticks, now) == TimeSpan.Zero, "future state is due");
        Check(DiagnosticLogPolicy.GetCleanupDelay(now.AddHours(-47).Ticks, now) == TimeSpan.FromHours(1), "valid state delay");
        Check(DiagnosticLogPolicy.GetProviderLabel("com.spacesquare640.ai-improve") == "AI Improve", "AI Improve identity");
        Check(DiagnosticLogPolicy.GetProviderLabel("com.spacesquare640.automated-public-transport-planning") == "Transport", "transport identity");
        Check(DiagnosticLogPolicy.GetProviderLabel("untrusted\nsecret") == "unknown", "unrecognized identity is sanitized");
        InspectWindowsCandidateWithoutDeleting();
        Console.WriteLine("PASS: log retention and edition policy checks.");
    }

    private static void InspectWindowsCandidateWithoutDeleting()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
        string directory = Path.Combine(AppContext.BaseDirectory, "logging-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string name = "ai-gaming-2026-01-01.log";
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, DiagnosticLogPolicy.Marker + Environment.NewLine + "signed: OpenAI-Codex");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-49));
        Check(WindowsDiagnosticCleanup.InspectForCleanup(directory, name, DateTime.UtcNow), "verified old file");
        Check(File.Exists(path), "inspection must not delete");
        Check(!WindowsDiagnosticCleanup.InspectForCleanup(directory, "../" + name, DateTime.UtcNow), "path traversal");
        Check(File.Exists(path), "rejected traversal must not delete");
        Check(WindowsDiagnosticCleanup.TryAppend(directory, name, "test diagnostic"), "safe append");
        Check(File.ReadAllText(path).Contains("test diagnostic"), "append reaches owned file");
        string foreign = "ai-gaming-2026-01-02.log";
        File.WriteAllText(Path.Combine(directory, foreign), "not an owned log");
        Check(!WindowsDiagnosticCleanup.TryAppend(directory, foreign, "must not write"), "reject unowned log");
        Check(File.ReadAllText(Path.Combine(directory, foreign)) == "not an owned log", "preserve unowned log");
        Check(WindowsDiagnosticCleanup.TryWriteState(directory, 123), "safe state write");
        long ticks;
        Check(WindowsDiagnosticCleanup.TryReadState(directory, out ticks) && ticks == 123, "safe state read");
        CheckCorruptStateWithoutOverwrite();
        string[] candidates;
        Check(WindowsDiagnosticCleanup.TryEnumerate(directory, out candidates) && candidates.Length >= 2, "safe enumeration");
        string original = Path.Combine(directory, "original.log");
        string link = Path.Combine(directory, "ai-gaming-2026-01-03.log");
        File.WriteAllText(original, DiagnosticLogPolicy.Marker + "\noriginal");
        if (CreateHardLinkW(link, original, IntPtr.Zero))
        {
            Check(!WindowsDiagnosticCleanup.TryAppend(directory, Path.GetFileName(link), "must not write"), "reject hard link");
            Check(File.ReadAllText(original) == DiagnosticLogPolicy.Marker + "\noriginal", "preserve hard-linked file");
            Console.WriteLine("PASS: hard-link protection.");
        }
        else Console.WriteLine("SKIP: hard-link fixture unavailable.");
    }

    private static void CheckCorruptStateWithoutOverwrite()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "logging-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "cleanup.state");
        File.WriteAllText(path, "AI Gaming cleanup v1 bad-ticks");
        long ticks;
        bool parsed;
        Check(WindowsDiagnosticCleanup.TryReadStateStatus(directory, out ticks, out parsed) && !parsed, "detect corrupt owned state");
        Check(WindowsDiagnosticCleanup.TryWriteState(directory, 123), "repair marked state");
        Check(WindowsDiagnosticCleanup.TryReadState(directory, out ticks) && ticks == 123, "read repaired state");
        string other = Path.Combine(AppContext.BaseDirectory, "logging-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(other);
        string otherPath = Path.Combine(other, "cleanup.state");
        Check(WindowsDiagnosticCleanup.TryReadStateStatus(other, out ticks, out parsed) && !parsed, "missing state is due");
        File.WriteAllText(otherPath, "");
        Check(WindowsDiagnosticCleanup.TryReadStateStatus(other, out ticks, out parsed) && !parsed, "detect empty state");
        Check(!WindowsDiagnosticCleanup.TryWriteState(other, 123), "do not overwrite unowned state");
        Check(File.ReadAllText(otherPath) == "", "preserve empty state");
        string partialDirectory = Path.Combine(AppContext.BaseDirectory, "logging-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(partialDirectory);
        string partialPath = Path.Combine(partialDirectory, "cleanup.state");
        File.WriteAllText(partialPath, "AI Gaming cleanup");
        Check(WindowsDiagnosticCleanup.TryReadStateStatus(partialDirectory, out ticks, out parsed) && !parsed, "detect partial marker");
        Check(!WindowsDiagnosticCleanup.TryWriteState(partialDirectory, 123), "do not overwrite partial marker");
        Check(File.ReadAllText(partialPath) == "AI Gaming cleanup", "preserve partial marker");
        using (var locked = new FileStream(partialPath, FileMode.Open, FileAccess.Read, FileShare.None))
            Check(!WindowsDiagnosticCleanup.TryReadStateStatus(partialDirectory, out ticks, out parsed), "locked state fails closed");
        string futureDirectory = Path.Combine(AppContext.BaseDirectory, "logging-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(futureDirectory);
        DateTime now = DateTime.UtcNow;
        Check(WindowsDiagnosticCleanup.TryWriteState(futureDirectory, now.AddHours(1).Ticks), "create future state");
        Check(WindowsDiagnosticCleanup.TryReadStateStatus(futureDirectory, out ticks, out parsed) && parsed &&
            DiagnosticLogPolicy.GetCleanupDelay(ticks, now) == TimeSpan.Zero, "future state triggers check");
        Check(WindowsDiagnosticCleanup.TryWriteState(futureDirectory, now.Ticks), "repair future state");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace AIGaming.Companion;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 0 || !OperatingSystem.IsWindows() || IntPtr.Size != 8)
        {
            return 2;
        }

        try
        {
            return RunAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return 1;
        }
    }

    private static async Task<int> RunAsync()
    {
        string? bootstrapText = await Console.In.ReadLineAsync().ConfigureAwait(false);
        if (bootstrapText == null || !SessionProtocol.TryParseBootstrap(bootstrapText, out CompanionBootstrap bootstrap))
        {
            return 3;
        }

        if (!IsValidIdentity(bootstrap, out string executablePath, out string executableHash, out int actualParentProcessId))
        {
            return 4;
        }

        byte[] secret = RandomNumberGenerator.GetBytes(32);
        using var provider = new OllamaLocalProvider();
        using var host = new LoopbackHost(secret, CompanionBuildInfo.Edition, provider);
        using var session = new CancellationTokenSource();
        host.Start();

        string ready = SessionProtocol.SerializeReady(
            host.Port,
            Environment.ProcessId,
            actualParentProcessId,
            executableHash,
            secret);
        await Console.Out.WriteLineAsync(ready).ConfigureAwait(false);
        await Console.Out.FlushAsync().ConfigureAwait(false);

        Task hostTask = host.RunAsync(session.Token);
        Task<string?> parentPipeClosed = Task.Run(() => Console.In.ReadLineAsync());
        Task finished = await Task.WhenAny(hostTask, parentPipeClosed).ConfigureAwait(false);
        if (finished == parentPipeClosed && parentPipeClosed.IsCompletedSuccessfully && parentPipeClosed.Result != null)
        {
            session.Cancel();
        }
        else if (finished == parentPipeClosed)
        {
            session.Cancel();
        }

        host.Stop();
        session.Cancel();
        try { await hostTask.ConfigureAwait(false); }
        catch (OperationCanceledException) { }

        return 0;
    }

    private static bool IsValidIdentity(
        CompanionBootstrap bootstrap,
        out string executablePath,
        out string executableHash,
        out int actualParentProcessId)
    {
        executablePath = string.Empty;
        executableHash = string.Empty;
        actualParentProcessId = 0;

        if (!string.Equals(bootstrap.ProtocolVersion, CompanionBuildInfo.ProtocolVersion, StringComparison.Ordinal) ||
            !string.Equals(bootstrap.Edition, CompanionBuildInfo.Edition, StringComparison.Ordinal) ||
            !string.Equals(bootstrap.ModVersion, CompanionBuildInfo.Version, StringComparison.Ordinal) ||
            !string.Equals(bootstrap.CompanionVersion, CompanionBuildInfo.Version, StringComparison.Ordinal) ||
            !SessionProtocol.TryParseHex(bootstrap.ExpectedSha256, out _))
        {
            return false;
        }

        try
        {
            actualParentProcessId = ParentProcessIdentity.GetParentProcessId();
            if (actualParentProcessId != bootstrap.ParentProcessId)
            {
                return false;
            }

            using Process parent = Process.GetProcessById(actualParentProcessId);
            executablePath = Path.GetFullPath(parent.MainModule?.FileName ?? string.Empty);
            string expectedPath = Path.GetFullPath(bootstrap.ParentImagePath);
            if (!string.Equals(executablePath, expectedPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileName(executablePath), "Cities.exe", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string ownPath = Environment.ProcessPath ?? string.Empty;
            if (!File.Exists(ownPath))
            {
                return false;
            }

            using FileStream executable = File.OpenRead(ownPath);
            executableHash = Convert.ToHexString(SHA256.HashData(executable)).ToLowerInvariant();
            return string.Equals(executableHash, bootstrap.ExpectedSha256, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            executablePath = string.Empty;
            executableHash = string.Empty;
            actualParentProcessId = 0;
            return false;
        }
    }
}

internal static class ParentProcessIdentity
{
    private const int ProcessBasicInformation = 0;

    [DllImport("ntdll.dll", SetLastError = false)]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        out ProcessBasicInformationData processInformation,
        uint processInformationLength,
        out uint returnLength);

    internal static int GetParentProcessId()
    {
        using Process current = Process.GetCurrentProcess();
        int status = NtQueryInformationProcess(
            current.Handle,
            ProcessBasicInformation,
            out ProcessBasicInformationData information,
            (uint)Marshal.SizeOf<ProcessBasicInformationData>(),
            out _);
        if (status != 0 || information.InheritedFromUniqueProcessId == IntPtr.Zero)
        {
            throw new InvalidOperationException();
        }

        long processId = information.InheritedFromUniqueProcessId.ToInt64();
        if (processId <= 0 || processId > int.MaxValue)
        {
            throw new InvalidOperationException();
        }
        return (int)processId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformationData
    {
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;
    }
}
// Implementation update signed by OpenAI-Codex.

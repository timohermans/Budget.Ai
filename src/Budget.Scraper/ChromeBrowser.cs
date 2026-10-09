using System.Diagnostics;
using Microsoft.Playwright;

namespace Budget.Scraper;

public class ChromeBrowser : IAsyncDisposable
{
    private const string ProcessName = "Google Chrome";

    private readonly string _browserPath;
    private readonly string _cdpPort;
    private IPlaywright? _playwright;
    private Process? _process = null;
    private static Process? _process2 = null;

    public ChromeBrowser(string browserPath, string cdpPort)
    {
        _browserPath = browserPath;
        _cdpPort = cdpPort;
    }

    public string CdpUrl => $"http://127.0.0.1:{_cdpPort}";

    private static string UserDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".budget-scraper-chrome");

    private static string DefaultChromeUserDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "Application Support", "Google", "Chrome");

    private static readonly string[] ExcludedDirNames =
    [
        "Cache", "Code Cache", "GPUCache", "GrShaderCache", "ShaderCache",
        "Service Worker", "Crashpad", "SafetyNet", "Download Service"
    ];

    private static void EnsureUserDataDir()
    {
        if (Directory.Exists(UserDataDir) && File.GetAttributes(UserDataDir).HasFlag(FileAttributes.ReparsePoint))
        {
            Directory.Delete(UserDataDir);
        }

        if (Directory.Exists(UserDataDir))
        {
            return;
        }

        Log.Information("Copying Chrome profile to {Target} (one-time)...", UserDataDir);
        CopyDirectory(new DirectoryInfo(DefaultChromeUserDataDir), new DirectoryInfo(UserDataDir), top: true);
        Log.Information("Chrome profile copied.");
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target, bool top)
    {
        target.Create();

        foreach (var entry in source.EnumerateFileSystemInfos())
        {
            if (!top && entry is DirectoryInfo { Name: var name } && ExcludedDirNames.Contains(name))
            {
                continue;
            }

            var destination = Path.Combine(target.FullName, entry.Name);
            switch (entry)
            {
                case FileInfo file:
                    file.CopyTo(destination, overwrite: true);
                    break;
                case DirectoryInfo dir:
                    CopyDirectory(dir, new DirectoryInfo(destination), top: false);
                    break;
            }
        }
    }

    public bool LaunchedChrome { get; private set; }

    public async Task<IBrowser?> LaunchOrConnectAsync()
    {
        _playwright ??= await Playwright.CreateAsync();

        if (await IsCdpUpAsync())
        {
            Log.Debug("CDP is already running");
            return await _playwright.Chromium.ConnectOverCDPAsync(CdpUrl);
        }

        await ForceCloseIfRunningAsync();

        StartChrome();

        var up = false;
        for (var i = 0; i < 60 && !up; i++)
        {
            await Task.Delay(500);
            up = await IsCdpUpAsync();
        }

        if (!up)
        {
            Log.Fatal($"Could not reach {CdpUrl}, even after closing Chrome and relaunching it with the debugging port.");
            return null;
        }

        return await _playwright.Chromium.ConnectOverCDPAsync(CdpUrl);
    }

    public async Task QuitAsync()
    {
        RunProcess("/usr/bin/osascript", "-e", "quit app \"Google Chrome\"");
        if (await WaitUntilClosedAsync(20))
        {
            return;
        }

        RunProcess("/usr/bin/pkill", "-x", ProcessName);
        if (await WaitUntilClosedAsync(10))
        {
            return;
        }

        RunProcess("/usr/bin/pkill", "-9", "-x", ProcessName);
        await WaitUntilClosedAsync(10);
    }

    public async ValueTask DisposeAsync()
    {
        _process?.Dispose();
        _process2?.Dispose();
        if (_playwright is null)
        {
            return;
        }

        if (_playwright is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_playwright is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await QuitAsync();
    }

    private async Task ForceCloseIfRunningAsync()
    {
        if (Process.GetProcessesByName(ProcessName).Length == 0)
        {
            return;
        }

        Log.Fatal("Chrome is running without the debugging port — closing it first.");
        await QuitAsync();

        if (Process.GetProcessesByName(ProcessName).Length > 0)
        {
            Log.Fatal("Chrome did not fully quit; not launching a new instance.");
            throw new InvalidOperationException("Chrome did not quit.");
        }
    }

    private async Task<bool> WaitUntilClosedAsync(int attempts)
    {
        for (var i = 0; i < attempts; i++)
        {
            await Task.Delay(500);
            if (Process.GetProcessesByName(ProcessName).Length == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void RunProcess(string file, params string[] arguments)
    {
        var info = new ProcessStartInfo(file) { UseShellExecute = false };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        var process2 = Process.Start(info);
        _process2 = process2;
    }

    private void StartChrome()
    {
        LaunchedChrome = true;
        EnsureUserDataDir();

        var info = new ProcessStartInfo(_browserPath) { UseShellExecute = false };
        info.ArgumentList.Add($"--remote-debugging-port={_cdpPort}");
        info.ArgumentList.Add($"--user-data-dir={UserDataDir}");

        var process = Process.Start(info);
        _process = process;
    }

    private async Task<bool> IsCdpUpAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
        try
        {
            using var response = await client.GetAsync($"{CdpUrl}/json/version");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
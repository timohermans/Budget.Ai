using System.Diagnostics;
using Microsoft.Playwright;

namespace Budget.Scraper;

public class BraveBrowser : IAsyncDisposable
{
    private const string ProcessName = "Brave Browser";

    private readonly string _browserPath;
    private readonly string _cdpPort;
    private readonly string? _profileDirectory;
    private IPlaywright? _playwright;
    private Process? _process = null;
    private static Process? _process2 = null;

    public BraveBrowser(string browserPath, string cdpPort, string? profileDirectory)
    {
        _browserPath = browserPath;
        _cdpPort = cdpPort;
        _profileDirectory = profileDirectory;
    }

    public string CdpUrl => $"http://127.0.0.1:{_cdpPort}";

    public bool LaunchedBrave { get; private set; }

    public async Task<IBrowser?> LaunchOrConnectAsync()
    {
        _playwright ??= await Playwright.CreateAsync();

        if (await IsCdpUpAsync())
        {
            return await _playwright.Chromium.ConnectOverCDPAsync(CdpUrl);
        }

        await ForceCloseIfRunningAsync();

        StartBrave();

        var up = false;
        for (var i = 0; i < 60 && !up; i++)
        {
            await Task.Delay(500);
            up = await IsCdpUpAsync();
        }

        if (!up)
        {
            Console.WriteLine($"Could not reach {CdpUrl}, even after closing Brave and relaunching it with the debugging port.");
            return null;
        }

        return await _playwright.Chromium.ConnectOverCDPAsync(CdpUrl);
    }

    public async Task QuitAsync()
    {
        RunProcess("/usr/bin/osascript", "-e", "quit app \"Brave Browser\"");
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
    }

    private async Task ForceCloseIfRunningAsync()
    {
        if (Process.GetProcessesByName(ProcessName).Length == 0)
        {
            return;
        }

        Console.WriteLine("Brave is running without the debugging port — closing it first.");
        await QuitAsync();
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

    private void StartBrave()
    {
        LaunchedBrave = true;

        var info = new ProcessStartInfo(_browserPath) { UseShellExecute = false };
        info.ArgumentList.Add($"--remote-debugging-port={_cdpPort}");
        if (_profileDirectory is not null)
        {
            info.ArgumentList.Add("--profile-directory");
            info.ArgumentList.Add(_profileDirectory);
        }

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
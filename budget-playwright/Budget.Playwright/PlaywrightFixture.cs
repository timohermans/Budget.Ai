using Microsoft.Playwright;

namespace Budget.Playwright;

public class PlaywrightFixture : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _initialized;

    private static readonly SemaphoreSlim InitLock = new(1, 1);

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
            });
            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public async Task<IPage> NewPageAsync(Dictionary<string, string>? headers = null)
    {
        await InitializeAsync();
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "nl-NL",
        });

        if (headers != null)
            await context.SetExtraHTTPHeadersAsync(headers);

        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}

using Budget.E2e.Support;
using Microsoft.Playwright;

namespace Budget.E2e;

public abstract class PlaywrightTestBase
{
    private static readonly PlaywrightFixture Fixture = new();
    private static bool _initialized;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    protected IPage Page { get; private set; } = null!;
    protected Guid UserId { get; private set; }

    [TestInitialize]
    public async Task TestInit()
    {
        if (!_initialized)
        {
            await InitLock.WaitAsync();
            try
            {
                if (!_initialized)
                {
                    await Fixture.InitializeAsync();
                    _initialized = true;
                }
            }
            finally
            {
                InitLock.Release();
            }
        }

        UserId = Guid.NewGuid();
        Page = await Fixture.NewPageAsync(new Dictionary<string, string>
        {
            ["X-Test-User"] = UserId.ToString()
        });
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        if (Page != null)
            await Page.CloseAsync();
    }

    protected async Task UploadCsvAsync(List<TestTransaction> transactions)
    {
        var csv = new CsvBuilder();
        foreach (var t in transactions)
            csv.Add(t);
        var csvBytes = csv.BuildBytes();

        var headers = new Dictionary<string, string>
        {
            ["X-Test-User"] = UserId.ToString()
        };

        var formData = Page.APIRequest.CreateFormData();
        formData.Set("file", new FilePayload
        {
            Name = "test.csv",
            MimeType = "text/csv",
            Buffer = csvBytes
        });

        var response = await Page.APIRequest.PostAsync(Routes.Upload, new()
        {
            Headers = headers,
            Multipart = formData,
            MaxRedirects = 0
        });

        Assert.AreEqual(302, response.Status,
            $"Upload expected 302 redirect but got {response.Status}");
    }

    protected static async Task<decimal> ParseCurrency(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var cleaned = text.Replace("€", "").Replace(".", "").Replace(",", ".").Trim();
        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}

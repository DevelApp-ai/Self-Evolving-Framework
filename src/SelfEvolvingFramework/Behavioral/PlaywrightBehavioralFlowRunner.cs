using Microsoft.Playwright;

namespace SelfEvolvingFramework.Behavioral;

public delegate Task PlaywrightBehavioralFlowScript(IPage page, CancellationToken cancellationToken);

public interface IPlaywrightBehavioralFlowRunner
{
    Task<IReadOnlyList<string>> RunAsync(Uri endpoint, CancellationToken cancellationToken = default);
}

public sealed class PlaywrightBehavioralFlowRunner(PlaywrightBehavioralFlowScript flowScript) : IPlaywrightBehavioralFlowRunner
{
    private readonly PlaywrightBehavioralFlowScript _flowScript = flowScript ?? throw new ArgumentNullException(nameof(flowScript));

    public async Task<IReadOnlyList<string>> RunAsync(Uri endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var diagnostics = new List<string>();
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                diagnostics.Add($"console: {message.Text}");
            }
        };

        page.PageError += (_, message) =>
        {
            diagnostics.Add($"page-error: {message}");
        };

        page.RequestFailed += (_, request) =>
        {
            diagnostics.Add($"request-failed: {request.Url}");
        };

        try
        {
            await page.GotoAsync(endpoint.ToString());
            await _flowScript(page, cancellationToken);
        }
        catch (Exception ex)
        {
            diagnostics.Add($"flow-failed: {ex.Message}");
        }
        finally
        {
            await context.CloseAsync();
            await browser.CloseAsync();
        }

        return diagnostics;
    }
}

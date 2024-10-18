using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SearchEngine.Server.Pages;

/// <summary>
/// Default error handling page.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    /// <summary>
    /// The ID from the request that caused the error.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// If we should show the request.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>
    /// Logger, currently not being used.
    /// </summary>
    private readonly ILogger<ErrorModel> _logger;

    /// <summary>
    /// Constructor which can pass in dependencies.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Getter.
    /// </summary>
    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
using Microsoft.AspNetCore.Mvc;
using SearchEngine.Lucene;
using SearchEngine.Shared;

namespace SearchEngine.Server.Controllers;

/// <summary>
/// API for handling getting requests from the frontend and loading them from Lucene.
/// </summary>
[ApiController]
[Route("[controller]")]
public class SearchEngineController : ControllerBase
{
    /// <summary>
    /// Logger, currently not being used.
    /// </summary>
    private readonly ILogger<SearchEngineController> _logger;

    /// <summary>
    /// Constructor which can pass in dependencies.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public SearchEngineController(ILogger<SearchEngineController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Our primary (and currently only) API get method.
    /// </summary>
    /// <param name="query">The search query from the user.</param>
    /// <param name="id">The ID for searching for a similar document.</param>
    /// <returns>The best results for the query.</returns>
    [HttpGet]
    public IEnumerable<SearchDocument> Get([FromQuery] string? query = null, [FromQuery] int? id = null)
    {
        return Core.Search(query, id);
    }
}
using Microsoft.AspNetCore.Mvc;
using SearchEngine.Shared;

namespace SearchEngine.Server.Controllers;

/// <summary>
/// API for handling getting requests from the frontend and loading them from Embeddings.
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
    /// <param name="start">The starting index for searching.</param>
    /// <param name="count">The number of results to get.</param>
    /// <returns>The best results for the query.</returns>
    [HttpGet]
    public async Task<QueryResult> Get([FromQuery] string? query = null, [FromQuery] string? id = null, [FromQuery] int? start = 0, [FromQuery] int? count = Values.SearchCount)
    {
        return await Embeddings.Search(query, id, start ?? 0, count ?? Values.SearchCount);
    }
}
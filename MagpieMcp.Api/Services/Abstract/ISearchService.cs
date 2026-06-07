using MagpieMcp.Api.Models.Configuration;

namespace MagpieMcp.Api.Services.Abstract;

public interface ISearchService
{
    Task<string> SearchAsync(string query, McpToolModel mcpTool, CancellationToken cancellationToken = default);
}

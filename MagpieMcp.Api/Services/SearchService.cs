using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using MagpieMcp.Api.Models.Configuration;
using MagpieMcp.Api.Services.Abstract;

namespace MagpieMcp.Api.Services;

public partial class SearchService(IHttpClientFactory httpClientFactory, ILogger<SearchService> logger) : ISearchService
{
    public async Task<string> SearchAsync(string query, McpToolModel mcpTool, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mcpTool?.UrlTemplate) || !mcpTool.Enabled)
        {
            return "Validation error";
        }

        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = string.Format(mcpTool.UrlTemplate, encodedQuery);

        using var client = httpClientFactory.CreateClient("search");

        // Apply custom headers from configuration
        if (mcpTool.Headers is { Count: > 0 })
        {
            foreach (var header in mcpTool.Headers)
            {
                if (!client.DefaultRequestHeaders.Contains(header.Key))
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        logger.LogInformation("Searching {ToolName} with url: {Url}", mcpTool.Name, url);

        await Task.Delay(100, cancellationToken); // rate-limit

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        // If the response is JSON (SearXNG format), parse it properly
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("format=json", StringComparison.OrdinalIgnoreCase))
        {
            return await ParseJsonResultsAsync(response, mcpTool.Name, query, cancellationToken);
        }

        // Fallback: regex-based HTML scraping for custom tools
        return await ParseHtmlResultsAsync(response, mcpTool, cancellationToken);
    }

    private async Task<string> ParseJsonResultsAsync(
        HttpResponseMessage response, string toolName, string query, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            logger.LogInformation("No JSON results for {ToolName}: {Query}", toolName, query);
            return "No results found.";
        }

        var output = new List<string>();
        var count = 0;

        foreach (var result in results.EnumerateArray())
        {
            if (count >= 10) break;

            var title = result.TryGetProperty("title", out var t) ? t.GetString() : "";
            var resultUrl = result.TryGetProperty("url", out var u) ? u.GetString() : "";
            var snippet = result.TryGetProperty("content", out var c) ? c.GetString() : "";

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(resultUrl))
                continue;

            output.Add($"{count + 1}. [{title}]({resultUrl})\n   {snippet}");
            count++;
        }

        if (output.Count == 0)
            return "No results found.";

        logger.LogInformation("Found {Count} results for {ToolName}: {Query}", output.Count, toolName, query);
        return string.Join("\n\n", output);
    }

    private async Task<string> ParseHtmlResultsAsync(
        HttpResponseMessage response, McpToolModel mcpTool, CancellationToken cancellationToken)
    {
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(mcpTool.ResultLinkRegex))
        {
            return html;
        }

        var matches = Regex.Matches(html, mcpTool.ResultLinkRegex, RegexOptions.Singleline);

        if (matches.Count == 0)
        {
            logger.LogInformation("No regex matches for {ToolName}", mcpTool.Name);
            return "No results found.";
        }

        var results = matches
            .Take(10)
            .Select((m, i) =>
            {
                var title = StripHtml(m.Groups["title"].Value);
                var urlResult = HttpUtility.HtmlDecode(m.Groups["url"].Value);
                var snippet = StripHtml(m.Groups["snippet"].Value);
                return $"{i + 1}. [{title}]({urlResult})\n   {snippet}";
            });

        return string.Join("\n\n", results);
    }

    private static string StripHtml(string input)
    {
        var text = HtmlTagRegex().Replace(input, string.Empty);
        return HttpUtility.HtmlDecode(text).Trim();
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagRegex();
}

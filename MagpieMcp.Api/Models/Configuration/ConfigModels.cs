namespace MagpieMcp.Api.Models.Configuration;

public class DefaultsModel
{
    public string RequestUserAgent { get; set; } = string.Empty;
}

public class CorsSettingsModel
{
    public bool Enabled { get; set; }
    public bool AllowAnyOrigin { get; set; }
    public bool AllowAnyMethod { get; set; }
    public bool AllowAnyHeader { get; set; }
}

public class McpToolsModel
{
    public IReadOnlyCollection<McpToolModel> Tools { get; set; } = [];
}

public class McpToolModel
{
    public bool Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UrlTemplate { get; set; } = string.Empty;
    public string ResultLinkRegex { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
}

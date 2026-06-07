using System.ComponentModel;
using MagpieMcp.Api.Models.Configuration;
using MagpieMcp.Api.Services;
using MagpieMcp.Api.Services.Abstract;
using ModelContextProtocol.Server;

namespace MagpieMcp.Api;

public static class Startup
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Defaults settings
        var defaultsSettings = new DefaultsModel();
        configuration.GetSection("Defaults").Bind(defaultsSettings);
        services.AddSingleton(defaultsSettings);

        // Register CORS settings
        var corsSettings = new CorsSettingsModel();
        configuration.GetSection("Cors").Bind(corsSettings);
        services.AddSingleton(corsSettings);

        // Register MCP tools
        var mcpTools = new McpToolsModel();
        configuration.GetSection("McpTools").Bind(mcpTools);

        // Filter tools to only include enabled tools with valid headers
        mcpTools.Tools = [.. mcpTools.Tools.Where(t => t.Enabled)];

        foreach (var tool in mcpTools.Tools)
        {
            tool.Headers = tool.Headers
                .Where(h => !string.IsNullOrWhiteSpace(h.Key))
                .ToDictionary();
        }
        services.AddSingleton(mcpTools);


        // Search service
        services.AddHttpClient("search", httpClient =>
            {
                if (!string.IsNullOrWhiteSpace(defaultsSettings.RequestUserAgent))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(defaultsSettings.RequestUserAgent);
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    UseCookies = false
                });
        services.AddSingleton<ISearchService, SearchService>();

        // Add CORS Services
        if (corsSettings.Enabled)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    if (corsSettings.AllowAnyOrigin)
                        builder.AllowAnyOrigin();
                    if (corsSettings.AllowAnyMethod)
                        builder.AllowAnyMethod();
                    if (corsSettings.AllowAnyHeader)
                        builder.AllowAnyHeader();
                });
            });
        }

        // MCP Server with dynamic tool registration
        var mcpBuilder = services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true);

        // Dynamically register a tool for each enabled config entry.
        // We use the service provider created earlier to let McpServerTool.Create
        // know which parameters are DI-resolvable (excluded from the tool's JSON schema).


        // Build service provider to resolve configuration instances
        #pragma warning disable ASP0000 // Intentional: needed to resolve configuration
        var serviceProvider = services.BuildServiceProvider();
        #pragma warning restore ASP0000

        var dynamicTools = mcpTools.Tools.Select(toolConfig =>
        {
            Task<string> SearchDelegate(
                ISearchService searchService,
                [Description("The search query")] string query,
                CancellationToken cancellationToken)
            {
                return searchService.SearchAsync(query, toolConfig, cancellationToken);
            }

            return McpServerTool.Create(
                (Delegate)SearchDelegate,
                new McpServerToolCreateOptions
                {
                    Name = toolConfig.Name,
                    Description = toolConfig.Description,
                    Services = serviceProvider
                });
        }).ToList();

        if (dynamicTools.Count > 0)
        {
            mcpBuilder.WithTools(dynamicTools);
        }
    }
}
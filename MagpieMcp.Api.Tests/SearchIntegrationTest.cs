using MagpieMcp.Api.Constants;
using MagpieMcp.Api.Models.Configuration;
using MagpieMcp.Api.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace MagpieMcp.Api.Tests;

public class SearchIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private IHost _appHost;
    private McpToolsModel _mcpTools;
    private ISearchService _searchService;

    public SearchIntegrationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Setup();
    }

    private void Setup(string env = null)
    {
        _appHost = Host.CreateDefaultBuilder()
            .UseEnvironment(Environment.GetEnvironmentVariable(EnvVarNames.DotNetEnv) ?? env ?? Environments.Development)
            .ConfigureServices((context, services) => services.AddAppServices(context.Configuration))
            .Build();

        _mcpTools = _appHost.Services.GetRequiredService<McpToolsModel>();
        _searchService = _appHost.Services.GetRequiredService<ISearchService>();
    }

    [Fact]
    public async Task Test1()
    {
        _testOutputHelper.WriteLine($"_searchService: {_searchService}");

        foreach (var tool in _mcpTools.Tools)
        {

            try
            {
                var searchResult = await _searchService.SearchAsync("magpie", tool);

                Assert.NotNull(searchResult);

                _testOutputHelper.WriteLine($"searchResult: {searchResult}");
            }
            catch (Exception e)
            {
                Assert.True(e.ToString().Contains("Connection refused"));
            }

        }
    }
}

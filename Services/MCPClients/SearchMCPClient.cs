using ModelContextProtocol.Client;
using System.Text.RegularExpressions;

namespace Services.MCPClients
{
    public class SearchMCPClient : IMCPClientBuilder
    {
        private readonly IMcpClient _client;
        public SearchMCPClient()
        {
            _client = McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new("https://router.mcp.so/mcp/dp49w4mauy9dij"),
                Name = "Search",
                TransportMode = HttpTransportMode.StreamableHttp
            })).Result;
        }

        public string Name => "Search";

        async ValueTask<IList<McpClientTool>> IMCPClientBuilder.GetTools() =>
            [.. (await _client.ListToolsAsync()).Select(tool => tool.WithName(Regex.Replace(tool.Name, "[^a-zA-Z0-9]", "")))];

    }
}

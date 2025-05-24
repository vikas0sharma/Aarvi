using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace Services.MCPClients
{
    public class YouTubeMusicClient : IMCPClientBuilder
    {
        private readonly IMcpClient _client;
        public YouTubeMusicClient(IConfiguration configuration)
        {
            _client = McpClientFactory.CreateAsync(new StdioClientTransport(new()
            {
                Name = "YouTubeMusic",
                Command = "npx",
                Arguments = ["-y", "@instructa/mcp-youtube-music"],
                EnvironmentVariables = new Dictionary<string, string>()
                {
                    { "YOUTUBE_API_KEY", configuration["YOUTUBE_API_KEY"]! },

                }!,
            })).Result;
        }

        public string Name => "YouTubeMusic";

        ValueTask<IList<McpClientTool>> IMCPClientBuilder.GetTools() => _client.ListToolsAsync();


    }
}

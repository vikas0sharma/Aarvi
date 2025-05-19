using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.MCPClients
{
    public class TimeClient : IMCPClientBuilder
    {
        private readonly IMcpClient _client;
        public TimeClient()
        {
            _client = McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions { Endpoint = new("https://router.mcp.so/mcp/p6588zmatl2p3b"), Name = "Time", UseStreamableHttp = true })).Result;
        }

        public string Name => "Time";

        ValueTask<IList<McpClientTool>> IMCPClientBuilder.GetTools() => _client.ListToolsAsync();


    }
}

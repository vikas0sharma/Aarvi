using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.MCPClients
{
    public interface IMCPClientBuilder
    {
        public string Name { get; }
        public ValueTask<IList<McpClientTool>> GetTools();
    }
}

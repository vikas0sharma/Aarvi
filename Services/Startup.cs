using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Services.MCPClients;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Services
{
    public static class Startup
    {
        public static void ConfigureService(this IServiceCollection services)
        {
            //services.AddScoped<IMCPClientBuilder, TimeClient>();
            services.AddScoped<IMCPClientBuilder, SearchMCPClient>();

            services.AddScoped(sp =>
            {
                var allMCPs = sp.GetServices<IMCPClientBuilder>().ToArray();

                var builder = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion("gemini-2.0-flash", "");

                builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
                var kernel = builder.Build();

                foreach (var mcp in allMCPs)
                {
                    var tools = mcp.GetTools().Result;
                    kernel.Plugins.AddFromFunctions(mcp.Name, tools.Select(tool => tool.AsKernelFunction()));

                }

                return kernel;
            });

        }
    }

    
}

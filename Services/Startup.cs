using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Services.MCPClients;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public static class Startup
    {
        public static void ConfigureService(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Google Gemini AI service
            services.AddGoogleAIGeminiChatCompletion("gemini-2.0-flash", configuration["GEMINI_KEY"]!);
            // Add MCP Tools
            services.AddScoped<IMCPClientBuilder, YouTubeMusicClient>();
            services.AddScoped<IMCPClientBuilder, SearchMCPClient>();
            services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
            // Add Semantic Kernel services
            services.AddScoped(sp =>
            {
                Kernel kernel = new(sp, []);

                var allMCPs = sp.GetServices<IMCPClientBuilder>().ToArray();


                foreach (var mcp in allMCPs)
                {
                    var tools = mcp.GetTools().Result;

                    kernel.Plugins.AddFromFunctions(mcp.Name, tools.Select(tool =>
                    {
                        return tool.AsKernelFunction();
                    }));

                }

                return kernel;
            });

        }


    }


}

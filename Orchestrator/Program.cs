// See https://aka.ms/new-console-template for more information
using A2A.Client;
using A2A.Client.Configuration;
using A2A.Client.Services;
using A2A.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Spectre.Console;

IConfigurationBuilder confBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddUserSecrets<Program>();

IConfiguration configuration = confBuilder.Build();


IKernelBuilder builder = Kernel.CreateBuilder()
    .AddGoogleAIGeminiChatCompletion("gemini-2.0-flash", configuration["GEMINI_KEY"]!);

builder.Services.AddHttpClient();
builder.Services.AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace));
Kernel kernel = builder.Build();

var server = new Uri("http://localhost:5183");
var httpFactory = kernel.Services.GetService<IHttpClientFactory>()!;
using var httpClient = httpFactory.CreateClient("A2AProtocolDiscoverer");
var discoveryDocument = await httpClient.GetA2ADiscoveryDocumentAsync(server);
var agent = discoveryDocument.Agents[0];
var agentName = agent.Name.Replace(" ", string.Empty);
var endpoint = agent.Url.IsAbsoluteUri ? agent.Url : new(server, agent.Url);
kernel.ImportPluginFromFunctions(agentName, [
    .. agent.Skills.Select(skill => {
        return kernel.CreateFunctionFromMethod(async (string prompt) => {
            var client = new A2AProtocolHttpClient(Options.Create(new A2AProtocolClientOptions { Endpoint = endpoint }), httpFactory.CreateClient(agent.Name));
            var resp = await client.SendMessageAsync(new A2A.Requests.SendMessageRequest { Params = new SendMessageRequestParameters {  Message = new Message { Role = A2A.MessageRole.User, Parts = [new TextPart(prompt)] } } }, default).ConfigureAwait(false);
            return ((TextPart)resp.Result?.Status?.Message?.Parts?.LastOrDefault()).Text ?? string.Empty;
        }, skill.Name, skill.Description, returnParameter: new() { Description = "Prompt response as a JSON object or array to be inferred upon.", ParameterType = typeof(string) });
    })
]);


ChatCompletionAgent chatCompletionAgent = new ChatCompletionAgent
{
    Name = "OrchestratorAgent",
    Instructions = "You are an helpful agent that will use its tools to answer user queries",
    Kernel = kernel,
    Arguments = new KernelArguments(new GeminiPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
    })
};

bool isComplete = false;
//ChatHistoryAgentThread agentThread = new() { };
AnsiConsole.Write(new Rule("Orchestrator Chat").RuleStyle("grey").Centered());
do
{
    // Draw user prompt
    // Prompt the user for input
    string input = AnsiConsole.Prompt(new TextPrompt<string>("[red]User > [/]"));


    if (string.IsNullOrEmpty(input)) continue;
    if (input.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase))
    {
        isComplete = true;
        break;
    }

    ChatMessageContent message = new(AuthorRole.User, input);
    await AnsiConsole.Status()
        .AutoRefresh(true)
        .Spinner(Spinner.Known.Dots)
        .StartAsync("[yellow]Thinking...[/]", async ctx =>
        {
            await foreach (var agentResponse in chatCompletionAgent.InvokeAsync(message))
            {
                // Stop the status/spinner before writing output
                ctx.Status("[yellow]Responding...[/]");
                AnsiConsole.MarkupLine($"[green]Agent > [/]{agentResponse.Message.Content}");
            }
        });
    // Force clear the line to remove any residual "Thinking..." text
    Console.SetCursorPosition(0, Console.CursorTop - 1);
    Console.Write(new string(' ', Console.WindowWidth));
    Console.SetCursorPosition(0, Console.CursorTop);


} while (!isComplete);

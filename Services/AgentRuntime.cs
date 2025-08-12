using A2A.Models;
using A2A.Server.Infrastructure;
using A2A.Server.Infrastructure.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Neuroglia;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace Services
{
    public class AgentRuntime(Kernel _kernel) : IAgentRuntime
    {

        protected ConcurrentDictionary<string, ChatHistory> Sessions { get; } = [];
        protected ConcurrentDictionary<string, CancellationTokenSource> Tasks { get; } = [];


        System.Threading.Tasks.Task IAgentRuntime.CancelAsync(string taskId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        async IAsyncEnumerable<AgentResponseContent> IAgentRuntime.ExecuteAsync(TaskRecord task, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            var settings = new GeminiPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            };
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Tasks.AddOrUpdate(task.Id, cancellationTokenSource, (id, existing) =>
            {
                if (!existing.IsCancellationRequested)
                {
                    existing.Cancel();
                    try
                    {
                        existing.Dispose();
                    }
                    catch (ObjectDisposedException) { }
                }
                return cancellationTokenSource;
            });
            if (!Sessions.TryGetValue(task.ContextId, out var session) || session == null)
            {
                session = new ChatHistory();
                Sessions.AddOrUpdate(task.ContextId, session, (id, existing) => existing);
            }
            session.AddUserMessage(task.Message.ToText() ?? string.Empty);

            string? currentRole = null;
            var currentContent = new StringBuilder();
            EquatableDictionary<string, object>? metadata = null;
            uint index = 0;
            await foreach (var (content, next) in chatCompletionService.GetStreamingChatMessageContentsAsync(session, settings, _kernel, cancellationTokenSource.Token).PeekingAsync(cancellationTokenSource.Token))
            {
                var role = content.Role?.ToString();
                if (role != null && role != currentRole)
                {
                    if (currentContent.Length > 0 && currentRole != null)
                    {
                        yield return new MessageResponseContent(new Message
                        {
                            Metadata = metadata,
                            Parts = [new TextPart(currentContent.ToString())],
                            Role = "agent"

                        });
                        currentContent.Clear();
                        metadata = null;
                    }
                    currentRole = role;
                }
                if (!string.IsNullOrEmpty(content.Content)) currentContent.Append(content.Content);
                if (content.Metadata is { Count: > 0 })
                {
                    metadata ??= [];
                    foreach (var kvp in content.Metadata) metadata[kvp.Key] = kvp.Value!;
                }
                if (next == null || (!string.IsNullOrWhiteSpace(next?.Role?.ToString()) && next?.Role?.ToString() != currentRole && currentContent.Length > 0 && currentRole != null))
                {
                    yield return new MessageResponseContent(new Message
                    {
                        Metadata = metadata,
                        Parts = [new TextPart(currentContent.ToString())],
                        Role = "agent"

                    });
                    currentContent.Clear();
                    metadata = null;
                }
            }
        }
    }
}

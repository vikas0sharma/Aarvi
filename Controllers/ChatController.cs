using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using ModelContextProtocol.Client;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Aarvi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;

        public ChatController(Kernel kernel)
        {
            _kernel = kernel;
        }

        //[HttpPost("invoke")]
        //public async Task<IActionResult> InvokePrompt([FromBody] PromptRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request?.Prompt))
        //        return BadRequest("Prompt is required.");

        //    var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        //    var settings = new GeminiPromptExecutionSettings
        //    {
        //        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        //        ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
        //    };

        //    try
        //    {
        //        var r = await chatCompletionService.GetChatMessageContentAsync(request.Prompt, settings, _kernel);
        //        return Ok(new { response = r.Content });
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        [HttpGet]
        public async Task GetWebSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketChat(webSocket, default);
        }

        private async Task HandleWebSocketChat(WebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                var history = new ChatHistory();
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    using var ms = new MemoryStream();
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", cancellationToken);
                            return;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);
                    var prompt = Encoding.UTF8.GetString(ms.ToArray());
                    if (string.IsNullOrWhiteSpace(prompt))
                    {
                        await SendWebSocketMessage(webSocket, "Prompt is required.", cancellationToken);
                        continue;
                    }

                    var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
                    var settings = new GeminiPromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                        ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
                    };

                    try
                    {
                        history.AddUserMessage(prompt);
                        var message = await chatCompletionService.GetChatMessageContentAsync(history, settings, _kernel);
                        history.AddMessage(message.Role, message.Content ?? string.Empty);
                        await SendWebSocketMessage(webSocket, message.Content, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        await SendWebSocketMessage(webSocket, $"Error: {ex.Message}", cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await SendWebSocketMessage(webSocket, $"Fatal error: {ex.Message}", cancellationToken);
                }
            }
            finally
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", cancellationToken);
                }
            }
        }

        private async Task SendWebSocketMessage(WebSocket webSocket, string message, CancellationToken cancellationToken)
        {
            if (webSocket.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    public class PromptRequest
    {
        public string Prompt { get; set; }
    }
}

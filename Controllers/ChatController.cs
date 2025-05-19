using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.HuggingFace;
using ModelContextProtocol.Client;

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

        [HttpPost("invoke")]
        public async Task<IActionResult> InvokePrompt([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
                return BadRequest("Prompt is required.");

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            var settings = new GeminiPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            };


            //var r = await _kernel.InvokePromptAsync(request.Prompt, new(settings));
            //HuggingFacePromptExecutionSettings settings = new HuggingFacePromptExecutionSettings
            //{

            //    FunctionChoiceBehavior = FunctionChoiceBehavior.Required(options: new FunctionChoiceBehaviorOptions { AllowStrictSchemaAdherence = true }),
            //};
            try
            {
                var r = await chatCompletionService.GetChatMessageContentAsync(request.Prompt, settings, _kernel);
            return Ok(new { response = r.Content });
            }
            catch ( Exception ex)
            {

                throw ex;
            }

        }
    }

    public class PromptRequest
    {
        public string Prompt { get; set; }
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using API.Data;
using API.DTOs;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers;

public class AIController : BaseApiController
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly StoreContext _context;
    private readonly ILogger<AIController> _logger;
    
    public AIController(IConfiguration config, StoreContext context, ILogger<AIController> logger = null)
    {
        _httpClient = new HttpClient();
        _config = config;
        _context = context;
        _logger = logger;
    }
    
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponseDto>> GetChatResponse([FromBody] ChatRequestDto request)
    {
        try
        {
            var apiKey = _config["DeepSeekAI:ApiKey"];
            Console.WriteLine($"API Key exists: {!string.IsNullOrEmpty(apiKey)}");
            
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest(new ProblemDetails { Title = "DeepSeek API key not configured" });
            
            // 尝试简化的请求，仅用于调试API连接
            var simpleMessages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = "Hello, who are you?" }
            };
            
            // Format messages for DeepSeek API
            var deepseekRequest = new
            {
                model = "deepseek-chat", // 尝试不同的模型名称
                messages = simpleMessages, // 简化调试
                temperature = 0.7,
                max_tokens = 800
            };
            
            var requestJson = JsonSerializer.Serialize(deepseekRequest);
            Console.WriteLine($"Request to DeepSeek: {requestJson}");
            
            var content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json");
                
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            
            Console.WriteLine("Sending request to DeepSeek API...");
            // 尝试两个可能的端点
            var urls = new[] {
                "https://api.deepseek.com/v1/chat/completions", 
                "https://api.deepseek.ai/v1/chat/completions"
            };
            
            HttpResponseMessage response = null;
            string responseString = null;
            
            foreach (var url in urls)
            {
                try
                {
                    Console.WriteLine($"Trying endpoint: {url}");
                    response = await _httpClient.PostAsync(url, content);
                    responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response status: {response.StatusCode}");
                    Console.WriteLine($"Response body: {responseString}");
                    
                    if (response.IsSuccessStatusCode)
                        break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with endpoint {url}: {ex.Message}");
                }
            }
            
            if (response == null || !response.IsSuccessStatusCode)
            {
                return BadRequest(new ProblemDetails { 
                    Title = "Error from DeepSeek AI service",
                    Detail = responseString ?? "No response from any endpoint"
                });
            }
            
            // Parse response - 修改解析方式，适应 DeepSeek 的实际响应格式
            try
            {
                var jsonResponse = JsonDocument.Parse(responseString);
                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                return new ChatResponseDto
                {
                    Message = messageContent
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing response: {ex.Message}");
                return BadRequest(new ProblemDetails { 
                    Title = "Error parsing DeepSeek AI response",
                    Detail = ex.Message + " Response: " + responseString
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return BadRequest(new ProblemDetails { 
                Title = "Error processing AI request",
                Detail = ex.Message
            });
        }
    }
}
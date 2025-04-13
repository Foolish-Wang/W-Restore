// 更新后的控制器，添加产品上下文
using System.Text;
using System.Text.Json;
using API.Data;
using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AIController : BaseApiController
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly StoreContext _context;
    
    public AIController(IConfiguration config, StoreContext context)
    {
        _httpClient = new HttpClient();
        _config = config;
        _context = context;
    }
    
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponseDto>> GetChatResponse([FromBody] ChatRequestDto request)
    {
        try
        {
            var apiKey = _config["DeepSeekAI:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest(new ProblemDetails { Title = "DeepSeek API key not configured" });
            
            // Get product catalog data to include in context
            var products = await _context.Products
                .Take(15)  // 限制数量以保持上下文大小合理
                .Select(p => new {
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Brand,
                    p.Type
                })
                .ToListAsync();
            
            // 如果请求没有系统信息，添加系统信息
            var hasSystemMessage = request.Messages.Any(m => m.Role == "system");
            if (!hasSystemMessage)
            {
                request.Messages.Insert(0, new ChatMessage
                {
                    Role = "system",
                    Content = $"You are a helpful shopping assistant for our e-commerce store named Restore. " +
                             $"Here are some products from our catalog: {JsonSerializer.Serialize(products)}. " +
                             $"Use this product information to make specific recommendations when asked. " +
                             $"Be friendly, helpful, and concise."
                });
            }
            
            // Format messages for DeepSeek API
            var deepseekRequest = new
            {
                model = "deepseek-chat",
                messages = request.Messages,
                temperature = 0.7,
                max_tokens = 800
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(deepseekRequest),
                Encoding.UTF8,
                "application/json");
                
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await _httpClient.PostAsync(
                "https://api.deepseek.com/v1/chat/completions", 
                content);
                
            var responseString = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                return BadRequest(new ProblemDetails { 
                    Title = "Error from DeepSeek AI service",
                    Detail = responseString
                });
                
            // Parse response
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
            return BadRequest(new ProblemDetails { 
                Title = "Error processing AI request",
                Detail = ex.Message
            });
        }
    }
}
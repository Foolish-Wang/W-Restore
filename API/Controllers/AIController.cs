
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


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
            
            // 获取并分析用户最新的消息
            var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
            Console.WriteLine($"Last user message: {lastUserMessage}");
            
            // 基于用户查询提取关键词
            var keywords = ExtractKeywords(lastUserMessage);
            Console.WriteLine($"Extracted keywords: {string.Join(", ", keywords)}");
            
            // 基于关键词智能查询相关产品
            var products = await GetRelevantProducts(keywords);
            Console.WriteLine($"Found {products.Count} relevant products");

            // 打印找到的产品名称，用于调试
            foreach (var product in products.Take(5))
            {
                var dynamicProduct = (dynamic)product;
                Console.WriteLine($"Found product: {dynamicProduct.Name}");
            }
            
            // 准备发送到API的消息
            var messagesToSend = new List<object>();
            
            // 添加系统消息和产品信息
            bool hasSystemMessage = request.Messages.Any(m => m.Role == "system");
            if (!hasSystemMessage)
            {
                messagesToSend.Add(new {
                    role = "system",
                    content = BuildSystemPrompt(products, lastUserMessage)
                });
            }
            
            // 添加用户的消息
            foreach (var message in request.Messages)
            {
                messagesToSend.Add(new {
                    role = message.Role,
                    content = message.Content
                });
            }
            
            // Format messages for DeepSeek API
            var deepseekRequest = new
            {
                model = "deepseek-chat",
                messages = messagesToSend,
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
            
            // Parse response
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
    
    // 提取用户消息中的关键词 - 增强版
    private List<string> ExtractKeywords(string message)
    {
        var keywords = new List<string>();
        var lowerMessage = message.ToLower();
        
        // 产品类别关键词映射 - 增加了更多关键词，包括中英文词汇
        var categoryKeywords = new Dictionary<string, List<string>> {
            { "board", new List<string> { "board", "angular", "react", "typescript", "vue", "coding", "deck", "skateboard", "滑板" } },
            { "boot", new List<string> { "boot", "shoe", "footwear", "hiking", "winter", "靴子", "鞋", "鞋子" } },
            { "glove", new List<string> { "glove", "mitt", "hand", "winter", "protection", "手套" } },
            { "hat", new List<string> { "hat", "cap", "beanie", "head", "winter", "帽", "帽子", "headwear" } },
            { "wool", new List<string> { "wool", "woolen", "fleece", "羊毛", "毛", "保暖" } }
        };
        
        // 价格范围关键词 - 增加中文支持
        var pricePattern = @"(\$|under|less than|cheaper than|above|more than|expensive|affordable|cheap|budget|premium|luxury|price range|cost|pricing|价格|便宜|贵|实惠)";
        var priceMatch = Regex.Match(lowerMessage, pricePattern);
        if (priceMatch.Success) keywords.Add("price");
        
        // 特定品牌关键词 - 增加中文支持
        var brandPattern = @"(brand|manufacturer|made by|from|provider|品牌|制造商|产自)";
        var brandMatch = Regex.Match(lowerMessage, brandPattern);
        if (brandMatch.Success) keywords.Add("brand");
        
        // 检查特定组合 - 羊毛帽
        if ((lowerMessage.Contains("wool") || lowerMessage.Contains("woolen") || 
             lowerMessage.Contains("羊毛")) && 
            (lowerMessage.Contains("hat") || lowerMessage.Contains("cap") || 
             lowerMessage.Contains("帽") || lowerMessage.Contains("帽子")))
        {
            keywords.Add("wool");
            keywords.Add("hat");
            Console.WriteLine("Detected wool hat combination keywords");
        }
        
        // 遍历类别词典，查找匹配项
        foreach (var category in categoryKeywords)
        {
            foreach (var keyword in category.Value)
            {
                if (lowerMessage.Contains(keyword))
                {
                    keywords.Add(category.Key);
                    break; // 一旦找到类别匹配，就停止搜索该类别的其他关键词
                }
            }
        }
        
        // 如果没有找到任何关键词，返回一个空关键词以获取所有产品
        if (keywords.Count == 0) keywords.Add("all");
        
        return keywords;
    }
    
    // 根据关键词获取相关产品 - 增强版
    private async Task<List<object>> GetRelevantProducts(List<string> keywords)
    {
        IQueryable<Product> query = _context.Products.OrderBy(p => p.Id);
        
        // 特殊组合查询 - 羊毛帽
        if (keywords.Contains("wool") && keywords.Contains("hat"))
        {
            query = query.Where(p => 
                (p.Type.ToLower().Contains("hat") || 
                p.Name.ToLower().Contains("hat")) && 
                (p.Name.ToLower().Contains("wool") || 
                p.Name.ToLower().Contains("woolen") ||
                p.Description.ToLower().Contains("wool") || 
                p.Description.ToLower().Contains("woolen"))
            );
            Console.WriteLine("Applying wool hat filter");
        }
        else 
        {
            // 处理单一产品类型过滤
            if (keywords.Contains("board"))
            {
                query = query.Where(p => p.Type.ToLower().Contains("board"));
            }
            else if (keywords.Contains("boot"))
            {
                query = query.Where(p => p.Type.ToLower().Contains("boot"));
            }
            else if (keywords.Contains("glove"))
            {
                query = query.Where(p => p.Type.ToLower().Contains("glove"));
            }
            else if (keywords.Contains("hat"))
            {
                query = query.Where(p => p.Type.ToLower().Contains("hat") || p.Name.ToLower().Contains("hat"));
            }
            
            // 单独的材料过滤 - 不考虑组合情况时使用
            if (keywords.Contains("wool") && !keywords.Contains("hat"))
            {
                query = query.Where(p => 
                    p.Name.ToLower().Contains("wool") || 
                    p.Name.ToLower().Contains("woolen") || 
                    p.Description.ToLower().Contains("wool") ||
                    p.Description.ToLower().Contains("woolen")
                );
            }
        }
        
        // 如果找不到任何匹配产品，就返回所有产品
        var count = await query.CountAsync();
        Console.WriteLine($"Query matched {count} products");
        
        if (count == 0)
        {
            // 尝试扩大搜索范围 - 检查产品名称
            if (keywords.Contains("wool") && keywords.Contains("hat"))
            {
                query = _context.Products.Where(p => 
                    p.Name.ToLower().Contains("woolen") && 
                    p.Name.ToLower().Contains("hat"));
                
                Console.WriteLine("Trying expanded search for woolen hat in product names");
                count = await query.CountAsync();
                Console.WriteLine($"Expanded search found {count} products");
                
                if (count == 0)
                {
                    // 最后尝试查找所有帽子
                    query = _context.Products.Where(p => 
                        p.Name.ToLower().Contains("hat") || 
                        p.Type.ToLower().Contains("hat"));
                    
                    Console.WriteLine("Falling back to all hats");
                }
            }
            else
            {
                query = _context.Products.OrderBy(p => p.Id);
                Console.WriteLine("Falling back to all products");
            }
        }
        
        // 限制返回数量以适应API上下文限制
        var products = await query
            .Take(15)
            .Select(p => new {
                p.Id,
                p.Name, 
                p.Description,
                p.Price,
                p.Brand,
                p.Type,
                PictureUrl = p.PictureUrl
            })
            .ToListAsync();
        
        Console.WriteLine($"Final product count: {products.Count}");
        
        // 如果仍然没有产品，返回一个空列表
        return products.Cast<object>().ToList();
    }
    
    // 构建基于产品和用户查询的系统提示
    private string BuildSystemPrompt(List<object> products, string userQuery)
    {
        var prompt = "You are a helpful shopping assistant for our e-commerce store Restore. ";
        
        prompt += "IMPORTANT: ONLY recommend products from the provided list. " +
             "If the user asks for a product type we don't have, clearly state that we don't carry that product. " +
             "For example, if we don't have woolen hats in our catalog and the user asks for them, " +
             "you should say 'We currently don't carry woolen hats in our catalog' rather than recommending something we don't have. ";
        // 添加产品信息
        if (products.Any())
        {
            prompt += $"Here are some products from our catalog that might be relevant to the customer's query: {JsonSerializer.Serialize(products)}. ";
        }
        else
        {
            prompt += "Unfortunately, I don't have specific product information to share at this moment, but I can still help with general questions. ";
        }
        
        // 添加通用提示
        prompt += "Use this product information to make specific recommendations when asked. " +
                 "Be friendly, helpful, and concise. When recommending products, mention their name, price, " +
                 "and a brief description. Never make up products that aren't in the provided list. " +
                 "If the products don't match what the customer is looking for, suggest browsing categories " +
                 "instead of making up product details. ";
        
        // 添加特别注意事项
        prompt += "Pay careful attention to product names that contain 'Woolen' or 'wool' as they indicate wool material products. " +
                 "When a customer asks about wool products, recommend items with 'Woolen' in their names. ";
        
        // 根据用户查询定制提示
        var lowerQuery = userQuery.ToLower();
        if (lowerQuery.Contains("price") || lowerQuery.Contains("cost") || 
            lowerQuery.Contains("expensive") || lowerQuery.Contains("价格") || 
            lowerQuery.Contains("贵") || lowerQuery.Contains("便宜"))
        {
            prompt += "The customer seems interested in price information, so highlight pricing in your response. ";
        }
        
        if (lowerQuery.Contains("compare") || lowerQuery.Contains("difference") || 
            lowerQuery.Contains("versus") || lowerQuery.Contains("vs") || 
            lowerQuery.Contains("比较"))
        {
            prompt += "The customer wants to compare products, so provide a comparison of relevant products if available. ";
        }
        
        if ((lowerQuery.Contains("wool") || lowerQuery.Contains("woolen") || 
             lowerQuery.Contains("羊毛")) && 
            (lowerQuery.Contains("hat") || lowerQuery.Contains("cap") || 
             lowerQuery.Contains("帽") || lowerQuery.Contains("帽子")))
        {
            prompt += "The customer is specifically looking for wool hats. Ensure you mention all products with 'Woolen' in their name that are hats. ";
        }
        
        return prompt;
    }
}
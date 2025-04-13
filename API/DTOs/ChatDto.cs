using System.Collections.Generic;

namespace API.DTOs;

public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class ChatRequestDto
{
    public List<ChatMessage> Messages { get; set; }
}

public class ChatResponseDto
{
    public string Message { get; set; }
}
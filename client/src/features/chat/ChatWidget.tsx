import { useState, useRef, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  TextField,
  IconButton,
  Fab,
  Drawer,
  Avatar,
  List,
  ListItem,
} from '@mui/material';
import { Send, Close, SmartToy } from '@mui/icons-material';
import ReactMarkdown from 'react-markdown';
import agent from '../../app/api/agent';

// 简单的 Markdown 清理函数，用于非 ReactMarkdown 方案
const stripMarkdown = (markdown: string): string => {
  if (!markdown) return '';

  // 移除标题标记 (# Heading)
  let text = markdown.replace(/^#+\s+/gm, '');

  // 移除粗体标记 (**text**)
  text = text.replace(/\*\*(.*?)\*\*/g, '$1');

  // 移除斜体标记 (*text*)
  text = text.replace(/\*(.*?)\*/g, '$1');

  // 移除链接标记 [text](url)
  text = text.replace(/\[(.*?)\]\(.*?\)/g, '$1');

  // 移除代码块标记
  text = text.replace(/```[\s\S]*?```/g, (match) => {
    // 提取代码块内容
    const code = match.replace(/```[\w]*\n|```$/g, '');
    return code.trim();
  });

  // 移除行内代码标记 (`code`)
  text = text.replace(/`([^`]+)`/g, '$1');

  // 移除列表标记 (- item 或 1. item)
  text = text.replace(/^[\s]*[-*]\s+/gm, '• ');
  text = text.replace(/^[\s]*\d+\.\s+/gm, '• ');

  return text;
};

export default function ChatWidget() {
  const [messages, setMessages] = useState<{ role: string; content: string }[]>(
    [
      {
        role: 'assistant',
        content:
          "Hello! I'm your AI shopping assistant. How can I help you today?",
      },
    ]
  );
  const [open, setOpen] = useState(false);

  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const messagesEndRef = useRef<null | HTMLDivElement>(null);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    if (input.trim() === '') return;

    // Add user message to chat
    const userMessage = { role: 'user', content: input };
    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    try {
      // 发送可见消息给后端，过滤系统消息
      const visibleMessages = messages.filter((msg) => msg.role !== 'system');

      // Send message to AI and get response
      const response = await agent.AI.chat(
        visibleMessages.concat([userMessage])
      );

      // 使用原始响应内容，不再需要清理 Markdown
      setMessages((prev) => [
        ...prev,
        { role: 'assistant', content: response.message },
      ]);
    } catch (error) {
      console.error('Error getting AI response:', error);
      setMessages((prev) => [
        ...prev,
        {
          role: 'assistant',
          content: 'Sorry, I encountered an error. Please try again later.',
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      {/* Floating button to open chat */}
      <Fab
        color="primary"
        aria-label="chat"
        onClick={() => setOpen(true)}
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
      >
        <SmartToy />
      </Fab>

      {/* Chat drawer */}
      <Drawer anchor="right" open={open} onClose={() => setOpen(false)}>
        <Box
          sx={{
            width: 320,
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          {/* Header */}
          <Box
            sx={{
              p: 2,
              bgcolor: 'primary.main',
              color: 'white',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <Typography variant="h6">AI Shopping Assistant</Typography>
            <IconButton color="inherit" onClick={() => setOpen(false)}>
              <Close />
            </IconButton>
          </Box>

          {/* Messages */}
          <Box sx={{ flexGrow: 1, p: 2, overflow: 'auto' }}>
            <List>
              {messages.map((message, index) => (
                <ListItem
                  key={index}
                  alignItems="flex-start"
                  sx={{
                    flexDirection: 'column',
                    alignItems:
                      message.role === 'user' ? 'flex-end' : 'flex-start',
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                    {message.role === 'assistant' && (
                      <Avatar sx={{ mr: 1, bgcolor: 'primary.main' }}>
                        <SmartToy />
                      </Avatar>
                    )}
                    <Typography variant="body2" color="textSecondary">
                      {message.role === 'user' ? 'You' : 'Assistant'}
                    </Typography>
                  </Box>
                  <Paper
                    elevation={1}
                    sx={{
                      p: 2,
                      maxWidth: '80%',
                      bgcolor:
                        message.role === 'user'
                          ? 'primary.light'
                          : 'background.paper',
                      color: message.role === 'user' ? 'white' : 'inherit',
                      borderRadius:
                        message.role === 'user'
                          ? '20px 20px 0 20px'
                          : '20px 20px 20px 0',
                    }}
                  >
                    {message.role === 'user' ? (
                      <Typography variant="body1">{message.content}</Typography>
                    ) : (
                      <ReactMarkdown
                        components={{
                          // 修复 Typography 组件错误
                          p: ({ children, ...props }) => (
                            <Typography variant="body1" gutterBottom>
                              {children}
                            </Typography>
                          ),
                          a: ({ children, ...props }) => (
                            <a style={{ color: '#1976d2' }} {...props}>
                              {children}
                            </a>
                          ),
                          ul: ({ children, ...props }) => (
                            <ul
                              style={{ marginLeft: '20px', paddingLeft: 0 }}
                              {...props}
                            >
                              {children}
                            </ul>
                          ),
                          li: ({ children, ...props }) => (
                            <li style={{ marginBottom: '4px' }} {...props}>
                              {children}
                            </li>
                          ),
                          h1: ({ children, ...props }) => (
                            <Typography variant="h6" gutterBottom>
                              {children}
                            </Typography>
                          ),
                          h2: ({ children, ...props }) => (
                            <Typography variant="subtitle1" gutterBottom>
                              {children}
                            </Typography>
                          ),
                          h3: ({ children, ...props }) => (
                            <Typography variant="subtitle2" gutterBottom>
                              {children}
                            </Typography>
                          ),
                          // 修复后的代码组件
                          code: ({ className, children, ...props }) => {
                            const match = /language-(\w+)/.exec(
                              className || ''
                            );
                            return props.node?.properties?.inline ? (
                              <code
                                style={{
                                  background: '#f5f5f5',
                                  padding: '2px 4px',
                                  borderRadius: '4px',
                                }}
                                className={className}
                                {...props}
                              >
                                {children}
                              </code>
                            ) : (
                              <pre
                                style={{
                                  background: '#f5f5f5',
                                  padding: '8px',
                                  borderRadius: '4px',
                                  overflowX: 'auto',
                                }}
                              >
                                <code className={className} {...props}>
                                  {children}
                                </code>
                              </pre>
                            );
                          },
                        }}
                      >
                        {message.content}
                      </ReactMarkdown>
                    )}
                  </Paper>
                </ListItem>
              ))}
              <div ref={messagesEndRef} />
            </List>
          </Box>

          {/* Input */}
          <Box
            sx={{ p: 2, borderTop: 1, borderColor: 'divider', display: 'flex' }}
          >
            <TextField
              fullWidth
              placeholder="Type a message..."
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSend()}
              disabled={loading}
            />
            <IconButton color="primary" onClick={handleSend} disabled={loading}>
              <Send />
            </IconButton>
          </Box>
        </Box>
      </Drawer>
    </>
  );
}

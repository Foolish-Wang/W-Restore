import { useState, useRef, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  IconButton,
  Fab,
  Drawer,
  Avatar,
  List,
  ListItem,
  ListItemText,
  Divider,
} from '@mui/material';
import { Send, Close, SmartToy } from '@mui/icons-material';
import agent from '../../app/api/agent';

export default function ChatWidget() {
  const [messages, setMessages] = useState<{ role: string; content: string }[]>(
    [
      {
        role: 'system',
        content:
          'You are a helpful shopping assistant for our e-commerce store Restore. ' +
          'You can recommend products from our catalog, answer questions about our items, ' +
          'help with sizing, and provide other shopping advice. Our store specializes in ' +
          'boards (Angular, React, TypeScript), boots, gloves, and other related products. ' +
          'Be friendly, helpful, and concise. When recommending products, refer to specific ' +
          "products in our catalog by name when possible. If you don't know something specific " +
          'about our products, you can suggest categories to browse instead of making up product details.',
      },
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
      // Send message to AI and get response
      const response = await agent.AI.chat(messages.concat([userMessage]));
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
                    <Typography variant="body1">{message.content}</Typography>
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

const express = require('express');
const cors = require('cors');

const app = express();
const PORT = process.env.PORT || 7808;
const DELAY_MIN = parseInt(process.env.RESPONSE_DELAY_MIN || '1000');
const DELAY_MAX = parseInt(process.env.RESPONSE_DELAY_MAX || '2000');

app.use(cors());
app.use(express.json());

// Helper function to simulate processing delay
const simulateDelay = () => {
  const delay = Math.floor(Math.random() * (DELAY_MAX - DELAY_MIN + 1)) + DELAY_MIN;
  return new Promise(resolve => setTimeout(resolve, delay));
};

// ProjectVG API format endpoint
app.post('/api/v1/chat', async (req, res) => {
  console.log(`[${new Date().toISOString()}] ProjectVG chat request received`);
  
  const startTime = Date.now();
  await simulateDelay();
  
  const { 
    system_prompt, 
    user_prompt, 
    conversation_history, 
    model, 
    max_tokens, 
    temperature,
    instructions 
  } = req.body;
  
  // Generate ProjectVG-compatible response
  const responseId = `llm_${Date.now()}_${Math.random().toString(36).substring(7)}`;
  const requestId = `req_${Date.now()}`;
  // Generate structured response in ProjectVG format
  const emotions = ['neutral', 'happy', 'sad', 'angry', 'surprised', 'confused', 'shy', 'excited'];
  const actions = ['tilting_head', 'smiling', 'sighing', 'blushing', 'looking_away', 'clapping', 'jumping', 'waving'];
  
  const emotion = emotions[Math.floor(Math.random() * emotions.length)];
  const action = actions[Math.floor(Math.random() * actions.length)];
  
  const userMsg = user_prompt || 'no message';
  let responseText;
  
  if (userMsg.toLowerCase().includes('hello') || userMsg.toLowerCase().includes('안녕')) {
    responseText = `[emotion:${emotion}](action:${action})""안녕하세요! 저는 더미 AI입니다.""[emotion:happy]""${userMsg}라고 말씀해주셔서 감사해요!""`;
  } else if (userMsg.toLowerCase().includes('how are you') || userMsg.toLowerCase().includes('어떻게')) {
    responseText = `[emotion:${emotion}](action:${action})""저는 부하 테스트 중이라 매우 바쁘답니다!""[emotion:excited]""하지만 테스트는 잘 진행되고 있어요!""`;
  } else {
    responseText = `[emotion:${emotion}](action:${action})""${userMsg}에 대한 더미 응답입니다.""[emotion:neutral]""부하 테스트용 가짜 AI 응답이에요!""`;
  }
  
  console.log(`[${new Date().toISOString()}] Generated structured response: ${responseText.substring(0, 100)}...`);
  
  const dummyResponse = {
    id: responseId,
    request_id: requestId,
    object: "response",
    created_at: Math.floor(Date.now() / 1000),
    status: "completed",
    model: model || "dummy-model",
    output_text: responseText,
    input_tokens: 0,
    output_tokens: 0,
    total_tokens: 0,
    cached_tokens: 0,
    reasoning_tokens: 0,
    text_format_type: "text",
    cost: 0,
    response_time: (Date.now() - startTime) / 1000,
    success: true,
    error: null,
    use_user_api_key: false
  };
  
  console.log(`[${new Date().toISOString()}] ProjectVG chat response sent: ${responseText.substring(0, 50)}...`);
  res.json(dummyResponse);
});

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'ok', 
    service: 'dummy-llm-server',
    port: PORT,
    timestamp: new Date().toISOString()
  });
});

// Catch all endpoint for any missed routes
app.all('*', (req, res) => {
  console.log(`[${new Date().toISOString()}] Unhandled request: ${req.method} ${req.path}`);
  res.status(404).json({ 
    error: 'Not Found', 
    message: 'This is a dummy LLM server for load testing',
    service: 'dummy-llm-server'
  });
});

app.listen(PORT, () => {
  console.log(`Dummy LLM Server running on port ${PORT}`);
  console.log(`Response delay: ${DELAY_MIN}ms - ${DELAY_MAX}ms`);
  console.log(`Health check: http://localhost:${PORT}/health`);
});

module.exports = app;
const express = require('express');
const cors = require('cors');

const app = express();
const PORT = process.env.PORT || 7812;
const DELAY = parseInt(process.env.RESPONSE_DELAY || '100');

app.use(cors());
app.use(express.json());

// In-memory storage for dummy data
const episodicMemories = new Map();
const semanticMemories = new Map();
let documentCounter = 1;

// Helper function to simulate processing delay
const simulateDelay = () => {
  return new Promise(resolve => setTimeout(resolve, DELAY));
};

// VectorMemoryClient API endpoints

// Insert episodic memory (InsertEpisodicAsync) - Required by ProjectVG
app.post('/api/memory/episodic', async (req, res) => {
  console.log(`[${new Date().toISOString()}] Episodic memory insert request received`);
  
  await simulateDelay();
  
  const { text, user_id, speaker, emotion, context, importance_score } = req.body;
  
  const memoryId = `epi_${documentCounter++}`;
  const memory = {
    id: memoryId,
    text: text || '더미 에피소딕 메모리',
    user_id: user_id || 'dummy-user',
    speaker: speaker || 'user',
    emotion,
    context,
    importance_score: importance_score || Math.random(),
    memory_type: 'episodic',
    collection_name: 'episodic_collection',
    timestamp: new Date().toISOString()
  };
  
  episodicMemories.set(memoryId, memory);
  
  console.log(`[${new Date().toISOString()}] Episodic memory stored: ${memoryId}`);
  res.json(memory);
});

// Insert semantic memory (InsertSemanticAsync) - Required by ProjectVG  
app.post('/api/memory/semantic', async (req, res) => {
  console.log(`[${new Date().toISOString()}] Semantic memory insert request received`);
  
  await simulateDelay();
  
  const { text, user_id, fact_type, confidence_score, importance_score, last_updated } = req.body;
  
  const memoryId = `sem_${documentCounter++}`;
  const memory = {
    id: memoryId,
    text: text || '더미 시맨틱 메모리',
    user_id: user_id || 'dummy-user',
    fact_type: fact_type || 'general',
    confidence_score: confidence_score || Math.random(),
    importance_score: importance_score || Math.random(),
    last_updated: last_updated || new Date().toISOString(),
    memory_type: 'semantic',
    collection_name: 'semantic_collection',
    timestamp: new Date().toISOString()
  };
  
  semanticMemories.set(memoryId, memory);
  
  console.log(`[${new Date().toISOString()}] Semantic memory stored: ${memoryId}`);
  res.json(memory);
});

// Auto-insert memory (InsertAutoAsync)
app.post('/api/memory', async (req, res) => {
  console.log(`[${new Date().toISOString()}] Auto memory insert request received`);
  
  await simulateDelay();
  
  const { text, user_id, speaker, emotion, context, importance_score } = req.body;
  
  // Simulate auto-classification (50% episodic, 50% semantic)
  const isEpisodic = Math.random() > 0.5;
  const memoryType = isEpisodic ? 'episodic' : 'semantic';
  const store = isEpisodic ? episodicMemories : semanticMemories;
  
  const memoryId = `mem_${documentCounter++}`;
  const memory = {
    id: memoryId,
    text: text || `더미 ${memoryType} 메모리`,
    user_id: user_id || 'dummy-user',
    speaker: speaker || 'user',
    emotion,
    context,
    importance_score: importance_score || Math.random(),
    memory_type: memoryType,
    collection_name: `${memoryType}_collection`,
    timestamp: new Date().toISOString(),
    classification_confidence: Math.random() * 0.3 + 0.7,
    classification_explanation: `Auto-classified as ${memoryType} based on content analysis`
  };
  
  store.set(memoryId, memory);
  
  console.log(`[${new Date().toISOString()}] Auto memory stored as ${memoryType}: ${memoryId}`);
  res.json(memory);
});





// Multi-search across both memory types
app.get('/api/memory/search/multi', async (req, res) => {
  console.log(`[${new Date().toISOString()}] Multi-search request received`);
  
  await simulateDelay();
  
  const { query, limit = 10 } = req.query;
  const userId = req.headers['x-user-id'];
  const halfLimit = Math.ceil(limit / 2);
  
  const episodicResults = Array.from(episodicMemories.values())
    .filter(mem => !userId || mem.user_id === userId)
    .slice(0, halfLimit)
    .map(mem => ({
      text: mem.text,
      score: Math.random() * 0.4 + 0.6
    }));
    
  const semanticResults = Array.from(semanticMemories.values())
    .filter(mem => !userId || mem.user_id === userId)
    .slice(0, halfLimit)
    .map(mem => ({
      text: mem.text,
      score: Math.random() * 0.4 + 0.6
    }));
  
  const response = {
    episodic_results: episodicResults,
    semantic_results: semanticResults,
    total_results: episodicResults.length + semanticResults.length
  };
  
  console.log(`[${new Date().toISOString()}] Multi-search completed: ${response.total_results} results`);
  res.json(response);
});



// Legacy endpoints (keeping for backward compatibility)

// Store document/memory
app.post('/api/memory/store', async (req, res) => {
  console.log(`[${new Date().toISOString()}] Memory store request received`);
  
  await simulateDelay();
  
  const { content, metadata, userId, characterId } = req.body;
  
  const documentId = `doc_${documentCounter++}`;
  const document = {
    id: documentId,
    content: content || `더미 메모리 콘텐츠 ${documentId}`,
    metadata: metadata || { type: 'conversation', importance: 'medium' },
    userId: userId || 'dummy-user',
    characterId: characterId || 'dummy-character',
    timestamp: new Date().toISOString(),
    vectorEmbedding: Array.from({ length: 384 }, () => Math.random()) // 더미 임베딩 벡터
  };
  
  episodicMemories.set(documentId, document);
  
  console.log(`[${new Date().toISOString()}] Memory stored with ID: ${documentId}`);
  res.json({
    success: true,
    documentId: documentId,
    message: '메모리가 성공적으로 저장되었습니다.'
  });
});

// Search/retrieve memories
app.post('/api/memory/search', async (req, res) => {
  console.log(`[${new Date().toISOString()}] Memory search request received`);
  
  await simulateDelay();
  
  const { query, userId, characterId, limit = 5, threshold = 0.7 } = req.body;
  
  // Simulate relevant memory retrieval
  const allDocuments = Array.from([...episodicMemories.values(), ...semanticMemories.values()]);
  const userDocuments = allDocuments.filter(doc => 
    !userId || doc.userId === userId
  ).filter(doc =>
    !characterId || doc.characterId === characterId
  );
  
  // Generate dummy search results
  const searchResults = userDocuments.slice(0, limit).map((doc, index) => ({
    ...doc,
    relevanceScore: Math.max(0.5, 1 - (index * 0.1)), // 감소하는 관련성 점수
    snippet: doc.content.substring(0, 150) + '...'
  }));
  
  console.log(`[${new Date().toISOString()}] Memory search completed, ${searchResults.length} results`);
  res.json({
    success: true,
    results: searchResults,
    query: query,
    totalFound: searchResults.length
  });
});






// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'ok', 
    service: 'dummy-memory-server',
    port: PORT,
    totalMemories: episodicMemories.size + semanticMemories.size,
    responseDelay: `${DELAY}ms`,
    timestamp: new Date().toISOString()
  });
});


// Catch all endpoint for any missed routes
app.all('*', (req, res) => {
  console.log(`[${new Date().toISOString()}] Unhandled request: ${req.method} ${req.path}`);
  res.status(404).json({ 
    error: 'Not Found', 
    message: 'This is a dummy Memory server for load testing',
    service: 'dummy-memory-server'
  });
});

app.listen(PORT, () => {
  console.log(`Dummy Memory Server running on port ${PORT}`);
  console.log(`Response delay: ${DELAY}ms`);
  console.log(`Health check: http://localhost:${PORT}/health`);
  
  // Initialize with some dummy data
  for (let i = 1; i <= 10; i++) {
    const isEpisodic = i % 2 === 0;
    const store = isEpisodic ? episodicMemories : semanticMemories;
    const memType = isEpisodic ? 'episodic' : 'semantic';
    
    const memId = `${memType.substring(0,3)}_${i}`;
    store.set(memId, {
      id: memId,
      text: `이것은 더미 ${memType} 메모리 콘텐츠 ${i}입니다. 부하 테스트용 가짜 데이터입니다.`,
      user_id: `user_${Math.ceil(i / 3)}`,
      memory_type: memType,
      collection_name: `${memType}_collection`,
      timestamp: new Date(Date.now() - i * 86400000).toISOString(),
      importance_score: Math.random(),
      ...(isEpisodic ? {
        speaker: 'user',
        emotion: { valence: Math.random(), arousal: Math.random() },
        context: `Context ${i}`
      } : {
        fact_type: 'general',
        confidence_score: Math.random()
      })
    });
  }
  documentCounter = 11;
  
  console.log(`Initialized with ${episodicMemories.size + semanticMemories.size} dummy memories`);
});

module.exports = app;
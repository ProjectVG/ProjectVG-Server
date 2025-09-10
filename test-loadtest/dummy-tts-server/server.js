const express = require('express');
const cors = require('cors');
const multer = require('multer');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 7816;
const DELAY_MIN = parseInt(process.env.RESPONSE_DELAY_MIN || '2000');
const DELAY_MAX = parseInt(process.env.RESPONSE_DELAY_MAX || '3000');

// Configure multer for file uploads
const upload = multer({ storage: multer.memoryStorage() });

app.use(cors());
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true, limit: '10mb' }));

// Helper function to simulate processing delay
const simulateDelay = () => {
  const delay = Math.floor(Math.random() * (DELAY_MAX - DELAY_MIN + 1)) + DELAY_MIN;
  return new Promise(resolve => setTimeout(resolve, delay));
};

// Generate dummy audio data (fake WAV format)
const generateDummyAudio = (duration = 5) => {
  // WAV header for a mono 16-bit 22050Hz file
  const sampleRate = 22050;
  const samples = sampleRate * duration;
  const dataSize = samples * 2; // 16-bit = 2 bytes per sample
  const fileSize = 44 + dataSize;
  
  const buffer = Buffer.alloc(fileSize);
  
  // WAV header
  buffer.write('RIFF', 0);
  buffer.writeUInt32LE(fileSize - 8, 4);
  buffer.write('WAVE', 8);
  buffer.write('fmt ', 12);
  buffer.writeUInt32LE(16, 16); // PCM format chunk size
  buffer.writeUInt16LE(1, 20);  // PCM format
  buffer.writeUInt16LE(1, 22);  // Mono
  buffer.writeUInt32LE(sampleRate, 24);
  buffer.writeUInt32LE(sampleRate * 2, 28); // Byte rate
  buffer.writeUInt16LE(2, 32);  // Block align
  buffer.writeUInt16LE(16, 34); // Bits per sample
  buffer.write('data', 36);
  buffer.writeUInt32LE(dataSize, 40);
  
  // Generate random audio data (pure random noise)
  for (let i = 0; i < samples; i++) {
    // Pure random noise - completely random audio data
    const sample = (Math.random() - 0.5) * 32767;
    
    buffer.writeInt16LE(sample, 44 + i * 2);
  }
  
  return buffer;
};


// ProjectVG TextToSpeechClient compatible endpoint
app.post('/v1/text-to-speech/:voiceId', async (req, res) => {
  console.log(`[${new Date().toISOString()}] ProjectVG TTS request with voice: ${req.params.voiceId}`);
  
  await simulateDelay();
  
  const { text, speed = 1.0, pitch = 1.0 } = req.body;
  const voiceId = req.params.voiceId;
  
  if (!text || text.trim().length === 0) {
    return res.status(400).json({
      error: 'Text is required',
      message: '변환할 텍스트가 필요합니다.'
    });
  }
  
  // Calculate actual audio duration for generating audio data
  const wordsPerMinute = 150 * speed;
  const wordCount = text.trim().split(/\s+/).length;
  const actualDuration = Math.max(1, Math.ceil((wordCount / wordsPerMinute) * 60));
  
  // Generate normal dummy audio with actual duration
  const audioBuffer = generateDummyAudio(actualDuration);
  
  // For load testing: report audio length as 0 (but send real audio data)
  const reportedDuration = 0;
  
  console.log(`[${new Date().toISOString()}] ProjectVG TTS synthesis completed with voice ${voiceId}`);
  
  // Return audio buffer with essential headers
  res.set({
    'Content-Type': 'audio/wav',
    'Content-Length': audioBuffer.length,
    'X-Audio-Length': reportedDuration.toString(),
    'X-Text-Length': text.length.toString(),
    'X-Voice-Id': voiceId
  });
  
  res.send(audioBuffer);
});


// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'ok', 
    service: 'dummy-tts-server',
    port: PORT,
    responseDelay: `${DELAY_MIN}ms - ${DELAY_MAX}ms`,
    timestamp: new Date().toISOString()
  });
});


// Catch all endpoint for any missed routes
app.all('*', (req, res) => {
  console.log(`[${new Date().toISOString()}] Unhandled request: ${req.method} ${req.path}`);
  res.status(404).json({ 
    error: 'Not Found', 
    message: 'This is a dummy TTS server for load testing',
    service: 'dummy-tts-server'
  });
});

app.listen(PORT, () => {
  console.log(`Dummy TTS Server running on port ${PORT}`);
  console.log(`Response delay: ${DELAY_MIN}ms - ${DELAY_MAX}ms`);
  console.log(`Health check: http://localhost:${PORT}/health`);
});

module.exports = app;
#!/usr/bin/env node
/**
 * OAuth2 Test Client Server
 * Runs OAuth2 test client on port 3000 using Node.js
 */

const http = require('http');
const fs = require('fs');
const path = require('path');
const url = require('url');

// Configuration
const PORT = 3000;
const SCRIPT_DIR = __dirname;
const HTML_FILE = path.join(SCRIPT_DIR, 'oauth2-test-client.html');

// MIME types
const mimeTypes = {
    '.html': 'text/html',
    '.js': 'text/javascript',
    '.css': 'text/css',
    '.json': 'application/json',
    '.png': 'image/png',
    '.jpg': 'image/jpg',
    '.gif': 'image/gif',
    '.svg': 'image/svg+xml',
    '.wav': 'audio/wav',
    '.mp4': 'video/mp4',
    '.woff': 'application/font-woff',
    '.ttf': 'application/font-ttf',
    '.eot': 'application/vnd.ms-fontobject',
    '.otf': 'application/font-otf',
    '.wasm': 'application/wasm'
};

// Check if HTML file exists
if (!fs.existsSync(HTML_FILE)) {
    console.error(`❌ HTML file not found: ${HTML_FILE}`);
    console.error('Please check if oauth2-test-client.html exists in the same directory.');
    process.exit(1);
}

// Create HTTP server
const server = http.createServer((req, res) => {
    const parsedUrl = url.parse(req.url);
    let pathname = parsedUrl.pathname;
    
    // Default route redirects to OAuth2 test client
    if (pathname === '/') {
        res.writeHead(302, {
            'Location': '/oauth2-test-client.html'
        });
        res.end();
        return;
    }
    
    // Get file path
    let filePath = path.join(SCRIPT_DIR, pathname);
    
    // Security: prevent directory traversal
    if (!filePath.startsWith(SCRIPT_DIR)) {
        res.writeHead(403, { 'Content-Type': 'text/plain' });
        res.end('Forbidden');
        return;
    }
    
    // Check if file exists
    if (!fs.existsSync(filePath)) {
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end('File not found');
        return;
    }
    
    // Get file stats
    const stat = fs.statSync(filePath);
    
    // Handle directories
    if (stat.isDirectory()) {
        res.writeHead(403, { 'Content-Type': 'text/plain' });
        res.end('Directory access not allowed');
        return;
    }
    
    // Get MIME type
    const ext = path.extname(filePath).toLowerCase();
    const contentType = mimeTypes[ext] || 'application/octet-stream';
    
    // Read and serve file
    fs.readFile(filePath, (err, data) => {
        if (err) {
            res.writeHead(500, { 'Content-Type': 'text/plain' });
            res.end('Internal server error');
            return;
        }
        
        res.writeHead(200, { 
            'Content-Type': contentType,
            'Cache-Control': 'no-cache'
        });
        res.end(data);
    });
});

// Start server
server.listen(PORT, () => {
    console.log('🚀 OAuth2 Test Client server started!');
    console.log(`📍 URL: http://localhost:${PORT}`);
    console.log(`📁 Service directory: ${SCRIPT_DIR}`);
    console.log(`📄 HTML file: ${HTML_FILE}`);
    console.log('\n' + '='.repeat(50));
    console.log('🔐 OAuth2 Test Instructions:');
    console.log('1. Open http://localhost:3000 in browser');
    console.log('2. Click "Generate PKCE" button');
    console.log('3. Enter Google OAuth2 Client ID');
    console.log('4. Click "Start OAuth2 Login" button');
    console.log('5. Login with Google and authorize');
    console.log('6. Check results');
    console.log('='.repeat(50));
    console.log('\nPress Ctrl+C to stop the server.\n');
});

// Handle server errors
server.on('error', (err) => {
    if (err.code === 'EADDRINUSE') {
        console.error(`❌ Port ${PORT} is already in use.`);
        console.error('Please terminate other processes or use a different port.');
    } else {
        console.error('❌ Server error:', err.message);
    }
    process.exit(1);
});

// Handle graceful shutdown
process.on('SIGINT', () => {
    console.log('\n\n🛑 Server stopped.');
    server.close(() => {
        process.exit(0);
    });
});

process.on('SIGTERM', () => {
    console.log('\n\n🛑 Server stopped.');
    server.close(() => {
        process.exit(0);
    });
});

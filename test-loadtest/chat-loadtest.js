#!/usr/bin/env node

// Comprehensive Chat API Load Test Script for ProjectVG API
// Tests complete chat workflow: Guest Login -> JWT Auth -> Chat API -> WebSocket Response

const http = require('http');
const https = require('https');
const { URL } = require('url');
const WebSocket = require('ws');

// 설정
const CONFIG = {
    apiUrl: process.env.API_URL || 'http://localhost:7804',
    wsUrl: process.env.WS_URL || 'ws://localhost:7804',
    clients: parseInt(process.env.CLIENTS || '20'),
    duration: parseInt(process.env.DURATION || '60'),
    rampUp: parseInt(process.env.RAMP_UP || '10'),
    chatInterval: parseInt(process.env.CHAT_INTERVAL || '5000'), // 채팅 간격 (ms)
    
    // 테스트할 공개 캐릭터들 (API에서 조회된 실제 ID 사용)
    characters: [
        '22222222-2222-2222-2222-222222222222', // 소피아 (메이드)
        '11111111-1111-1111-1111-111111111111', // 미유 (딸같은 존재)
        '33333333-3333-3333-3333-333333333333'  // 하루 (절친)
    ],
    
    // 다양한 채팅 메시지 패턴
    chatMessages: [
        "안녕하세요! 오늘 날씨가 좋네요.",
        "오늘 하루 어떻게 지내셨나요?",
        "요즘 무엇을 하고 계시나요?",
        "좋아하는 음식이 무엇인가요?",
        "취미가 있다면 무엇인가요?",
        "최근에 본 영화나 책이 있나요?",
        "스트레스 받을 때 어떻게 해소하세요?",
        "주말 계획이 있으신가요?",
        "가장 기억에 남는 여행지는 어디인가요?",
        "새해 목표나 계획이 있으신가요?"
    ]
};

// WebSocket이 설치되어 있지 않으면 에러 메시지 표시
if (!WebSocket) {
    console.error('WebSocket package is required. Please install it:');
    console.error('npm install ws');
    process.exit(1);
}

class ChatLoadTestClient {
    constructor(clientId) {
        this.clientId = clientId;
        this.guestId = `loadtest-guest-${clientId}-${Date.now()}`;
        this.accessToken = null;
        this.refreshToken = null;
        this.webSocket = null;
        this.selectedCharacterId = null;
        
        // 통계
        this.stats = {
            guestLogins: 0,
            guestLoginErrors: 0,
            chatRequests: 0,
            chatSuccesses: 0,
            chatErrors: 0,
            wsConnections: 0,
            wsConnectionErrors: 0,
            wsMessages: 0,
            wsErrors: 0,
            totalResponseTime: 0,
            minResponseTime: Infinity,
            maxResponseTime: 0
        };
        
        this.running = false;
        this.authenticated = false;
        this.wsConnected = false;
    }

    // HTTP 요청 헬퍼
    async makeHttpRequest(method, path, data = null, headers = {}) {
        return new Promise((resolve) => {
            const startTime = Date.now();
            const url = new URL(path, CONFIG.apiUrl);
            const options = {
                hostname: url.hostname,
                port: url.port || (url.protocol === 'https:' ? 443 : 80),
                path: url.pathname + url.search,
                method: method,
                timeout: 10000,
                headers: {
                    'Content-Type': 'application/json',
                    'User-Agent': `ChatLoadTest-Client-${this.clientId}`,
                    ...headers
                }
            };

            const client = url.protocol === 'https:' ? https : http;
            const req = client.request(options, (res) => {
                let body = '';
                
                res.on('data', chunk => {
                    body += chunk;
                });
                
                res.on('end', () => {
                    const responseTime = Date.now() - startTime;
                    this.updateResponseTimeStats(responseTime);
                    
                    try {
                        const jsonBody = body ? JSON.parse(body) : null;
                        resolve({
                            success: res.statusCode >= 200 && res.statusCode < 300,
                            statusCode: res.statusCode,
                            data: jsonBody,
                            responseTime,
                            error: null
                        });
                    } catch (parseError) {
                        resolve({
                            success: false,
                            statusCode: res.statusCode,
                            data: null,
                            responseTime,
                            error: `JSON Parse Error: ${parseError.message}`
                        });
                    }
                });
            });

            req.on('error', (error) => {
                const responseTime = Date.now() - startTime;
                resolve({
                    success: false,
                    statusCode: 0,
                    data: null,
                    responseTime,
                    error: error.message
                });
            });

            req.on('timeout', () => {
                req.destroy();
                const responseTime = Date.now() - startTime;
                resolve({
                    success: false,
                    statusCode: 0,
                    data: null,
                    responseTime,
                    error: 'Request Timeout'
                });
            });

            if (data) {
                req.write(JSON.stringify(data));
            }
            req.end();
        });
    }

    // 응답 시간 통계 업데이트
    updateResponseTimeStats(responseTime) {
        this.stats.totalResponseTime += responseTime;
        this.stats.minResponseTime = Math.min(this.stats.minResponseTime, responseTime);
        this.stats.maxResponseTime = Math.max(this.stats.maxResponseTime, responseTime);
    }

    // Phase 1: Guest Login으로 JWT 토큰 획득
    async authenticateAsGuest() {
        try {
            const response = await this.makeHttpRequest('POST', '/api/v1/auth/guest-login', this.guestId);
            
            if (response.success && response.data && response.data.tokens) {
                this.accessToken = response.data.tokens.accessToken;
                this.refreshToken = response.data.tokens.refreshToken;
                this.authenticated = true;
                this.stats.guestLogins++;
                
                console.log(`[Client ${this.clientId}] Guest login successful (${response.responseTime}ms)`);
                return true;
            } else {
                this.stats.guestLoginErrors++;
                console.error(`[Client ${this.clientId}] Guest login failed:`, response.error || `HTTP ${response.statusCode}`);
                return false;
            }
        } catch (error) {
            this.stats.guestLoginErrors++;
            console.error(`[Client ${this.clientId}] Guest login exception:`, error.message);
            return false;
        }
    }

    // Phase 2: WebSocket 연결 설정
    async connectWebSocket() {
        return new Promise((resolve) => {
            try {
                if (!this.accessToken) {
                    console.error(`[Client ${this.clientId}] No access token for WebSocket connection`);
                    this.stats.wsConnectionErrors++;
                    resolve(false);
                    return;
                }

                const wsUrl = `${CONFIG.wsUrl}/ws?token=${this.accessToken}`;
                this.webSocket = new WebSocket(wsUrl);

                const connectionTimeout = setTimeout(() => {
                    if (this.webSocket.readyState === WebSocket.CONNECTING) {
                        this.webSocket.terminate();
                        this.stats.wsConnectionErrors++;
                        console.error(`[Client ${this.clientId}] WebSocket connection timeout`);
                        resolve(false);
                    }
                }, 10000);

                this.webSocket.on('open', () => {
                    clearTimeout(connectionTimeout);
                    this.wsConnected = true;
                    this.stats.wsConnections++;
                    console.log(`[Client ${this.clientId}] WebSocket connected`);
                    resolve(true);
                });

                this.webSocket.on('message', (data) => {
                    this.stats.wsMessages++;
                    try {
                        const message = JSON.parse(data.toString());
                        // 채팅 응답 수신 처리
                        if (message.type === 'chat_response') {
                            this.stats.chatSuccesses++;
                        }
                    } catch (parseError) {
                        // JSON이 아닌 메시지는 무시
                    }
                });

                this.webSocket.on('error', (error) => {
                    clearTimeout(connectionTimeout);
                    this.stats.wsErrors++;
                    console.error(`[Client ${this.clientId}] WebSocket error:`, error.message);
                    resolve(false);
                });

                this.webSocket.on('close', () => {
                    this.wsConnected = false;
                    console.log(`[Client ${this.clientId}] WebSocket closed`);
                });

            } catch (error) {
                this.stats.wsConnectionErrors++;
                console.error(`[Client ${this.clientId}] WebSocket connection exception:`, error.message);
                resolve(false);
            }
        });
    }

    // Phase 3: Chat API 요청 발송
    async sendChatMessage() {
        try {
            if (!this.authenticated || !this.accessToken) {
                console.error(`[Client ${this.clientId}] Not authenticated for chat`);
                return false;
            }

            // 랜덤 캐릭터와 메시지 선택
            if (!this.selectedCharacterId) {
                this.selectedCharacterId = CONFIG.characters[Math.floor(Math.random() * CONFIG.characters.length)];
            }
            
            const message = CONFIG.chatMessages[Math.floor(Math.random() * CONFIG.chatMessages.length)];
            
            const chatRequest = {
                message: message,
                character_id: this.selectedCharacterId,
                use_tts: false, // 부하 테스트에서는 TTS 비활성화
                request_at: new Date().toISOString()
            };

            const headers = {
                'Authorization': `Bearer ${this.accessToken}`
            };

            const response = await this.makeHttpRequest('POST', '/api/v1/chat', chatRequest, headers);
            
            if (response.success) {
                this.stats.chatRequests++;
                console.log(`[Client ${this.clientId}] Chat sent successfully (${response.responseTime}ms)`);
                return true;
            } else {
                this.stats.chatErrors++;
                console.error(`[Client ${this.clientId}] Chat failed:`, response.error || `HTTP ${response.statusCode}`);
                return false;
            }
        } catch (error) {
            this.stats.chatErrors++;
            console.error(`[Client ${this.clientId}] Chat exception:`, error.message);
            return false;
        }
    }

    // 메인 클라이언트 실행 루프
    async start() {
        this.running = true;
        console.log(`[Client ${this.clientId}] Starting chat load test...`);

        // Phase 1: Guest Login
        const authenticated = await this.authenticateAsGuest();
        if (!authenticated) {
            console.error(`[Client ${this.clientId}] Failed to authenticate, stopping`);
            this.running = false;
            return;
        }

        // Phase 2: WebSocket Connection
        const wsConnected = await this.connectWebSocket();
        if (!wsConnected) {
            console.error(`[Client ${this.clientId}] Failed to connect WebSocket, continuing without real-time responses`);
        }

        // Phase 3 & 4: Chat Loop
        while (this.running) {
            await this.sendChatMessage();
            
            // 채팅 간격 대기 (랜덤 지터 추가)
            const delay = CONFIG.chatInterval + (Math.random() * 2000 - 1000); // ±1초 지터
            await new Promise(resolve => setTimeout(resolve, Math.max(1000, delay)));
        }

        // Phase 5: Cleanup
        this.cleanup();
        console.log(`[Client ${this.clientId}] Stopped`);
    }

    stop() {
        this.running = false;
    }

    cleanup() {
        if (this.webSocket && this.webSocket.readyState === WebSocket.OPEN) {
            this.webSocket.close();
        }
    }

    getStats() {
        const totalRequests = this.stats.guestLogins + this.stats.chatRequests;
        const avgResponseTime = totalRequests > 0 ? 
            Math.round(this.stats.totalResponseTime / totalRequests) : 0;

        return {
            ...this.stats,
            totalRequests,
            avgResponseTime,
            authSuccessRate: this.stats.guestLogins > 0 ? 
                Math.round((this.stats.guestLogins / (this.stats.guestLogins + this.stats.guestLoginErrors)) * 100) : 0,
            chatSuccessRate: this.stats.chatRequests > 0 ?
                Math.round((this.stats.chatSuccesses / this.stats.chatRequests) * 100) : 0,
            wsSuccessRate: this.stats.wsConnections > 0 ?
                Math.round((this.stats.wsConnections / (this.stats.wsConnections + this.stats.wsConnectionErrors)) * 100) : 0
        };
    }
}

class ChatLoadTestManager {
    constructor() {
        this.clients = [];
        this.startTime = null;
        this.endTime = null;
        this.statsInterval = null;
    }

    async run() {
        console.log('=== ProjectVG Chat API Load Test ===');
        console.log(`API URL: ${CONFIG.apiUrl}`);
        console.log(`WebSocket URL: ${CONFIG.wsUrl}`);
        console.log(`Clients: ${CONFIG.clients}`);
        console.log(`Duration: ${CONFIG.duration} seconds`);
        console.log(`Ramp-up: ${CONFIG.rampUp} seconds`);
        console.log(`Chat Interval: ${CONFIG.chatInterval}ms`);
        console.log(`Characters: ${CONFIG.characters.length} available`);
        console.log('');

        // API 연결 테스트
        console.log('Testing API connection...');
        try {
            const testClient = new ChatLoadTestClient(0);
            const testResult = await testClient.makeHttpRequest('GET', '/health');
            if (testResult.success) {
                console.log(`✓ API is accessible (${testResult.responseTime}ms)`);
            } else {
                console.log(`✗ API connection failed: ${testResult.error}`);
                process.exit(1);
            }
        } catch (error) {
            console.log(`✗ API connection failed: ${error.message}`);
            process.exit(1);
        }

        console.log('');
        console.log('Starting chat load test...');

        this.startTime = Date.now();

        // 클라이언트 생성 및 점진적 시작
        for (let i = 0; i < CONFIG.clients; i++) {
            const client = new ChatLoadTestClient(i + 1);
            this.clients.push(client);
            
            // 점진적 시작 (Ramp-up)
            setTimeout(() => {
                client.start();
            }, (i / CONFIG.clients) * CONFIG.rampUp * 1000);
        }

        // 통계 출력
        this.statsInterval = setInterval(() => {
            this.printStats();
        }, 10000); // 10초마다 통계 출력

        // 테스트 종료
        setTimeout(() => {
            this.stop();
        }, CONFIG.duration * 1000);

        // 종료 신호 처리
        process.on('SIGINT', () => {
            console.log('\nReceived SIGINT, stopping chat load test...');
            this.stop();
        });
    }

    stop() {
        console.log('\nStopping chat load test...');
        
        this.clients.forEach(client => client.stop());
        
        if (this.statsInterval) {
            clearInterval(this.statsInterval);
        }

        setTimeout(() => {
            this.endTime = Date.now();
            this.printFinalStats();
            process.exit(0);
        }, 5000); // 5초 대기 후 최종 통계 출력
    }

    printStats() {
        const totalStats = this.aggregateStats();
        const runtime = Math.round((Date.now() - this.startTime) / 1000);
        const chatRPS = Math.round(totalStats.chatRequests / runtime);
        const totalRPS = Math.round(totalStats.totalRequests / runtime);

        console.log(`[${runtime}s] Auth: ${totalStats.guestLogins}/${totalStats.guestLoginErrors} | Chat: ${totalStats.chatRequests}/${totalStats.chatErrors} | WS: ${totalStats.wsConnections}/${totalStats.wsMessages} | RPS: ${chatRPS}(chat)/${totalRPS}(total) | Avg: ${totalStats.avgResponseTime}ms`);
    }

    printFinalStats() {
        console.log('\n=== Chat Load Test Results ===');
        const totalStats = this.aggregateStats();
        const duration = Math.round((this.endTime - this.startTime) / 1000);
        const chatRPS = Math.round(totalStats.chatRequests / duration);
        const totalRPS = Math.round(totalStats.totalRequests / duration);

        console.log(`Duration: ${duration} seconds`);
        console.log(`\n--- Authentication Stats ---`);
        console.log(`Guest Logins: ${totalStats.guestLogins} (${totalStats.authSuccessRate}% success)`);
        console.log(`Login Errors: ${totalStats.guestLoginErrors}`);
        
        console.log(`\n--- Chat API Stats ---`);
        console.log(`Chat Requests: ${totalStats.chatRequests}`);
        console.log(`Chat Successes: ${totalStats.chatSuccesses} (${totalStats.chatSuccessRate}% success)`);
        console.log(`Chat Errors: ${totalStats.chatErrors}`);
        console.log(`Chat RPS: ${chatRPS}`);
        
        console.log(`\n--- WebSocket Stats ---`);
        console.log(`WS Connections: ${totalStats.wsConnections} (${totalStats.wsSuccessRate}% success)`);
        console.log(`WS Connection Errors: ${totalStats.wsConnectionErrors}`);
        console.log(`WS Messages Received: ${totalStats.wsMessages}`);
        console.log(`WS Errors: ${totalStats.wsErrors}`);
        
        console.log(`\n--- Overall Performance ---`);
        console.log(`Total Requests: ${totalStats.totalRequests}`);
        console.log(`Total RPS: ${totalRPS}`);
        console.log(`Response Time: Min=${totalStats.minResponseTime}ms, Avg=${totalStats.avgResponseTime}ms, Max=${totalStats.maxResponseTime}ms`);
        
        console.log('\n=== Per-Client Stats ===');
        this.clients.slice(0, 5).forEach((client, index) => { // 처음 5개 클라이언트만 표시
            const stats = client.getStats();
            console.log(`Client ${index + 1}: Auth=${stats.guestLogins}, Chat=${stats.chatRequests}, WS=${stats.wsConnections}, Avg=${stats.avgResponseTime}ms`);
        });
        if (this.clients.length > 5) {
            console.log(`... and ${this.clients.length - 5} more clients`);
        }
    }

    aggregateStats() {
        const aggregated = this.clients.reduce((total, client) => {
            const stats = client.getStats();
            return {
                guestLogins: total.guestLogins + stats.guestLogins,
                guestLoginErrors: total.guestLoginErrors + stats.guestLoginErrors,
                chatRequests: total.chatRequests + stats.chatRequests,
                chatSuccesses: total.chatSuccesses + stats.chatSuccesses,
                chatErrors: total.chatErrors + stats.chatErrors,
                wsConnections: total.wsConnections + stats.wsConnections,
                wsConnectionErrors: total.wsConnectionErrors + stats.wsConnectionErrors,
                wsMessages: total.wsMessages + stats.wsMessages,
                wsErrors: total.wsErrors + stats.wsErrors,
                totalRequests: total.totalRequests + stats.totalRequests,
                totalResponseTime: total.totalResponseTime + stats.totalResponseTime,
                minResponseTime: Math.min(total.minResponseTime, stats.minResponseTime === Infinity ? 0 : stats.minResponseTime),
                maxResponseTime: Math.max(total.maxResponseTime, stats.maxResponseTime)
            };
        }, {
            guestLogins: 0,
            guestLoginErrors: 0,
            chatRequests: 0,
            chatSuccesses: 0,
            chatErrors: 0,
            wsConnections: 0,
            wsConnectionErrors: 0,
            wsMessages: 0,
            wsErrors: 0,
            totalRequests: 0,
            totalResponseTime: 0,
            minResponseTime: Infinity,
            maxResponseTime: 0
        });

        // 계산된 통계 추가
        aggregated.avgResponseTime = aggregated.totalRequests > 0 ? 
            Math.round(aggregated.totalResponseTime / aggregated.totalRequests) : 0;
        
        aggregated.authSuccessRate = (aggregated.guestLogins + aggregated.guestLoginErrors) > 0 ? 
            Math.round((aggregated.guestLogins / (aggregated.guestLogins + aggregated.guestLoginErrors)) * 100) : 0;
        
        aggregated.chatSuccessRate = aggregated.chatRequests > 0 ?
            Math.round((aggregated.chatSuccesses / aggregated.chatRequests) * 100) : 0;
        
        aggregated.wsSuccessRate = (aggregated.wsConnections + aggregated.wsConnectionErrors) > 0 ?
            Math.round((aggregated.wsConnections / (aggregated.wsConnections + aggregated.wsConnectionErrors)) * 100) : 0;

        return aggregated;
    }
}

// 실행
if (require.main === module) {
    const manager = new ChatLoadTestManager();
    manager.run().catch(error => {
        console.error('Chat load test failed:', error);
        process.exit(1);
    });
}

module.exports = ChatLoadTestManager;
#!/usr/bin/env node

// Simple LoadTest Script for ProjectVG API
// HTTP 부하테스트를 통한 성능 모니터링

const http = require('http');
const https = require('https');
const { URL } = require('url');

// 설정
const CONFIG = {
    apiUrl: process.env.API_URL || 'http://localhost:7804',
    clients: parseInt(process.env.CLIENTS || '10'),
    duration: parseInt(process.env.DURATION || '30'),
    rampUp: parseInt(process.env.RAMP_UP || '5'),
    endpoints: [
        '/health',
        '/api/v1/monitoring/metrics',
        '/api/v1/monitoring/health-detailed',
        '/api/v1/monitoring/threadpool',
        '/api/v1/monitoring/gc'
    ]
};

class LoadTestClient {
    constructor(clientId) {
        this.clientId = clientId;
        this.stats = {
            requests: 0,
            success: 0,
            errors: 0,
            totalResponseTime: 0,
            minResponseTime: Infinity,
            maxResponseTime: 0
        };
        this.running = false;
    }

    async makeRequest(endpoint) {
        return new Promise((resolve) => {
            const startTime = Date.now();
            const url = new URL(endpoint, CONFIG.apiUrl);
            const options = {
                hostname: url.hostname,
                port: url.port || (url.protocol === 'https:' ? 443 : 80),
                path: url.pathname + url.search,
                method: 'GET',
                timeout: 5000,
                headers: {
                    'User-Agent': `LoadTest-Client-${this.clientId}`
                }
            };

            const client = url.protocol === 'https:' ? https : http;
            const req = client.request(options, (res) => {
                const responseTime = Date.now() - startTime;
                
                // 응답 데이터 소비 (메모리 누수 방지)
                res.on('data', () => {});
                res.on('end', () => {
                    this.stats.requests++;
                    this.stats.totalResponseTime += responseTime;
                    this.stats.minResponseTime = Math.min(this.stats.minResponseTime, responseTime);
                    this.stats.maxResponseTime = Math.max(this.stats.maxResponseTime, responseTime);
                    
                    if (res.statusCode >= 200 && res.statusCode < 300) {
                        this.stats.success++;
                    } else {
                        this.stats.errors++;
                    }
                    
                    resolve({ success: true, responseTime, statusCode: res.statusCode });
                });
            });

            req.on('error', (error) => {
                const responseTime = Date.now() - startTime;
                this.stats.requests++;
                this.stats.errors++;
                this.stats.totalResponseTime += responseTime;
                resolve({ success: false, responseTime, error: error.message });
            });

            req.on('timeout', () => {
                req.destroy();
                const responseTime = Date.now() - startTime;
                this.stats.requests++;
                this.stats.errors++;
                this.stats.totalResponseTime += responseTime;
                resolve({ success: false, responseTime, error: 'Timeout' });
            });

            req.end();
        });
    }

    async start() {
        this.running = true;
        console.log(`[Client ${this.clientId}] Starting load test...`);

        while (this.running) {
            // 랜덤하게 엔드포인트 선택
            const endpoint = CONFIG.endpoints[Math.floor(Math.random() * CONFIG.endpoints.length)];
            
            try {
                await this.makeRequest(endpoint);
                
                // 요청 간격 (50-200ms)
                const delay = 50 + Math.random() * 150;
                await new Promise(resolve => setTimeout(resolve, delay));
            } catch (error) {
                console.error(`[Client ${this.clientId}] Error:`, error.message);
            }
        }
        
        console.log(`[Client ${this.clientId}] Stopped`);
    }

    stop() {
        this.running = false;
    }

    getStats() {
        return {
            ...this.stats,
            avgResponseTime: this.stats.requests > 0 ? 
                Math.round(this.stats.totalResponseTime / this.stats.requests) : 0,
            successRate: this.stats.requests > 0 ? 
                Math.round((this.stats.success / this.stats.requests) * 100) : 0
        };
    }
}

class LoadTestManager {
    constructor() {
        this.clients = [];
        this.startTime = null;
        this.endTime = null;
        this.statsInterval = null;
    }

    async run() {
        console.log('=== ProjectVG API Load Test ===');
        console.log(`API URL: ${CONFIG.apiUrl}`);
        console.log(`Clients: ${CONFIG.clients}`);
        console.log(`Duration: ${CONFIG.duration} seconds`);
        console.log(`Ramp-up: ${CONFIG.rampUp} seconds`);
        console.log(`Endpoints: ${CONFIG.endpoints.join(', ')}`);
        console.log('');

        // API 연결 테스트
        console.log('Testing API connection...');
        try {
            const testResult = await new LoadTestClient(0).makeRequest('/health');
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
        console.log('Starting load test...');

        this.startTime = Date.now();

        // 클라이언트 생성 및 점진적 시작
        for (let i = 0; i < CONFIG.clients; i++) {
            const client = new LoadTestClient(i + 1);
            this.clients.push(client);
            
            // 점진적 시작 (Ramp-up)
            setTimeout(() => {
                client.start();
            }, (i / CONFIG.clients) * CONFIG.rampUp * 1000);
        }

        // 통계 출력
        this.statsInterval = setInterval(() => {
            this.printStats();
        }, 5000);

        // 테스트 종료
        setTimeout(() => {
            this.stop();
        }, CONFIG.duration * 1000);

        // 종료 신호 처리
        process.on('SIGINT', () => {
            console.log('\nReceived SIGINT, stopping load test...');
            this.stop();
        });
    }

    stop() {
        console.log('\nStopping load test...');
        
        this.clients.forEach(client => client.stop());
        
        if (this.statsInterval) {
            clearInterval(this.statsInterval);
        }

        setTimeout(() => {
            this.endTime = Date.now();
            this.printFinalStats();
            process.exit(0);
        }, 2000);
    }

    printStats() {
        const totalStats = this.aggregateStats();
        const runtime = Math.round((Date.now() - this.startTime) / 1000);
        const rps = Math.round(totalStats.requests / runtime);

        console.log(`[${runtime}s] Requests: ${totalStats.requests} | Success: ${totalStats.success} | Errors: ${totalStats.errors} | RPS: ${rps} | Avg: ${totalStats.avgResponseTime}ms`);
    }

    printFinalStats() {
        console.log('\n=== Load Test Results ===');
        const totalStats = this.aggregateStats();
        const duration = Math.round((this.endTime - this.startTime) / 1000);
        const rps = Math.round(totalStats.requests / duration);

        console.log(`Duration: ${duration} seconds`);
        console.log(`Total Requests: ${totalStats.requests}`);
        console.log(`Successful: ${totalStats.success} (${totalStats.successRate}%)`);
        console.log(`Errors: ${totalStats.errors}`);
        console.log(`Requests/sec: ${rps}`);
        console.log(`Response Time: Min=${totalStats.minResponseTime}ms, Avg=${totalStats.avgResponseTime}ms, Max=${totalStats.maxResponseTime}ms`);
        
        console.log('\n=== Per-Client Stats ===');
        this.clients.forEach((client, index) => {
            const stats = client.getStats();
            console.log(`Client ${index + 1}: ${stats.requests} requests, ${stats.successRate}% success, ${stats.avgResponseTime}ms avg`);
        });
    }

    aggregateStats() {
        return this.clients.reduce((total, client) => {
            const stats = client.getStats();
            return {
                requests: total.requests + stats.requests,
                success: total.success + stats.success,
                errors: total.errors + stats.errors,
                totalResponseTime: total.totalResponseTime + stats.totalResponseTime,
                minResponseTime: Math.min(total.minResponseTime, stats.minResponseTime === Infinity ? 0 : stats.minResponseTime),
                maxResponseTime: Math.max(total.maxResponseTime, stats.maxResponseTime),
                avgResponseTime: 0, // 계산 후 설정
                successRate: 0 // 계산 후 설정
            };
        }, {
            requests: 0,
            success: 0, 
            errors: 0,
            totalResponseTime: 0,
            minResponseTime: Infinity,
            maxResponseTime: 0
        });
    }
}

// 실행
if (require.main === module) {
    const manager = new LoadTestManager();
    manager.run().catch(error => {
        console.error('Load test failed:', error);
        process.exit(1);
    });
}

module.exports = LoadTestManager;
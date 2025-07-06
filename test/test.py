import asyncio
import websockets
import requests
import json
import time
from datetime import datetime

class ChatApp:
    def __init__(self, http_url="http://localhost:5287", ws_url="ws://localhost:5287/ws"):
        self.http_url = http_url
        self.ws_url = ws_url
        self.websocket = None
        self.session_id = None
        
    async def connect_websocket(self):
        """WebSocket 연결"""
        try:
            # 세션 ID가 있으면 재연결, 없으면 새 연결
            ws_url = self.ws_url
            if self.session_id:
                ws_url = f"{self.ws_url}?sessionId={self.session_id}"
                print(f"기존 세션으로 재연결 시도: {self.session_id}")
            else:
                print("새 세션 연결 시도")
            
            print(f"WebSocket URL: {ws_url}")
            self.websocket = await websockets.connect(ws_url)
            print("WebSocket 연결됨")
            
            # 세션 ID 수신 대기
            await self.wait_for_session_id()
            
            return True
        except Exception as e:
            print(f"WebSocket 연결 실패: {e}")
            print(f"연결 URL: {ws_url}")
            return False
    
    async def wait_for_session_id(self):
        """서버로부터 세션 ID 수신"""
        try:
            print("세션 ID 수신 대기 중...")
            message = await asyncio.wait_for(self.websocket.recv(), timeout=5.0)
            data = json.loads(message)
            
            if data.get("type") == "session_id":
                self.session_id = data.get("session_id")
                print(f"세션 ID 수신: {self.session_id}")
            else:
                print(f"예상치 못한 메시지: {message}")
        except asyncio.TimeoutError:
            print("세션 ID 수신 타임아웃")
            raise
        except Exception as e:
            print(f"세션 ID 수신 실패: {e}")
            raise
    

    
    async def listen_for_responses(self):
        """WebSocket에서 응답 수신"""
        if not self.websocket:
            return
            
        try:
            while True:
                message = await self.websocket.recv()
                
                # 세션 ID 메시지는 무시
                try:
                    data = json.loads(message)
                    if data.get("type") == "session_id":
                        continue
                except:
                    pass
                
                print(f"\nAI: {message}")
                print("-" * 50)
                break
                    
        except websockets.exceptions.ConnectionClosed:
            print("WebSocket 연결이 종료됨")
        except Exception as e:
            print(f"WebSocket 수신 오류: {e}")
    
    def send_chat_request(self, message):
        """HTTP로 채팅 요청 전송"""
        try:
            # 세션 ID가 없으면 오류
            if not self.session_id:
                print("세션 ID가 없습니다. WebSocket 연결을 먼저 해주세요.")
                return False
            
            payload = {
                "session_id": self.session_id,
                "actor": "test_user",
                "message": message,
                "action": "chat"
            }
            
            print("요청 전송 중...")
            response = requests.post(f"{self.http_url}/api/chat", json=payload, timeout=30)
            
            if response.status_code == 200:
                result = response.json()
                self.session_id = result.get("id", self.session_id)
                print(f"요청 성공 (세션: {self.session_id})")
                return True
            else:
                print(f"요청 실패: {response.status_code} - {response.text}")
                return False
                
        except requests.exceptions.Timeout:
            print("요청 시간 초과")
            return False
        except Exception as e:
            print(f"요청 오류: {e}")
            return False
    
    async def chat_loop(self):
        """대화 루프"""
        print("채팅 앱 시작")
        print("=" * 50)
        
        if not await self.connect_websocket():
            return
        
        print("\n대화를 시작하세요 (종료: quit, exit, 종료)")
        print("=" * 50)
        
        while True:
            try:
                user_input = input("\n나: ").strip()
                
                if user_input.lower() in ['quit', 'exit', '종료']:
                    print("채팅 종료")
                    break
                
                if not user_input:
                    print("메시지를 입력하세요")
                    continue
                
                # WebSocket 연결이 없으면 연결 (첫 실행 시에만)
                if not self.websocket:
                    print("WebSocket 연결 시도...")
                    if not await self.connect_websocket():
                        print("WebSocket 연결 실패. 다시 시도하세요")
                        continue
                
                if not self.send_chat_request(user_input):
                    print("요청 전송 실패. 다시 시도하세요")
                    continue
                
                print("AI 응답 대기 중...")
                await self.listen_for_responses()
                
            except KeyboardInterrupt:
                print("\n채팅 종료")
                break
            except Exception as e:
                print(f"오류: {e}")
                continue
        
        if self.websocket:
            await self.websocket.close()
            print("WebSocket 연결 종료")

async def main():
    app = ChatApp()
    await app.chat_loop()

if __name__ == "__main__":
    asyncio.run(main())

import asyncio
import websockets
import requests
import json
import time
from datetime import datetime
import simpleaudio as sa
import threading

class ChatApp:
    def __init__(self, http_url="http://localhost:5287", ws_url="ws://localhost:5287/ws"):
        self.http_url = http_url
        self.ws_url = ws_url
        self.websocket = None
        self.session_id = None
    
    def play_audio_file(self, filename):
        """저장된 오디오 파일을 simpleaudio로 자동 재생 (백그라운드 스레드)"""
        def _play():
            try:
                print(f"[클라이언트] 오디오 자동 재생 시작: {filename}")
                wave_obj = sa.WaveObject.from_wave_file(filename)
                play_obj = wave_obj.play()
                play_obj.wait_done()
                print("[클라이언트] 오디오 자동 재생 완료")
            except Exception as e:
                print(f"[클라이언트] 오디오 재생 실패: {e}")
        threading.Thread(target=_play, daemon=True).start()

    async def connect_websocket(self):
        """WebSocket 연결"""
        try:
            ws_url = self.ws_url
            if self.session_id:
                ws_url = f"{self.ws_url}?sessionId={self.session_id}"
                print(f"기존 세션으로 재연결 시도: {self.session_id}")
            else:
                print("새 세션 연결 시도")
            print(f"WebSocket URL: {ws_url}")
            self.websocket = await websockets.connect(ws_url)
            print("WebSocket 연결됨")
            await self.wait_for_session_id()
            return True
        except Exception as e:
            print(f"WebSocket 연결 실패: {e}")
            print(f"연결 URL: {ws_url}")
            return False
    
    async def wait_for_session_id(self):
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
    
    async def listen_for_responses(self, timeout=5):
        """WebSocket에서 응답 수신 (텍스트/오디오 모두, 오디오는 백그라운드 재생, 타임아웃 처리)"""
        if not self.websocket:
            return
        try:
            text_received = False
            start_time = time.time()
            while True:
                # 타임아웃 체크
                if time.time() - start_time > timeout:
                    print("오디오 응답 타임아웃, 다음 입력을 받습니다.")
                    break
                try:
                    message = await asyncio.wait_for(self.websocket.recv(), timeout=timeout)
                except asyncio.TimeoutError:
                    print("WebSocket 응답 타임아웃")
                    break
                if isinstance(message, bytes):
                    filename = f"output_{datetime.now().strftime('%Y%m%d_%H%M%S')}.wav"
                    with open(filename, "wb") as f:
                        f.write(message)
                    print(f"[클라이언트] 오디오 파일 저장: {filename}")
                    self.play_audio_file(filename)
                    continue
                try:
                    data = json.loads(message)
                    if data.get("type") == "session_id":
                        continue
                except Exception:
                    pass
                print(f"\nAI: {message}")
                print("-" * 50)
                text_received = True
                # 텍스트만 오면 바로 break
                break
        except websockets.exceptions.ConnectionClosed:
            print("WebSocket 연결이 종료됨")
        except Exception as e:
            print(f"WebSocket 수신 오류: {e}")
    
    def send_chat_request(self, message):
        try:
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
            response = requests.post(f"{self.http_url}/api/v1/chat", json=payload, timeout=30)
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

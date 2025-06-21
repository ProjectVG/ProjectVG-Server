import asyncio
import websockets
import threading
import uuid
import requests
import json

# 연결할 서버 주소
session_id = str(uuid.uuid4())
uri = f"ws://localhost:5287/ws?sessionId={session_id}"

# 전역으로 소켓 저장
ws_connection = None

# 수신 쓰레드용 루프
async def listen():
    global ws_connection
    try:
        async for message in ws_connection:
            print(f"Received: {message}")
    except websockets.ConnectionClosed:
        print("WebSocket connection closed")
    except Exception as e:
        print(f"Error in listen: {e}")

def send_talk_request():
    url = "http://localhost:5287/api/talk"
    data = {
        "id": session_id,
        "actor": "test_user",
        "message": "Hello, this is a test message",
        "action": "test"
    }
    
    try:
        response = requests.post(url, json=data)
        print(f"HTTP request sent, status: {response.status_code}")
        if response.status_code == 200:
            print("Request accepted by server")
        else:
            print(f"Request failed: {response.text}")
    except Exception as e:
        print(f"HTTP request error: {e}")

# 연결 함수 (메인)
async def connect():
    global ws_connection
    try:
        print(f"Connecting to {uri}")
        ws_connection = await websockets.connect(uri)
        print("Connected successfully")

        # 연결 후 간단한 메시지 전송
        await ws_connection.send("Hello from Python client!")
        print("Sent hello message")

        # 수신 쓰레드 시작
        threading.Thread(target=lambda: asyncio.run(listen()), daemon=True).start()

        # send actual talk request
        print("Sending talk request...")
        send_talk_request()

        # CLI 입력으로 메시지 전송
        while True:
            msg = input("Enter message (or 'exit'): ")
            if msg.strip().lower() == "exit":
                break
            await ws_connection.send(msg)

        await ws_connection.close()
        print("Disconnected")

    except Exception as e:
        print(f"Connection failed: {e}")

# 실행
if __name__ == "__main__":
    asyncio.run(connect())

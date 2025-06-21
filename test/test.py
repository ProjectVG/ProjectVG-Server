import asyncio
import websockets
import uuid
import requests
import json
import time

# 연결할 서버 주소
session_id = str(uuid.uuid4())
uri = f"ws://localhost:5287/ws?sessionId={session_id}"

# 전역으로 소켓 저장
ws_connection = None

# 수신 쓰레드용 루프
async def listen(websocket):
    try:
        async for message in websocket:
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
    
    start_time = time.time()
    try:
        response = requests.post(url, json=data)
        http_time = time.time() - start_time
        print(f"HTTP request sent, status: {response.status_code}, time: {http_time:.3f}s")
        if response.status_code == 200:
            print("Request accepted by server")
        else:
            print(f"Request failed: {response.text}")
        return http_time
    except Exception as e:
        print(f"HTTP request error: {e}")
        return time.time() - start_time

# 연결 함수 (메인)
async def connect():
    try:
        print(f"Connecting to {uri}")
        async with websockets.connect(uri) as websocket:
            print("Connected successfully")

            await websocket.send("Hello from Python client!")
            print("Sent hello message")

            # Start listening task
            listen_task = asyncio.create_task(listen(websocket))
            
            # Send talk request and measure time
            print("Sending talk request...")
            http_time = send_talk_request()

            # Wait for WebSocket response and measure total time
            start_wait = time.time()
            await asyncio.sleep(1)
            
            send_talk_request()

            await asyncio.sleep(1)
            
            total_time = time.time() - start_wait
            print(f"Total response time: {total_time:.3f}s")
            
            # Main input loop
            while True:
                msg = input("Enter message (or 'exit'): ")
                if msg.strip().lower() == "exit":
                    break
                await websocket.send(msg)

            # Cancel listen task
            listen_task.cancel()
            try:
                await listen_task
            except asyncio.CancelledError:
                pass

        print("Disconnected")

    except Exception as e:
        print(f"Connection failed: {e}")

# 실행
if __name__ == "__main__":
    asyncio.run(connect())

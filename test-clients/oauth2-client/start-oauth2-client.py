#!/usr/bin/env python3
"""
OAuth2 Test Client Server
Runs OAuth2 test client on port 3000.
"""

import http.server
import socketserver
import os
import sys
from pathlib import Path

# Current script directory
SCRIPT_DIR = Path(__file__).parent.absolute()
HTML_FILE = SCRIPT_DIR / "oauth2-test-client.html"

class OAuth2TestHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        """
        루트 경로('/')를 OAuth2 테스트 클라이언트 페이지('/oauth2-test-client.html')로 302 리디렉트하고,
        요청이 '/oauth2-test-client.html'일 경우 파일(HTML_FILE)의 존재를 확인하여 없으면 404 응답을 보낸다.
        그 밖의 경로에 대해서는 기본 SimpleHTTPRequestHandler의 처리로 위임한다.
        
        부작용:
        - HTTP 응답(리디렉션 또는 오류)을 직접 전송한다.
        """
        if self.path == '/':
            # Redirect root path to OAuth2 test client
            self.send_response(302)
            self.send_header('Location', '/oauth2-test-client.html')
            self.end_headers()
            return
        
        # Check if HTML file exists
        if self.path == '/oauth2-test-client.html':
            if not HTML_FILE.exists():
                self.send_error(404, f"File not found: {HTML_FILE}")
                return
        
        # Default HTTP server behavior
        super().do_GET()

def main():
    """
    로컬 HTTP 서버를 실행하여 같은 디렉터리의 oauth2-test-client.html을 제공하고 OAuth2 테스트 안내를 출력합니다.
    
    서버를 포트 3000에서 시작하며, 시작 전에 HTML 파일 존재를 확인합니다. HTML 파일이 없으면 에러 메시지를 출력하고 프로세스를 종료합니다. 작업 디렉터리를 스크립트 디렉터리로 변경한 뒤 SimpleHTTPRequestHandler를 기반으로 하는 서버(OAuth2TestHandler)를 생성하고 serve_forever()로 요청을 처리합니다. 실행 중 Ctrl+C(KeyboardInterrupt)로 중지하면 정상 종료 메시지를 출력하고, 포트가 이미 사용 중인 경우에는 해당 사실을 안내하며 다른 포트 사용 또는 프로세스 종료를 제안합니다. 기타 예외는 "Unexpected error" 메시지와 함께 출력됩니다.
    
    부수 효과:
    - 현재 작업 디렉터리를 SCRIPT_DIR로 변경합니다.
    - HTML 파일이 없을 경우 sys.exit(1)로 프로세스를 종료합니다.
    - 표준 출력에 서버 상태와 사용 안내를 출력합니다.
    """
    PORT = 3000
    
    # Check if HTML file exists
    if not HTML_FILE.exists():
        print(f"HTML file not found: {HTML_FILE}")
        print("Please check if oauth2-test-client.html exists in the same directory.")
        sys.exit(1)
    
    # Change working directory to script directory
    os.chdir(SCRIPT_DIR)
    
    try:
        with socketserver.TCPServer(("", PORT), OAuth2TestHandler) as httpd:
            print(f"OAuth2 Test Client server started!")
            print(f"URL: http://localhost:{PORT}")
            print(f"Service directory: {SCRIPT_DIR}")
            print(f"HTML file: {HTML_FILE}")
            print("\n" + "="*50)
            print("OAuth2 Test Instructions:")
            print("1. Open http://localhost:3000 in browser")
            print("2. Click 'Generate PKCE' button")
            print("3. Enter Google OAuth2 Client ID")
            print("4. Click 'Start OAuth2 Login' button")
            print("5. Login with Google and authorize")
            print("6. Check results")
            print("="*50)
            print("\nPress Ctrl+C to stop the server.\n")
            
            httpd.serve_forever()
            
    except KeyboardInterrupt:
        print("\n\nServer stopped.")
    except OSError as e:
        if e.errno == 48:  # Address already in use
            print(f"Port {PORT} is already in use.")
            print("Please terminate other processes or use a different port.")
        else:
            print(f"Server start failed: {e}")
    except Exception as e:
        print(f"Unexpected error: {e}")

if __name__ == "__main__":
    main()

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

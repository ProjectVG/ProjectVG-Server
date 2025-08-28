// 환경에 따른 포트 설정
// Development 환경(7901)에서 실행되면 7901 사용, 아니면 기본값 7900 사용
const currentPort = window.location.port;
const isDevelopment = currentPort === '7901';
const serverPort = isDevelopment ? '7901' : '7900';
const currentHost = window.location.hostname || 'localhost';
const ENDPOINT = `${currentHost}:${serverPort}`;
const WS_URL = `ws://${ENDPOINT}/ws`;
const HTTP_URL = `http://${ENDPOINT}/api/v1/chat`;
const LOGIN_URL = `http://${ENDPOINT}/api/v1/auth/guest-login`;
const SERVER_MESSAGE_TYPE = "json";
let ws = null;
let reconnectAttempts = 0;
const MAX_RECONNECT = 3;
let authToken = null;
let isLoggedIn = false;
let userId = null;

const statusBox = document.getElementById('status');
const loginStatus = document.getElementById('login-status');
const wsStatus = document.getElementById('ws-status');
const sessionIdDisplay = document.getElementById('session-id');
const serverInfo = document.getElementById('server-info');
const chatLog = document.getElementById('chat-log');
const userInput = document.getElementById('user-input');
const sendBtn = document.getElementById('send-btn');
const audioPlayer = document.getElementById('audio-player');
const characterSelect = document.getElementById('character-select');
const guestIdInput = document.getElementById('guest-id');
const loginBtn = document.getElementById('login-btn');
const loginSection = document.getElementById('login-section');
const includeAudioCheckbox = document.getElementById('include-audio');

const audioQueue = [];
let isPlayingAudio = false;
let serverConfig = null;

// 서버 정보 표시
serverInfo.textContent = ENDPOINT;

// 서버 설정 확인
async function checkServerConfig() {
  try {
    const response = await fetch(`${HTTP_URL.replace('/chat', '')}/config`);
    if (response.ok) {
      serverConfig = await response.json();
      console.log("서버 설정:", serverConfig);
    }
  } catch (e) {
    console.warn("서버 설정 확인 실패:", e);
  }
}

// Guest 로그인 함수
async function guestLogin() {
  const guestId = guestIdInput.value.trim();
  if (!guestId) {
    appendLog('<span style="color:red">[로그인 오류] 게스트 ID를 입력하세요</span>');
    return;
  }

  setLoginStatus('로그인 중...', false);
  loginBtn.disabled = true;
  loginBtn.textContent = '로그인 중...';

  try {
    const response = await fetch(LOGIN_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(guestId)
    });

    if (response.ok) {
      const data = await response.json();
      if (data.success && data.tokens) {
        authToken = data.tokens.accessToken;
        userId = data.user.id;
        isLoggedIn = true;
        setLoginStatus('로그인 성공', false);
        updateOverallStatus();
        
        appendLog(`<b>[로그인 성공]</b> 게스트 ID: ${guestId}`);
        appendLog(`<small>사용자: ${data.user.username} (${data.user.email})</small>`);
        updateSessionId(userId);
        
        loginSection.style.display = 'none';
        
        // 로그인 성공 후 WebSocket 연결 시작
        connectWebSocket();
      } else {
        throw new Error(data.message || '로그인 실패');
      }
    } else {
      const errorData = await response.json();
      throw new Error(errorData.message || `HTTP ${response.status}`);
    }
  } catch (error) {
    console.error('로그인 오류:', error);
    setLoginStatus('로그인 실패', true);
    updateOverallStatus();
    appendLog(`<span style="color:red">[로그인 오류] ${error.message}</span>`);
  } finally {
    loginBtn.disabled = false;
    loginBtn.textContent = '게스트 로그인';
  }
}

// 바이너리 메시지 파싱 함수
function parseBinaryMessage(arrayBuffer) {
  try {
    const dataView = new DataView(arrayBuffer);
    let offset = 0;

    // 메시지 타입 확인 (1바이트)
    const messageType = dataView.getUint8(offset);
    offset += 1;

    if (messageType !== 0x03) {
      return null; 
    }

    // 텍스트 읽기
    const textLength = dataView.getUint32(offset, true);
    offset += 4;
    let text = null;
    if (textLength > 0) {
      const textBytes = new Uint8Array(arrayBuffer, offset, textLength);
      text = new TextDecoder().decode(textBytes);
      offset += textLength;
    }

    // 오디오 데이터 읽기
    const audioLength = dataView.getUint32(offset, true);
    offset += 4;
    let audioData = null;
    if (audioLength > 0) {
      audioData = new Uint8Array(arrayBuffer, offset, audioLength);
      offset += audioLength;
    }

    // 오디오 길이 읽기 (float)
    const audioDuration = dataView.getFloat32(offset, true);

    return {
      sessionId,
      text,
      audioData,
      audioLength: audioDuration
    };
  } catch (e) {
    console.error("바이너리 메시지 파싱 오류:", e);
    return null;
  }
}

function setLoginStatus(message, isError = false) {
  loginStatus.textContent = message;
  loginStatus.style.color = isError ? '#dc3545' : '#2e7d32';
}

function setWSStatus(message, isError = false) {
  wsStatus.textContent = message;
  wsStatus.style.color = isError ? '#dc3545' : '#2e7d32';
}

function updateOverallStatus() {
  const hasError = loginStatus.style.color === 'rgb(220, 53, 69)' || wsStatus.style.color === 'rgb(220, 53, 69)';
  statusBox.style.background = hasError ? '#ffebee' : '#e8f5e8';
  statusBox.style.borderColor = hasError ? '#ffcdd2' : '#c8e6c9';
}

function updateSessionId(userId) {
  if (userId) {
    sessionIdDisplay.textContent = userId.substring(0, 20) + '...';
    sessionIdDisplay.title = userId;
    sessionIdDisplay.style.color = '#4caf50';
  } else {
    sessionIdDisplay.textContent = '없음';
    sessionIdDisplay.title = '';
    sessionIdDisplay.style.color = '#6c757d';
  }
}

function playNextAudio() {
  console.log("playNextAudio 호출:", {
    queueLength: audioQueue.length,
    isPlayingAudio: isPlayingAudio
  });

  if (audioQueue.length === 0) {
    isPlayingAudio = false;
    audioPlayer.style.display = "none";
    console.log("오디오 큐가 비어있음");
    return;
  }
  const blob = audioQueue.shift();
  console.log("오디오 재생 시작:", {
    blobSize: blob.size,
    blobType: blob.type
  });

  audioPlayer.src = URL.createObjectURL(blob);
  audioPlayer.style.display = "";
  isPlayingAudio = true;

  const playPromise = audioPlayer.play();
  if (playPromise !== undefined) {
    playPromise.then(() => {
      console.log("오디오 재생 성공");
      appendLog(`<i>[오디오 재생]</i>`);
    }).catch(error => {
      console.error("오디오 재생 실패:", error);
      appendLog(`<i>[오디오 재생 실패: ${error.message}]</i>`);
    });
  }
}

audioPlayer.onended = () => playNextAudio();

function appendLog(text) {
  chatLog.innerHTML += `<div>${text}</div>`;
  chatLog.scrollTop = chatLog.scrollHeight;
}

function connectWebSocket() {
  if (!isLoggedIn || !authToken) {
    setWSStatus("로그인 필요", true);
    updateOverallStatus();
    return;
  }

  setWSStatus("연결 중...", false);
  updateOverallStatus();
  
  // JWT 토큰을 쿼리 파라미터로 전달 (브라우저 제한으로 헤더 사용 불가)
  ws = new WebSocket(`${WS_URL}?token=${authToken}`);
  ws.binaryType = "arraybuffer";

  ws.onopen = () => {
    setWSStatus("연결됨", false);
    updateOverallStatus();
    reconnectAttempts = 0;
    checkServerConfig();
  };

  ws.onmessage = (event) => {
    if (typeof event.data === "string") {
      try {
        const data = JSON.parse(event.data);
        console.log("수신된 메시지:", data);

        // 새로운 WebSocket 메시지 구조 처리 (우선순위)
        if (data.type && data.data !== undefined) {
          console.log(`메시지 타입: ${data.type}`);

          // 세션 처리 (UserId 확인)
          if (data.type === "session") {
            const receivedUserId = data.data.user_id;
            if (receivedUserId) {
              appendLog(`<i>[WebSocket 세션 확인] User ID: ${receivedUserId.substring(0, 8)}...</i>`);
            }
            return;
          }

          // 채팅 메시지 처리
          if (data.type === "chat") {
            const chatData = data.data;
            let messageText = "";

            // 텍스트 메시지가 있는 경우
            if (chatData.text) {
              messageText += `<b>AI:</b> ${chatData.text}`;
            }

            // 오디오 데이터가 있는 경우
            if (chatData.audio_data) {
              try {
                console.log("오디오 데이터 수신:", {
                  audioDataLength: chatData.audio_data.length,
                  audioFormat: chatData.audio_format,
                  hasAudioData: !!chatData.audio_data
                });

                // Base64 디코딩
                const audioBytes = atob(chatData.audio_data);
                const audioArray = new Uint8Array(audioBytes.length);
                for (let i = 0; i < audioBytes.length; i++) {
                  audioArray[i] = audioBytes.charCodeAt(i);
                }

                console.log("오디오 배열 생성:", {
                  arrayLength: audioArray.length,
                  blobType: chatData.audio_format || 'audio/wav'
                });

                const blob = new Blob([audioArray], { type: `audio/${chatData.audio_format || 'wav'}` });
                console.log("Blob 생성 완료:", {
                  blobSize: blob.size,
                  blobType: blob.type
                });

                audioQueue.push(blob);
                if (!isPlayingAudio) playNextAudio();

                messageText += chatData.text ? " [오디오 포함]" : "<b>AI:</b> [음성 메시지]";
              } catch (e) {
                console.error("오디오 데이터 파싱 오류:", e);
                messageText += " [오디오 파싱 오류]";
              }
            }

            if (messageText) {
              appendLog(messageText);
            }

            // 메타데이터 표시 (개발용)
            if (chatData.metadata) {
              appendLog(`<small style='color:#666'>[메타데이터: ${JSON.stringify(chatData.metadata)}]</small>`);
            }

            return;
          }

          // 기타 메시지 타입 처리
          appendLog(`<small style='color:#666'>[메시지 타입: ${data.type}]</small>`);
          return;
        }

        // sessionId 로직은 더 이상 사용하지 않음

        if (data.type === "chat") {
          let messageText = "";

          // 텍스트 메시지가 있는 경우
          if (data.text) {
            messageText += `<b>AI:</b> ${data.text}`;
          }

          // 오디오 데이터가 있는 경우
          if (data.audio_data) {
            try {
              // Base64 디코딩
              const audioBytes = atob(data.audio_data);
              const audioArray = new Uint8Array(audioBytes.length);
              for (let i = 0; i < audioBytes.length; i++) {
                audioArray[i] = audioBytes.charCodeAt(i);
              }

              const blob = new Blob([audioArray], { type: data.audio_format || 'audio/wav' });
              audioQueue.push(blob);
              if (!isPlayingAudio) playNextAudio();

              messageText += data.text ? " [오디오 포함]" : "<b>AI:</b> [음성 메시지]";
            } catch (e) {
              console.error("오디오 데이터 파싱 오류:", e);
              messageText += " [오디오 파싱 오류]";
            }
          }

          if (messageText) {
            appendLog(messageText);
          }

          // 메타데이터 표시 (개발용)
          if (data.metadata) {
            appendLog(`<small style='color:#666'>[메타데이터: ${JSON.stringify(data.metadata)}]</small>`);
          }

          return;
        }

        // 기존 텍스트 메시지 처리 (하위 호환성)
        appendLog(`<b>AI:</b> ${event.data}`);
      } catch (e) {
        // JSON 파싱 실패 시 일반 텍스트로 처리
        appendLog(`<b>AI:</b> ${event.data}`);
      }
    } else if (event.data instanceof ArrayBuffer) {
      // 바이너리 메시지 처리
      console.log("바이너리 메시지 수신:", {
        dataLength: event.data.byteLength,
        dataType: typeof event.data
      });

      try {
        // 바이너리 프로토콜 파싱 시도
        const result = parseBinaryMessage(event.data);
        if (result) {
          // 세션 ID 업데이트
          if (result.sessionId) {
            sessionId = result.sessionId;
            updateSessionId(sessionId);
          }

          let messageText = "";

          if (result.text) {
            messageText += `<b>AI:</b> ${result.text}`;
          }

          if (result.audioData && result.audioData.length > 0) {
            const blob = new Blob([result.audioData], { type: "audio/wav" });
            audioQueue.push(blob);
            if (!isPlayingAudio) playNextAudio();

            messageText += result.text ? " [오디오 포함]" : "<b>AI:</b> [음성 메시지]";
          }

          if (messageText) {
            appendLog(messageText);
          }

          return;
        }
      } catch (e) {
        console.error("바이너리 메시지 파싱 오류:", e);
      }

      const blob = new Blob([event.data], { type: "audio/wav" });
      audioQueue.push(blob);
      if (!isPlayingAudio) playNextAudio();
    }
  };

  ws.onclose = () => {
    setWSStatus("연결 종료됨", true);
    updateOverallStatus();
    appendLog("<b>[WebSocket 연결 종료]</b>");
    if (isLoggedIn) tryReconnect();
  };

  ws.onerror = () => {
    setWSStatus("오류 발생", true);
    updateOverallStatus();
    appendLog("<b>[WebSocket 오류]</b>");
  };
}

function tryReconnect() {
  if (reconnectAttempts < MAX_RECONNECT) {
    reconnectAttempts++;
    setWSStatus(`재연결 시도 중... (${reconnectAttempts}/${MAX_RECONNECT})`, true);
    updateOverallStatus();
    setTimeout(connectWebSocket, 1000 * reconnectAttempts);
  } else {
    setWSStatus("재연결 실패. 새로고침 해주세요.", true);
    updateOverallStatus();
  }
}

function sendChat() {
  const msg = userInput.value.trim();
  if (!msg) return;
  appendLog(`<b>나:</b> ${msg}`);
  userInput.value = "";

  const payload = {
    message: msg,
    action: "chat",
    character_id: characterSelect.value,
    use_tts: includeAudioCheckbox.checked,
    request_at: new Date().toISOString()
  };

  console.log(includeAudioCheckbox.checked)

  const headers = { "Content-Type": "application/json" };
  if (authToken) {
    headers["Authorization"] = `Bearer ${authToken}`;
  }

  fetch(HTTP_URL, {
    method: "POST",
    headers: headers,
    body: JSON.stringify(payload)
  })
    .then(res => {
      if (!res.ok) {
        appendLog(`<span style='color:red'>[HTTP 오류] 상태코드: ${res.status}</span>`);
        console.error("HTTP 오류", res);
      }
      return res.json();
    })
    .catch(err => {
      appendLog(`<span style='color:red'>[HTTP 오류] ${err}</span>`);
      console.error(err);
    });
}

// Event listeners
sendBtn.onclick = sendChat;
userInput.onkeydown = (e) => { if (e.key === "Enter") sendChat(); };

loginBtn.onclick = guestLogin;
guestIdInput.onkeydown = (e) => { if (e.key === "Enter") guestLogin(); };

characterSelect.onchange = () => {
  userInput.value = "";
  chatLog.innerHTML = "";
  appendLog(`<i>[캐릭터 변경됨: ${characterSelect.options[characterSelect.selectedIndex].text}]</i>`);
};

// 초기화 - 로그인을 기다림
appendLog('<b>[시작]</b> 게스트 ID를 입력하고 로그인하세요.');
# Voice Server API Reference

## Overview
This document provides a comprehensive reference for integrating with the Voice Server API, including authentication, request/response formats, error handling, and best practices.

---

## Authentication
All API requests require an API key provided in the HTTP header:

- **Header:** `x-sup-api-key: [YOUR_API_KEY]`

---

## Endpoints

### 1. Text-to-Speech (TTS)

- **Endpoint:** `POST /v1/text-to-speech/{voice_id}`
- **Content-Type:** `application/json`
- **Authentication:** Required (`x-sup-api-key`)

#### Request Body
| Field           | Type     | Required | Description                                                      |
|-----------------|----------|----------|------------------------------------------------------------------|
| text            | string   | Yes      | Text to synthesize (max 300 chars)                               |
| language        | string   | Yes      | Language code (`ko`, `en`, `ja`)                                 |
| style           | string   | No       | Emotion/style (e.g., `neutral`, `happy`, `sad`, `angry`)         |
| model           | string   | No       | Voice model (default: `sona_speech_1`)                           |
| voice_settings  | object   | No       | Advanced voice settings (see below)                              |

##### Example
```json
{
  "text": "안녕하세요, 수퍼톤 API입니다.",
  "language": "ko",
  "style": "neutral",
  "model": "sona_speech_1",
  "voice_settings": {
    "pitch_shift": 0,
    "pitch_variance": 1,
    "speed": 1
  }
}
```

#### Voice Settings Object
| Field         | Type   | Description                                         | Range      | Default |
|--------------|--------|-----------------------------------------------------|------------|---------|
| pitch_shift  | int    | Pitch shift in semitones                            | -12 ~ +12  | 0       |
| pitch_variance| float | Degree of pitch variation (expressiveness)          | 0.1 ~ 2    | 1       |
| speed        | float  | Speech speed multiplier                             | > 0        | 1       |

#### Response
- **Success:** Returns audio stream (`audio/wav` or `audio/mpeg`).
- **Header:** `X-Audio-Length: [float]` (audio duration in seconds)

#### Example (Success)
- **Status:** 200 OK
- **Headers:**
  - `Content-Type: audio/wav`
  - `X-Audio-Length: 3.42`
- **Body:** Binary audio data

---

### 2. Predict Duration

- **Endpoint:** `POST /v1/predict-duration/{voice_id}`
- **Content-Type:** `application/json`
- **Authentication:** Required

#### Request Body
| Field    | Type   | Required | Description                |
|----------|--------|----------|----------------------------|
| text     | string | Yes      | Text to estimate duration  |
| language | string | Yes      | Language code              |

#### Response
| Field    | Type   | Description                |
|----------|--------|----------------------------|
| duration | float  | Predicted duration (sec)   |

##### Example
```json
{
  "duration": 2.87
}
```

---

## Error Handling

### HTTP Status Codes
| Code | Meaning                  | Typical Causes & Solutions                                                                 |
|------|--------------------------|------------------------------------------------------------------------------------------|
| 400  | Bad Request              | Missing/invalid fields, text > 300 chars, invalid style/model, out-of-range settings      |
| 401  | Unauthorized             | Missing or invalid API key                                                               |
| 402  | Payment Required         | Insufficient credits or plan not purchased                                               |
| 403  | Forbidden                | Accessing unauthorized resources                                                         |
| 404  | Not Found                | Invalid `voice_id` or endpoint                                                           |
| 408  | Request Timeout          | Network issues, overly large/complex requests                                            |
| 429  | Too Many Requests        | Rate limit exceeded                                                                      |
| 500  | Internal Server Error    | Temporary server error, try again later                                                  |

#### Error Response Example
```json
{
  "error": "Missing required field: text"
}
```

---

## Best Practices
- Always validate input fields (text length, language, style, etc.) before sending requests.
- Handle all error codes gracefully and provide user feedback.
- Use the `X-Audio-Length` header to manage audio playback timing.
- Securely store and manage your API key.
- Monitor rate limits and handle 429 errors with exponential backoff.

---

## References
- [Supertone API Documentation](https://docs.supertoneapi.com/en/user-guide/text-to-speech)
- For further support, contact the API provider. 
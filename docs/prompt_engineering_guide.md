# OpenAI 프롬프트 엔지니어링 가이드 (Chat API 기준)

이 문서는 OpenAI 공식 가이드([원문 링크](https://platform.openai.com/docs/guides/text?api-mode=chat))를 바탕으로, 텍스트 생성 및 프롬프트 엔지니어링의 핵심 원칙과 실전 팁을 한국어로 정리한 자료입니다. 실무 개발자와 프롬프트 설계자가 바로 참고할 수 있도록 예시와 함께 꼼꼼하게 정리하였습니다.

---

## 1. 텍스트 생성 기본

OpenAI API를 사용하면 대형 언어 모델(LLM)로부터 프롬프트에 기반한 텍스트를 생성할 수 있습니다. 생성 가능한 텍스트는 코드, 수식, 구조화된 JSON, 자연어 등 거의 모든 형태가 가능합니다.

### 예시: 간단한 프롬프트로 텍스트 생성
```python
from openai import OpenAI
client = OpenAI()

completion = client.chat.completions.create(
    model="gpt-4.1",
    messages=[
        {"role": "user", "content": "Write a one-sentence bedtime story about a unicorn."}
    ]
)

print(completion.choices[0].message.content)
```
ㄴㄴ
응답 예시:
```
Under the soft glow of the moon, Luna the unicorn danced through fields of twinkling stardust, leaving trails of dreams for every child asleep.
```

- 응답은 `choices` 배열에 담겨 반환됩니다.
- 여러 개의 응답이 필요한 경우, `n` 파라미터로 개수 지정 가능.

---

## 2. 모델 선택

- **Reasoning 모델**: 내부적으로 chain-of-thought(추론 사슬)을 생성하여 복잡한 문제, 멀티스텝 플래닝에 강함. 다만 속도와 비용이 더 높음.
- **GPT 계열 모델**: 빠르고 비용 효율적이며, 명확한 지시사항이 있을 때 성능이 극대화됨.
- **대형/소형(미니/나노) 모델**: 대형은 범용성, 이해력, 문제 해결력 우수. 소형은 속도와 비용이 강점.
- **권장**: 특별한 이유가 없다면 `gpt-4.1`이 지능, 속도, 비용의 균형이 좋음.

---

## 3. 프롬프트 엔지니어링이란?

프롬프트 엔지니어링은 모델이 일관되게 원하는 결과를 내도록 효과적인 지시문(프롬프트)을 작성하는 과정입니다. 생성 결과는 비결정적이므로, 아트와 사이언스가 결합된 영역입니다. 다음과 같은 기법과 베스트 프랙티스를 적용하면 좋은 결과를 얻을 수 있습니다.

- 모델별로 최적의 프롬프트 방식이 다를 수 있음
- 프로덕션에서는 **특정 모델 스냅샷**(예: `gpt-4.1-2025-04-14`)에 고정해 일관성 확보
- 프롬프트 성능을 측정하는 **Evals**(평가 지표) 구축 권장

---

## 4. 메시지 역할과 지시사항 구조

OpenAI Chat API는 메시지에 역할(role)을 부여해 다양한 수준의 지시를 할 수 있습니다.

- **developer**: 시스템/비즈니스 로직, 규칙 등(최우선)
- **user**: 실제 사용자 입력(그 다음 우선순위)
- **assistant**: 모델이 생성한 응답

### 예시: 역할별 메시지
```python
messages=[
    {"role": "developer", "content": "Talk like a pirate."},
    {"role": "user", "content": "Are semicolons optional in JavaScript?"}
]
```
- developer 메시지는 함수 정의, user 메시지는 함수 인자에 비유 가능

---

## 5. 프롬프트 재사용

- OpenAI 대시보드에서 재사용 가능한 프롬프트를 관리할 수 있음(Responses API 한정)
- 코드 수정 없이 프롬프트 버전 관리 및 배포 가능

---

## 6. 메시지 포맷팅: Markdown & XML

- **Markdown**: 프롬프트 내 논리적 구역, 계층 구조, 가독성 향상
- **XML**: 문서, 예시, 메타데이터 등 구획 구분 및 속성 부여

### 권장 섹션 구조
1. **Identity**: 어시스턴트의 목적, 스타일, 목표
2. **Instructions**: 규칙, 해야 할 일/하지 말아야 할 일, 함수 호출 등
3. **Examples**: 입력/출력 예시
4. **Context**: 추가 정보(사내 데이터, 문서 등)

### 예시
```
# Identity
You are coding assistant that helps enforce the use of snake case variables in JavaScript code, and writing code that will run in Internet Explorer version 6.

# Instructions
* When defining variables, use snake case names (e.g. my_variable) instead of camel case names (e.g. myVariable).
* To support old browsers, declare variables using the older "var" keyword.
* Do not give responses with Markdown formatting, just return the code as requested.

# Examples
<user_query>
How do I declare a string variable for a first name?
</user_query>
<assistant_response>
var first_name = "Anna";
</assistant_response>
```

---

## 7. 프롬프트 캐싱

- 자주 사용하는 프롬프트/컨텍스트는 프롬프트 앞부분에 배치
- Chat Completions/Responses API의 JSON 요청 바디 내에서도 앞부분에 위치
- 캐싱을 통해 비용 및 지연(latency) 절감 가능

---

## 8. Few-shot Learning (예시 기반 학습)

- 입력/출력 예시(샷)를 프롬프트에 포함시켜 새로운 작업에 대한 패턴을 모델이 "암묵적으로 학습"하도록 유도
- 다양한 입력/출력 예시를 제공할수록 일반화 성능이 향상됨

### 예시
```
# Identity
You are a helpful assistant that labels short product reviews as Positive, Negative, or Neutral.

# Instructions
* Only output a single word in your response with no additional formatting or commentary.
* Your response should only be one of the words "Positive", "Negative", or "Neutral" depending on the sentiment of the product review you are given.

# Examples
<product_review id="example-1">
I absolutely love this headphones — sound quality is amazing!
</product_review>
<assistant_response id="example-1">
Positive
</assistant_response>
...
```

---

## 9. 컨텍스트 정보 활용

- 모델이 답변을 생성할 때 참고할 수 있도록, 사내 데이터/문서 등 추가 컨텍스트를 프롬프트에 포함
- RAG(Retrieval-Augmented Generation): 벡터DB 등에서 검색한 정보를 프롬프트에 삽입
- 컨텍스트는 프롬프트의 마지막 부분에 위치시키는 것이 일반적

---

## 10. 컨텍스트 윈도우(토큰 한계) 관리

- 각 모델마다 한 번에 처리할 수 있는 토큰(문자/단어) 한계가 있음
- 최신 GPT-4.1은 최대 100만 토큰까지 지원
- 프롬프트+컨텍스트+예시+대화 이력 등 전체 합산 토큰 수를 고려해야 함

---

## 11. GPT-4.1 프롬프트 베스트 프랙티스

- **명확하고 구체적인 지시사항**을 제공할수록 좋은 결과
- 논리/데이터/출력 포맷을 명시적으로 요구
- 체인 오브 쏘트(Chain of Thought) 유도: 복잡한 문제는 단계별로 생각하도록 지시
- 긴 컨텍스트, 에이전트 워크플로우 등 고급 기능 활용 가능

---

## 12. Reasoning 모델 vs GPT 모델 프롬프트 차이

- **Reasoning 모델**: 목표만 제시해도 스스로 세부 전략을 세움(시니어 동료 느낌)
- **GPT 모델**: 구체적이고 명확한 지시가 필요(주니어 동료 느낌)

---

## 13. 실전 팁 & 참고 자료

- 프로덕션에서는 모델 버전을 고정해 일관성 확보
- 프롬프트 성능을 자동 평가하는 eval 시스템 구축 권장
- 다양한 예시, 컨텍스트, 역할 메시지 조합 실험
- [OpenAI Cookbook - Prompt Engineering](https://cookbook.openai.com/prompts) 참고

--- 
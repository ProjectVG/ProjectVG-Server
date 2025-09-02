using ProjectVG.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ProjectVG.Domain.Entities.Characters
{
    /// <summary>
    /// AI 캐릭터 - Hybrid 구조 (고정 컬럼 + JSON 설정)
    /// </summary>
    public class Character : BaseEntity
    {
        /// <summary> 캐릭터 고유 ID </summary>
        public Guid Id { get; set; }
        
        /// <summary> 캐릭터 이름 </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary> 캐릭터 설명 </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary> 캐릭터 이미지 URL </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary> 활성화 여부 </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary> 캐릭터 보이스 ID </summary>
        public string VoiceId { get; set; } = string.Empty;
        
        /// <summary> 캐릭터를 생성한 사용자 ID (nullable - 시스템 캐릭터 허용) </summary>
        public Guid? UserId { get; set; }
        
        /// <summary> 캐릭터를 생성한 사용자 (네비게이션 속성) </summary>
        public virtual Users.User? User { get; set; }
        
        /// <summary> 설정 모드 (개별 설정 vs SystemPrompt) </summary>
        public CharacterConfigMode ConfigMode { get; set; } = CharacterConfigMode.Individual;
        
        /// <summary> 개별 설정 JSON 데이터 (DB 저장용) </summary>
        public string? IndividualConfigJson { get; set; }
        
        /// <summary> SystemPrompt 직접 입력 (최대 5000자) </summary>
        public string? SystemPrompt { get; set; }
        
        /// <summary>
        /// 개별 설정 객체 (JSON 래퍼)
        /// DB에는 저장되지 않고 IndividualConfigJson과 연동
        /// </summary>
        [NotMapped]
        public IndividualConfig? IndividualConfig 
        {
            get => string.IsNullOrEmpty(IndividualConfigJson) 
                ? null 
                : JsonSerializer.Deserialize<IndividualConfig>(IndividualConfigJson);
            set => IndividualConfigJson = value == null 
                ? null 
                : JsonSerializer.Serialize(value);
        }
        
        /// <summary>
        /// 현재 설정 모드에 따라 실제 사용할 SystemPrompt 반환
        /// </summary>
        /// <returns>효과적인 SystemPrompt</returns>
        public string GetEffectiveSystemPrompt()
        {
            return ConfigMode switch
            {
                CharacterConfigMode.SystemPrompt => SystemPrompt ?? string.Empty,
                CharacterConfigMode.Individual => IndividualConfig?.BuildSystemPrompt() ?? string.Empty,
                _ => string.Empty
            };
        }
        
        /// <summary>
        /// 개별 설정 모드로 변경
        /// </summary>
        /// <param name="config">개별 설정 객체</param>
        public void SetIndividualConfig(IndividualConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            if (!config.IsValid())
                throw new ArgumentException("Individual config is not valid", nameof(config));
            
            ConfigMode = CharacterConfigMode.Individual;
            IndividualConfig = config;
            SystemPrompt = null;
        }
        
        /// <summary>
        /// SystemPrompt 모드로 변경
        /// </summary>
        /// <param name="prompt">SystemPrompt 내용</param>
        public void SetSystemPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("SystemPrompt cannot be empty", nameof(prompt));
                
            if (prompt.Length > 5000)
                throw new ArgumentException("SystemPrompt cannot exceed 5000 characters", nameof(prompt));
            
            ConfigMode = CharacterConfigMode.SystemPrompt;
            SystemPrompt = prompt;
            IndividualConfigJson = null;
        }
        
        /// <summary>
        /// 현재 캐릭터 설정이 유효한지 검증
        /// </summary>
        /// <returns>유효하면 true</returns>
        public bool ValidateConfiguration()
        {
            return ConfigMode switch
            {
                CharacterConfigMode.Individual => IndividualConfig?.IsValid() ?? false,
                CharacterConfigMode.SystemPrompt => !string.IsNullOrWhiteSpace(SystemPrompt) && SystemPrompt.Length <= 5000,
                _ => false
            };
        }
        
        /// <summary>
        /// 캐릭터가 대화를 시작할 수 있는 상태인지 확인
        /// </summary>
        /// <returns>대화 가능하면 true</returns>
        public bool CanStartConversation()
        {
            return IsActive && ValidateConfiguration() && !string.IsNullOrEmpty(GetEffectiveSystemPrompt());
        }
    }
} 
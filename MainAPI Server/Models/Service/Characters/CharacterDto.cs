using System;

namespace MainAPI_Server.Models.Service.Characters
{
    public class CharacterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
} 
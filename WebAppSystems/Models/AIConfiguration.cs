using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class AIConfiguration
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string Provider { get; set; } = "GoogleGemini"; // GoogleGemini, OpenAI, Anthropic

        [StringLength(500)]
        public string ApiKey { get; set; } = string.Empty;

        [StringLength(100)]
        public string Model { get; set; } = "gemini-1.5-flash"; // gemini-1.5-flash, gemini-1.5-pro, gpt-4, claude-3-sonnet, etc

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace WebAppSystems.Models
{
    public class AIUsageLimit
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AttorneyId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public int UsageCount { get; set; } = 0;
        
        [Required]
        public int DailyLimit { get; set; } = 10;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Navigation property
        public virtual Attorney Attorney { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppSystems.Models
{
    [Table("DocumentAnalysis")]
    public class DocumentAnalysis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } // PDF, DOCX, TXT

        [Required]
        public long FileSize { get; set; }

        [Required]
        public string FilePath { get; set; }

        [Required]
        public DateTime UploadDate { get; set; }

        [Required]
        public int UploadedByAttorneyId { get; set; }

        [ForeignKey("UploadedByAttorneyId")]
        public Attorney? UploadedBy { get; set; }

        // Análise da IA
        public string? Summary { get; set; } // Resumo executivo

        [StringLength(100)]
        public string? LegalArea { get; set; } // Trabalhista, Cível, Tributário, etc

        [StringLength(100)]
        public string? ActionType { get; set; } // Tipo de ação

        [StringLength(50)]
        public string? Complexity { get; set; } // Simples, Média, Alta

        public int? EstimatedHours { get; set; } // Horas estimadas

        public string? MainTopics { get; set; } // JSON com tópicos principais

        public string? LegalBasis { get; set; } // JSON com fundamentos legais

        public string? Parties { get; set; } // JSON com partes envolvidas

        public decimal? CauseValue { get; set; } // Valor da causa

        public string? Deadlines { get; set; } // JSON com prazos

        // Recomendações de advogados
        public string? RecommendedAttorneys { get; set; } // JSON com top 3 advogados

        // Status
        [StringLength(50)]
        public string? AnalysisStatus { get; set; } // Pending, Completed, Error

        public DateTime? AnalysisDate { get; set; }

        public string? ErrorMessage { get; set; }

        // Atribuição
        public int? AssignedToAttorneyId { get; set; }

        [ForeignKey("AssignedToAttorneyId")]
        public Attorney? AssignedTo { get; set; }

        public DateTime? AssignedDate { get; set; }

        // Relacionamento com cliente (opcional)
        public int? ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace WebAppSystems.Models.ViewModels
{
    public class DocumentAnalysisViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedByName { get; set; }

        // Análise
        public string Summary { get; set; }
        public string LegalArea { get; set; }
        public string ActionType { get; set; }
        public string Complexity { get; set; }
        public int? EstimatedHours { get; set; }
        public List<string> MainTopics { get; set; }
        public List<string> LegalBasis { get; set; }
        public PartyInfo Parties { get; set; }
        public decimal? CauseValue { get; set; }
        public List<DeadlineInfo> Deadlines { get; set; }

        // Recomendações
        public List<AttorneyRecommendation> RecommendedAttorneys { get; set; }

        // Status
        public string AnalysisStatus { get; set; }
        public DateTime? AnalysisDate { get; set; }
        public string ErrorMessage { get; set; }

        // Atribuição
        public int? AssignedToAttorneyId { get; set; }
        public string AssignedToName { get; set; }
        public DateTime? AssignedDate { get; set; }

        public int? ClientId { get; set; }
        public string ClientName { get; set; }
    }

    public class PartyInfo
    {
        public string Plaintiff { get; set; }
        public string Defendant { get; set; }
        public List<string> Others { get; set; }
    }

    public class DeadlineInfo
    {
        public string Description { get; set; }
        public DateTime? Date { get; set; }
        public int? Days { get; set; }
    }

    public class AttorneyRecommendation
    {
        public int AttorneyId { get; set; }
        public string Name { get; set; }
        public int Score { get; set; } // 0-100
        public string Justification { get; set; }
        public int SimilarCasesCount { get; set; }
        public double AverageHours { get; set; }
        public double SuccessRate { get; set; }
        public int AvailableHours { get; set; }
        public string Specialization { get; set; }
    }

    public class DocumentUploadViewModel
    {
        public IFormFile File { get; set; }
        public string AnalysisType { get; set; } // Summary, DataExtraction, Full
        public int? ClientId { get; set; }
    }
}

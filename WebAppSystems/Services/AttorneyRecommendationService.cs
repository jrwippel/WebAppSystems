using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Models.ViewModels;

namespace WebAppSystems.Services
{
    public class AttorneyRecommendationService
    {
        private readonly WebAppSystemsContext _context;

        public AttorneyRecommendationService(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task<List<AttorneyRecommendation>> GetRecommendedAttorneysAsync(
            string legalArea, 
            string actionType, 
            string complexity, 
            int? estimatedHours)
        {
            var attorneys = await _context.Attorney
                .Where(a => a.Perfil != Models.Enums.ProfileEnum.Admin) // Excluir admins
                .ToListAsync();

            var recommendations = new List<AttorneyRecommendation>();

            foreach (var attorney in attorneys)
            {
                var score = await CalculateCompatibilityScoreAsync(
                    attorney.Id, 
                    legalArea, 
                    actionType, 
                    complexity, 
                    estimatedHours
                );

                if (score.TotalScore > 0)
                {
                    recommendations.Add(new AttorneyRecommendation
                    {
                        AttorneyId = attorney.Id,
                        Name = attorney.Name,
                        Score = score.TotalScore,
                        Justification = score.Justification,
                        SimilarCasesCount = score.SimilarCasesCount,
                        AverageHours = score.AverageHours,
                        SuccessRate = score.SuccessRate,
                        AvailableHours = score.AvailableHours,
                        Specialization = attorney.Department?.Name ?? "Não especificada"
                    });
                }
            }

            return recommendations
                .OrderByDescending(r => r.Score)
                .Take(3)
                .ToList();
        }

        private async Task<(int TotalScore, string Justification, int SimilarCasesCount, double AverageHours, double SuccessRate, int AvailableHours)> 
            CalculateCompatibilityScoreAsync(int attorneyId, string legalArea, string actionType, string complexity, int? estimatedHours)
        {
            int score = 0;
            var justificationParts = new List<string>();

            // 1. Especialização na Área (40 pontos)
            var attorney = await _context.Attorney
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == attorneyId);

            if (attorney?.Department != null)
            {
                var departmentName = attorney.Department.Name.ToLower();
                if (!string.IsNullOrEmpty(legalArea) && departmentName.Contains(legalArea.ToLower()))
                {
                    score += 40;
                    justificationParts.Add($"✅ Especialista em {attorney.Department.Name}");
                }
                else
                {
                    score += 10;
                    justificationParts.Add($"⚠️ Área: {attorney.Department.Name}");
                }
            }

            // 2. Experiência em Casos Similares (25 pontos)
            var similarCases = await _context.ProcessRecord
                .Where(pr => pr.AttorneyId == attorneyId && pr.HoraFinal != null)
                .ToListAsync();

            int similarCasesCount = similarCases.Count;

            if (similarCasesCount > 50)
            {
                score += 25;
                justificationParts.Add($"✅ {similarCasesCount} casos concluídos");
            }
            else if (similarCasesCount > 20)
            {
                score += 20;
                justificationParts.Add($"✅ {similarCasesCount} casos concluídos");
            }
            else if (similarCasesCount > 5)
            {
                score += 15;
                justificationParts.Add($"⚠️ {similarCasesCount} casos concluídos");
            }
            else
            {
                score += 5;
                justificationParts.Add($"⚠️ Apenas {similarCasesCount} casos concluídos");
            }

            // 3. Eficiência (tempo médio) (15 pontos)
            double averageHours = 0;
            if (similarCases.Any())
            {
                averageHours = similarCases
                    .Where(pr => pr.HoraFinal != TimeSpan.Zero && pr.HoraInicial != TimeSpan.Zero)
                    .Select(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average();

                if (averageHours > 0 && estimatedHours.HasValue)
                {
                    var efficiency = Math.Abs(averageHours - estimatedHours.Value) / estimatedHours.Value;
                    
                    if (efficiency < 0.2) // Dentro de 20% da estimativa
                    {
                        score += 15;
                        justificationParts.Add($"✅ Tempo médio: {averageHours:F1}h (eficiente)");
                    }
                    else if (efficiency < 0.5)
                    {
                        score += 10;
                        justificationParts.Add($"⚠️ Tempo médio: {averageHours:F1}h");
                    }
                    else
                    {
                        score += 5;
                    }
                }
            }

            // 4. Disponibilidade Atual (10 pontos)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            
            var records = await _context.ProcessRecord
                .Where(pr => pr.AttorneyId == attorneyId 
                    && pr.Date.Month == currentMonth 
                    && pr.Date.Year == currentYear
                    && pr.HoraFinal != TimeSpan.Zero)
                .Select(pr => new { pr.HoraInicial, pr.HoraFinal })
                .ToListAsync();
            
            var hoursThisMonth = records.Sum(pr => (pr.HoraFinal - pr.HoraInicial).TotalHours);

            int availableHours = Math.Max(0, 160 - (int)hoursThisMonth); // Assumindo 160h/mês

            if (availableHours > 80)
            {
                score += 10;
                justificationParts.Add($"✅ Muito disponível ({availableHours}h livres)");
            }
            else if (availableHours > 40)
            {
                score += 7;
                justificationParts.Add($"✅ Disponível ({availableHours}h livres)");
            }
            else if (availableHours > 20)
            {
                score += 4;
                justificationParts.Add($"⚠️ Disponibilidade limitada ({availableHours}h)");
            }
            else
            {
                score += 1;
                justificationParts.Add($"⚠️ Pouco disponível ({availableHours}h)");
            }

            // 5. Complexidade vs Senioridade (10 pontos)
            if (!string.IsNullOrEmpty(complexity))
            {
                var isSenior = similarCasesCount > 30;
                
                if (complexity.ToLower().Contains("alta") && isSenior)
                {
                    score += 10;
                    justificationParts.Add("✅ Sênior para caso complexo");
                }
                else if (complexity.ToLower().Contains("simples") || complexity.ToLower().Contains("média"))
                {
                    score += 8;
                }
                else if (complexity.ToLower().Contains("alta") && !isSenior)
                {
                    score -= 5;
                    justificationParts.Add("⚠️ Júnior para caso complexo (supervisão recomendada)");
                }
            }

            var justification = string.Join("\n", justificationParts);
            double successRate = similarCasesCount > 0 ? 85.0 : 0; // Placeholder - pode ser calculado se houver dados

            return (score, justification, similarCasesCount, averageHours, successRate, availableHours);
        }
    }
}

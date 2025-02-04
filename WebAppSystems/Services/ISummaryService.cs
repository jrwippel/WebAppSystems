namespace WebAppSystems.Services
{
    public interface ISummaryService
    {
        Task<string> GenerateSummaryAsync(string text);
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class LicenseService : ILicenseService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _licenseUrl = "https://raw.githubusercontent.com/jrwippel/licenses/master/license.json"; // URL do arquivo no GitHub
    private readonly string _localToken;

    public LicenseService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _localToken = _configuration["TokenSettings:LocalToken"]; // Obtendo o token do appsettings.json
    }

    public async Task<bool> IsLicenseValid()
    {
        var licenseInfo = await GetLicenseInfo();
        return licenseInfo != null && licenseInfo.ExpiryDate > DateTime.UtcNow;
    }

    private async Task<LicenseInfo> GetLicenseInfo()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true
        };
        var urlWithTimestamp = $"{_licenseUrl}?t={DateTime.UtcNow.Ticks}";
        var response = await client.GetStringAsync(urlWithTimestamp);
        var licenses = JsonConvert.DeserializeObject<List<LicenseInfo>>(response);
        return licenses.FirstOrDefault(license => license.Token == _localToken);
    }


    private class LicenseInfo
    {
        public DateTime ExpiryDate { get; set; }
        public string Token { get; set; }
    }

    public async Task<DateTime> GetExpiryDate()
    {
        var licenseInfo = await GetLicenseInfo();
        if (licenseInfo == null)
        {
            throw new Exception("Licenca não encontrada");
        }
        return licenseInfo.ExpiryDate;
    }

    public async Task<bool> IsLicenseNearExpiry(int daysBeforeExpiry = 30)
    {
        DateTime expiryDate = await GetExpiryDate();
        return (expiryDate - DateTime.UtcNow).TotalDays <= daysBeforeExpiry;
    }
}


using System;
using System.Threading.Tasks;

public interface ILicenseService
{
    Task<bool> IsLicenseValid();
    Task<DateTime> GetExpiryDate();
    Task<bool> IsLicenseNearExpiry(int daysBeforeExpiry = 30);
}

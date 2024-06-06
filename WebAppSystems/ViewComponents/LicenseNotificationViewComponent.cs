/*using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAppSystems.Services;

public class LicenseNotificationViewComponent : ViewComponent
{
    private readonly ILicenseService _licenseService;

    public LicenseNotificationViewComponent(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var isNearExpiry = await _licenseService.IsLicenseNearExpiry();
        return View(isNearExpiry);
    }
}

*/
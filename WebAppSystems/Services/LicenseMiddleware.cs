namespace WebAppSystems.Services
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILicenseService _licenseService;       

        public LicenseMiddleware(RequestDelegate next, ILicenseService licenseService)
        {
            _next = next;
            _licenseService = licenseService;          
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var licenseService = context.RequestServices.GetRequiredService<ILicenseService>();

            if (!await licenseService.IsLicenseValid())
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Sua Licenca Expirou ou e invalida. Por favor, entre em contato com o administrador do sistema.");
                return;
            }

            await _next(context);
        }
    }

}

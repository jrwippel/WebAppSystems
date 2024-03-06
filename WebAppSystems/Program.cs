using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using WebAppSystems.Data;
using WebAppSystems.Helper;
using WebAppSystems.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;

namespace WebAppSystems
{
    public class Program
    {
        public static void Main(string[] args)
        {           

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<WebAppSystemsContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("WebAppSystemsContext") ?? throw new InvalidOperationException("Connection string 'WebAppSystemsContext' not found.")));

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Add services to the container.
            builder.Services.AddScoped<SeedingService>();
            builder.Services.AddScoped<BasicAuthenticationFilterAttribute, BasicAuthenticationFilterAttribute>();
            
            builder.Services.AddScoped<AttorneyService>();
            builder.Services.AddScoped<DepartmentService>();
            builder.Services.AddScoped<ProcessRecordService>();
            builder.Services.AddScoped<ProcessRecordsService>();
            builder.Services.AddScoped<ClientService>();
            builder.Services.AddScoped<ISessao, Sessao>();
            builder.Services.AddScoped<IEmail, Email>();
            builder.Services.AddScoped<MensalistaService>();
            builder.Services.AddScoped<ValorClienteService>();


            builder.Services.AddSession(o =>
                {
                    o.Cookie.HttpOnly = true;
                    o.Cookie.IsEssential = true;
                });

            builder.Services.AddControllersWithViews();
            builder.Services.AddControllers();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var app = builder.Build();


            var ptBR = new CultureInfo("pt-BR");
            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(ptBR),
                SupportedCultures = new List<CultureInfo> { ptBR },
                SupportedUICultures = new List<CultureInfo> { ptBR }                
        };

            app.UseRequestLocalization(localizationOptions);


            // Create a new scope to retrieve scoped services
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    // Get the DbContext instance
                    var myDbContext = services.GetRequiredService<WebAppSystemsContext>();

                    // Apply pending migrations
                    myDbContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            // Injeta a dependência do IWebHostEnvironment
            var env = app.Services.GetRequiredService<IWebHostEnvironment>();

            //if (env.IsDevelopment())
            //{
            app.Services.CreateScope().ServiceProvider.GetRequiredService<SeedingService>().Seed();
            //}

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllers(); // Adicione suporte ao roteamento da API


            app.MapControllerRoute(
            name: "about",
            pattern: "about",
            defaults: new { controller = "Home", action = "About" });

            //app.UseMiddleware<BasicAuthenticationMiddleware>();

            app.UseAuthorization();
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Index}/{id?}");

            app.Run();
        }
    }

}
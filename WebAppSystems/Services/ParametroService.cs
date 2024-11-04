using Microsoft.EntityFrameworkCore;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Models.Dto;

namespace WebAppSystems.Services
{
    public interface IParametroService
    {
        Task<(byte[] ImageData, string MimeType, int Width, int Height)> GetLogoAsync();
    }

    public class ParametroService : IParametroService
    {
        private readonly WebAppSystemsContext _context;

        public ParametroService(WebAppSystemsContext context)
        {
            _context = context;
        }

        public async Task<(byte[] ImageData, string MimeType, int Width, int Height)> GetLogoAsync()
        {
            var parametros = await _context.Parametros.FirstOrDefaultAsync();
            if (parametros == null || parametros.LogoData == null)
            {
                throw new Exception("Configuração de logo não encontrada.");
            }

            return (parametros.LogoData, parametros.LogoMimeType, parametros.Width, parametros.Height);
        }
    }
}

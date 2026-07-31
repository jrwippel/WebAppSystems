using WebAppSystems.Services;

namespace WebAppSystems.Models
{
    public class Mensalista
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public int ClientId { get; set; }
        public decimal ValorMensalBruto { get; set; }
        public decimal ComissaoParceiro { get; set; }
        public decimal ComissaoSocio { get; set; }
        public decimal? ValorHoraVirtual { get; set; }

        // Relação com a tabela associativa
        //public ICollection<MensalistaDepartment> MensalistaDepartments { get; set; } = new List<MensalistaDepartment>();

        public Mensalista()
        {
        }

        public Mensalista(int id, Client client, int clientId, decimal valorMensalBruto, decimal comissaoParceiro, decimal comissaoSocio)
        {
            Id = id;
            Client = client;
            ClientId = clientId;
            ValorMensalBruto = valorMensalBruto;
            ComissaoParceiro = comissaoParceiro;
            ComissaoSocio = comissaoSocio;
        }

        public static implicit operator Mensalista(MensalistaService v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retorna o valor-hora efetivo: usa o valor manual se definido, senão calcula pela faixa da mensalidade.
        /// </summary>
        public decimal GetValorHoraEfetivo()
        {
            if (ValorHoraVirtual.HasValue && ValorHoraVirtual.Value > 0)
                return ValorHoraVirtual.Value;

            // Cálculo automático por faixa
            if (ValorMensalBruto <= 3500m)
                return 350m;
            else if (ValorMensalBruto <= 12000m)
                return 325m;
            else
                return 300m;
        }
    }
}


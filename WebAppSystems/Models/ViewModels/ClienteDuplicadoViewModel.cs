namespace WebAppSystems.Models.ViewModels
{
    // Resumo sintético de um cliente (exibido ao selecionar a origem/destino)
    public class ResumoClienteViewModel
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Documento { get; set; }
        public bool Inativo { get; set; }
        public int TotalLancamentos { get; set; }
        public int LancamentosBloqueados { get; set; }
        public string TotalHoras { get; set; } = "00:00";
        public int Valores { get; set; }
        public int PercentuaisArea { get; set; }
        public int Mensalistas { get; set; }
        public int Lotes { get; set; }
        public int Documentos { get; set; }
        public int Solicitantes { get; set; }
    }

    // Prévia da movimentação origem -> destino (o que será movido)
    public class PreviaUnificacaoViewModel
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public string ClienteCorretoNome { get; set; } = string.Empty;   // destino
        public string ClienteDuplicadoNome { get; set; } = string.Empty; // origem
        public bool Bloqueado { get; set; }
        public int Lancamentos { get; set; }
        public int Valores { get; set; }
        public int PercentuaisArea { get; set; }
        public int Mensalistas { get; set; }
        public int Lotes { get; set; }
        public int Documentos { get; set; }
        public int Solicitantes { get; set; }
    }
}

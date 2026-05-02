namespace Mecanica.Hermes.Application.OrdensDeServico.Constants;

/// <summary>
///     Chaves usadas em métricas de OpenTelemetry para operações sobre o agregado OrdemDeServico.
///     Centralizadas para evitar drift entre Consumers e dashboards.
/// </summary>
public static class OrdemDeServicoMetricKeys
{
    public static class Operations
    {
        public const string Aprovar = "aprovar";
        public const string Rejeitar = "rejeitar";
        public const string PagamentoAprovado = "pagamento_aprovado";
        public const string AvancarEtapa = "avancar_etapa";
        public const string ReverterEtapa = "reverter_etapa";
        public const string Criar = "criar";
        public const string DispatchEvent = "dispatch_event";
    }

    public static class ErrorTypes
    {
        public const string NotFound = "nao_encontrada";
        public const string InvalidStatus = "status_invalido";
        public const string DomainError = "domain_error";
        public const string ValidationError = "validacao";
        public const string ConcurrencyConflict = "concorrencia";
    }
}

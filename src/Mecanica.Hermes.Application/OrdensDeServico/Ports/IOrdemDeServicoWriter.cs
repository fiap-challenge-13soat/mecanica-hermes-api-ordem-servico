using Mecanica.Hermes.Domain.OrdensDeServico;

namespace Mecanica.Hermes.Application.OrdensDeServico.Ports;

/// <summary>
///     Port (Hexagonal) de escrita para o agregado <see cref="OrdemDeServico" />.
///     Aplicação do princípio ISP: separa o contrato de mutação do contrato de consulta.
/// </summary>
public interface IOrdemDeServicoWriter
{
    Task AddAsync(OrdemDeServico ordemDeServico);
    Task UpdateAsync(OrdemDeServico ordemDeServico);
}

namespace Mecanica.Hermes.Application.OrdensDeServico.Ports;

/// <summary>
/// Composite port que une <see cref="IOrdemDeServicoReader"/> e <see cref="IOrdemDeServicoWriter"/>.
/// Mantida para compatibilidade com Consumers que precisam tanto ler quanto escrever no mesmo
/// caso de uso. Para novas dependências, prefira o port mais granular (ISP).
/// </summary>
public interface IOrdemDeServicoRepository : IOrdemDeServicoReader, IOrdemDeServicoWriter
{
}

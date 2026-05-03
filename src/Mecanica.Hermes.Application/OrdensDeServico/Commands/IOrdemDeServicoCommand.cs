namespace Mecanica.Hermes.Application.OrdensDeServico.Commands;

/// <summary>
///     Marker interface para commands que operam sobre uma <c>OrdemDeServico</c> específica.
///     Permite ao handler genérico aplicar uma única validação de <see cref="OrdemDeServicoId" />
///     não-vazio sem duplicação em cada caso de uso.
/// </summary>
public interface IOrdemDeServicoCommand
{
    Guid OrdemDeServicoId { get; }
}

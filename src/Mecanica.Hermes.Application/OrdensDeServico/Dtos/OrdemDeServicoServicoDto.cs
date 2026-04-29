namespace Mecanica.Hermes.Application.OrdensDeServico.Dtos;

public sealed record OrdemDeServicoServicoDto(
    Guid Id,
    Guid ServicoId,
    decimal Valor);

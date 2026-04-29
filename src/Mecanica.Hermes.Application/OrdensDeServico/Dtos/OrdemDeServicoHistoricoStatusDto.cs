using Mecanica.Hermes.Domain.OrdensDeServico.Enums;

namespace Mecanica.Hermes.Application.OrdensDeServico.Dtos;

public sealed record OrdemDeServicoHistoricoStatusDto(
    OrdemDeServicoStatus StatusAnterior,
    OrdemDeServicoStatus StatusAtual,
    OrdemDeServicoStatus StatusDestino,
    DateTime DataInicio,
    DateTime? DataFinalizacao,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("ordem-de-servico.aguardando-aprovacao.v1")]
public sealed record OrdemDeServicoAguardandoAprovacaoEvent(Guid OrdemDeServicoId);

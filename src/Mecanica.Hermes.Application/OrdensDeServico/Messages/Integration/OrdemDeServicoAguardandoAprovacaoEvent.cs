using MassTransit;

namespace Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;

[EntityName("ordem-de-servico.aguardando-aprovacao")]
public sealed record OrdemDeServicoAguardandoAprovacaoEvent(Guid OrdemDeServicoId);

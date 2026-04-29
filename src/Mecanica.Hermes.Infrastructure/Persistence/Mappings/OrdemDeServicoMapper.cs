using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Abstractions;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.Domain.OrdensDeServico.Status;
using Mecanica.Hermes.Infrastructure.Persistence.Entities;

namespace Mecanica.Hermes.Infrastructure.Persistence.Mappings;

internal static class OrdemDeServicoMapper
{
    internal static OrdemDeServico ToDomain(this OrdemDeServicoEntity entity)
    {
        var produtos = entity.Produtos.Select(p => OrdemDeServicoProduto.Restaurar(
            p.Id,
            p.ProdutoId,
            p.Valor,
            p.Quantidade,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        var servicos = entity.Servicos.Select(s => OrdemDeServicoServico.Restaurar(
            s.Id,
            s.ServicoId,
            s.Valor,
            s.CreatedAt,
            s.UpdatedAt)).ToList();

        var statusAtual = entity.StatusAtual.ToDomain();

        var historicosStatus = entity.HistoricosStatus
            .Select(h => h.ToDomain())
            .ToList();

        return OrdemDeServico.Restaurar(
            entity.Id,
            entity.ClienteId,
            entity.VeiculoId,
            entity.ProblemaRelatado,
            entity.Observacoes,
            statusAtual,
            historicosStatus,
            produtos,
            servicos,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    internal static OrdemDeServicoEntity ToEntity(this OrdemDeServico ordemDeServico)
    {
        var statusAtualEntity = ordemDeServico.StatusAtual.ToEntity();

        var historicosStatus = ordemDeServico.HistoricosStatus.Select(h => new OrdemDeServicoHistoricoStatusEntity(
            h.Id,
            ordemDeServico.Id,
            h.StatusAnterior,
            h.StatusAtual,
            h.StatusDestino,
            h.DataInicio,
            h.DataFinalizacao,
            h.CreatedAt,
            h.UpdatedAt)).ToList();

        var produtos = ordemDeServico.Produtos.Select(p => new OrdemDeServicoProdutoEntity(
            p.Id,
            ordemDeServico.Id,
            p.ProdutoId,
            p.ValorVenda,
            p.Quantidade,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        var servicos = ordemDeServico.Servicos.Select(s => new OrdemDeServicoServicoEntity(
            s.Id,
            ordemDeServico.Id,
            s.ServicoId,
            s.ValorVenda,
            s.CreatedAt,
            s.UpdatedAt)).ToList();

        return new OrdemDeServicoEntity(
            ordemDeServico.Id,
            ordemDeServico.ClienteId,
            ordemDeServico.VeiculoId,
            ordemDeServico.ProblemaRelatado,
            ordemDeServico.Observacoes,
            statusAtualEntity.Id,
            statusAtualEntity,
            historicosStatus,
            produtos,
            servicos,
            ordemDeServico.CreatedAt,
            ordemDeServico.UpdatedAt);
    }

    private static OrdemDeServicoStatusBase ToDomain(this OrdemDeServicoStatusEntity entity)
    {
        var status = entity.StatusAtual.ToStatusBase();
        status.Restaurar(
            entity.Id,
            entity.StatusDestino,
            entity.DataInicio,
            entity.DataFinalizacao,
            entity.CreatedAt,
            entity.UpdatedAt);
        return status;
    }

    private static OrdemDeServicoHistoricoStatus ToDomain(this OrdemDeServicoHistoricoStatusEntity entity)
    {
        return OrdemDeServicoHistoricoStatus.Restaurar(
            entity.Id,
            entity.StatusAnterior,
            entity.StatusAtual,
            entity.StatusDestino,
            entity.DataInicio,
            entity.DataFinalizacao,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static OrdemDeServicoStatusEntity ToEntity(this OrdemDeServicoStatusBase status)
    {
        return new OrdemDeServicoStatusEntity(
            status.Id,
            status.StatusAtual,
            status.StatusDestino,
            status.DataInicio,
            status.DataFinalizacao,
            status.CreatedAt,
            status.UpdatedAt);
    }

    private static OrdemDeServicoStatusBase ToStatusBase(this OrdemDeServicoStatus status)
    {
        return status switch
        {
            OrdemDeServicoStatus.Recebida => new OrdemDeServicoRecebida(),
            OrdemDeServicoStatus.EmDiagnostico => new OrdemDeServicoEmDiagnostico(),
            OrdemDeServicoStatus.AguardandoAprovacao => new OrdemDeServicoAguardandoAprovacao(),
            OrdemDeServicoStatus.Rejeitada => new OrdemDeServicoRejeitada(),
            OrdemDeServicoStatus.EmExecucao => new OrdemDeServicoEmExecucao(),
            OrdemDeServicoStatus.ManutencaoFinalizada => new OrdemDeServicoManutencaoFinalizada(),
            OrdemDeServicoStatus.AguardandoPagamento => new OrdemDeServicoAguardandoPagamento(),
            OrdemDeServicoStatus.PagamentoConfirmado => new OrdemDeServicoPagamentoConfirmado(),
            OrdemDeServicoStatus.Entregue => new OrdemDeServicoEntregue(),
            OrdemDeServicoStatus.Cancelada => new OrdemDeServicoCancelada(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}

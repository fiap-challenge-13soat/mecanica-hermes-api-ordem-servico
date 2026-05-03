using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico;

public static class OrdensDeServicoEndpoints
{
    public static void MapOrdensDeServicoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ordens-de-servico")
            .WithTags("Ordens de Servico");

        group.MapCreateOrdemDeServico();
        group.MapGetOrdemDeServicoById();
        group.MapListAllOrdensDeServico();
        group.MapListOrdemDeServicoByStatus();
        group.MapAddProduto();
        group.MapAddServico();
        group.MapRemoveProduto();
        group.MapRemoveServico();
        group.MapAvancarEtapa();
        group.MapReverterEtapa();
        group.MapCancelarOrdemDeServico();
        group.MapAprovarOrdemDeServico();
        group.MapRejeitarOrdemDeServico();
        group.MapPagamentoAprovado();
    }
}

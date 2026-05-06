# Fluxo da Ordem de Serviço

A `OrdemDeServico` é o agregado raiz do domínio. Seu ciclo de vida é modelado com
**State Pattern**: cada estado é uma classe (`OrdemDeServicoStatusBase`) que conhece os
seus próprios alvos de transição e suas regras de edição.

## Máquina de estados

```mermaid
stateDiagram-v2
    [*] --> Recebida
    Recebida --> EmDiagnostico : Avancar
    EmDiagnostico --> AguardandoAprovacao : Avancar
    AguardandoAprovacao --> EmExecucao : Aprovar (Avancar)\nou OrcamentoAprovadoPeloClienteEvent ⬅ Cadastros
    AguardandoAprovacao --> Rejeitada : Rejeitar\nou OrcamentoRejeitadoPeloClienteEvent ⬅ Cadastros
    EmExecucao --> ManutencaoFinalizada : Avancar
    ManutencaoFinalizada --> AguardandoPagamento : Avancar
    AguardandoPagamento --> PagamentoConfirmado : Avancar\nou PagamentoConfirmadoEvent ⬅ Pagamentos
    AguardandoPagamento --> ManutencaoFinalizada : ReverterEtapa\nou PagamentoRecusadoEvent ⬅ Pagamentos
    PagamentoConfirmado --> Entregue : Avancar
    Entregue --> [*]

    Recebida --> Cancelada : Cancelar
    EmDiagnostico --> Cancelada : Cancelar
    EmExecucao --> Cancelada : Cancelar
    ManutencaoFinalizada --> Cancelada : Cancelar
    AguardandoPagamento --> Cancelada : Cancelar
    PagamentoConfirmado --> Cancelada : Cancelar
```

> **Reverter etapa** segue o caminho inverso (`StatusRevertido` em cada classe de status).
> Transições marcadas com `⬅` são iniciadas por **eventos externos cross-service**
> (via consumers de integração) — veja a seção [Gatilhos externos](#gatilhos-externos) abaixo.

## Estados

| Estado | Próximo (`Avancar`) | Reversível? | Edita produtos/serviços? |
| --- | --- | --- | --- |
| `Recebida` | `EmDiagnostico` | Não | **Sim** |
| `EmDiagnostico` | `AguardandoAprovacao` | → `Recebida` | **Sim** |
| `AguardandoAprovacao` | `EmExecucao` | → `EmDiagnostico` | Não |
| `EmExecucao` | `ManutencaoFinalizada` | → `AguardandoAprovacao` | Não |
| `ManutencaoFinalizada` | `AguardandoPagamento` | → `EmExecucao` | Não |
| `AguardandoPagamento` | `PagamentoConfirmado` | → `ManutencaoFinalizada` | Não |
| `PagamentoConfirmado` | `Entregue` | → `AguardandoPagamento` | Não |
| `Entregue` | — *(terminal)* | → `PagamentoConfirmado` | Não |
| `Rejeitada` | — *(terminal)* | → `AguardandoAprovacao` | Não |
| `Cancelada` | — *(terminal)* | — | Não |

## Operações

| Operação | Onde se aplica | Efeito |
| --- | --- | --- |
| `AvancarEtapa` | qualquer estado não-terminal | Transiciona para `ProximoStatus`. |
| `ReverterEtapa` | qualquer estado com predecessor | Transiciona para `StatusRevertido`. |
| `Cancelar` | qualquer estado não-terminal | Transiciona para `Cancelada`. |
| `Aprovar` | apenas em `AguardandoAprovacao` | Equivalente a `AvancarEtapa` com guard de status. |
| `Rejeitar` | apenas em `AguardandoAprovacao` | Equivalente a `Cancelar` (transiciona para `Rejeitada`). |
| `PagamentoAprovado` | apenas em `AguardandoPagamento` | Equivalente a `AvancarEtapa` com guard de status (avança para `PagamentoConfirmado`). |
| `AdicionarProduto` / `RemoverProduto` | estados com `PermiteEditarProdutos = true` | Modifica a lista de produtos. |
| `AdicionarServico` / `RemoverServico` | estados com `PermiteEditarProdutos = true` | Modifica a lista de serviços. |

## Histórico

Toda transição registra um `OrdemDeServicoHistoricoStatus` com `DataInicio`/`DataFinalizacao`
do estado *que está sendo arquivado*. O histórico é parte do agregado e é serializado nas
respostas de query.

## Eventos de domínio

| Evento | Quando é disparado |
| --- | --- |
| `OrdemDeServicoCriadoEvent` | Em `OrdemDeServico.Criar(...)` (estado inicial `Recebida`). |
| `OrdemDeServicoEtapaAvancadaEvent` | Em `AvancarEtapa()` bem-sucedido. |
| `OrdemDeServicoEtapaRevertidaEvent` | Em `ReverterEtapa()` bem-sucedido. |
| `OrdemDeServicoCanceladaEvent` | Em `Cancelar()` bem-sucedido (incluindo `Rejeitar`). |

Os eventos são gravados no Outbox dentro da mesma transação do `SaveChangesAsync` e
despachados pelo `OutboxProcessor`. Veja [messaging.md](./messaging.md#outbox).

## Gatilhos externos

Quatro transições de estado podem ser iniciadas por **eventos MassTransit publicados por outras APIs** (em vez de por chamadas HTTP diretas ao OS). Os consumers cross-service vivem em `Application/OrdensDeServico/Consumers/Integration/` e simplesmente traduzem o evento externo para um Request interno, que a SAGA processa normalmente.

| Evento externo | Origem | Consumer | Transição |
| --- | --- | --- | --- |
| `orcamento.aprovado-pelo-cliente.v1` | API de Cadastros | `OrcamentoAprovadoPeloClienteConsumer` | `AguardandoAprovacao` → `EmExecucao` |
| `orcamento.rejeitado-pelo-cliente.v1` | API de Cadastros | `OrcamentoRejeitadoPeloClienteConsumer` | `AguardandoAprovacao` → `Rejeitada` |
| `pagamento.confirmado.v1` | API de Pagamentos | `PagamentoConfirmadoConsumer` | `AguardandoPagamento` → `PagamentoConfirmado` |
| `pagamento.recusado.v1` | API de Pagamentos | `PagamentoRecusadoConsumer` | `AguardandoPagamento` → `ManutencaoFinalizada` (reverte) |

O padrão de tradução é sempre o mesmo: o consumer publica o `*Request` correspondente no bus, e a SAGA trata como se fosse um trigger interno.

```csharp
// Exemplo: PagamentoRecusadoConsumer
public Task Consume(ConsumeContext<PagamentoRecusadoEvent> context)
    => context.Publish(new ReverterEtapaRequest(context.Message.OrdemDeServicoId));
```

Para o contexto completo da integração cross-service, consulte [`messaging.md#integration-events-cross-service`](./messaging.md#integration-events-cross-service).

## Snapshots de produtos e serviços

`OrdemDeServicoProduto` e `OrdemDeServicoServico` são **entidades filhas que armazenam
preço e descrição no momento da criação** — `ProdutoId`/`ServicoId` são referências lógicas
sem FK no banco para preservar histórico mesmo após o catálogo mudar.

`ValorTotal` é calculado a partir desses snapshots:

```text
ValorTotal = Σ(produto.ValorVenda × produto.Quantidade)
           + Σ(serviço.ValorVenda)
```

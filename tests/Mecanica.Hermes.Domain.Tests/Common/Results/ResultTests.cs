using System.Net;
using Mecanica.Hermes.Shared.Core.Results;

namespace Mecanica.Hermes.Domain.Tests.Common.Results;

[Trait("Category", "Unit")]
public class ResultTests
{
    // ---- Result (não-genérico) ----

    [Fact]
    public void Ok_DeveCriarResultadoSucessoSemErros()
    {
        var result = Result.Ok();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Accepted_DeveCriarResultadoSucessoCom202()
    {
        var result = Result.Accepted();

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public void BadRequest_ComMensagemUnica_DeveFalhar()
    {
        var result = Result.BadRequest("Erro");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Erro");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void BadRequest_ComMultiplasMensagens_DeveFalhar()
    {
        var result = Result.BadRequest(["E1", "E2"]);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo("E1", "E2");
    }

    [Fact]
    public void NotFound_DeveFalhar()
    {
        var result = Result.NotFound();

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Result<T> (genérico) ----

    [Fact]
    public void GenericOk_ComDados_DeveRetornarSucessoComData()
    {
        var result = Result<string>.Ok("payload");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("payload");
    }

    [Fact]
    public void GenericOk_SemDados_DeveRetornarSucessoComDataNull()
    {
        var result = Result<string>.Ok();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    [Fact]
    public void GenericCreated_DeveRetornar201ComData()
    {
        var result = Result<string>.Created("x");

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data.Should().Be("x");
    }

    [Fact]
    public void GenericConflict_DeveFalharCom409()
    {
        var result = Result<string>.Conflict("dup");

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
        result.Errors.Should().ContainSingle().Which.Should().Be("dup");
    }

    [Fact]
    public void GenericInternalServerError_DeveFalharCom500()
    {
        var result = Result<string>.InternalServerError("boom");

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(nameof(Result<string>.Forbidden), HttpStatusCode.Forbidden)]
    [InlineData(nameof(Result<string>.Unauthorized), HttpStatusCode.Unauthorized)]
    [InlineData(nameof(Result<string>.NoContent), HttpStatusCode.NoContent)]
    [InlineData(nameof(Result<string>.NotFound), HttpStatusCode.NotFound)]
    public void GenericFactories_DeveMapearCorretamente(string factory, HttpStatusCode expected)
    {
        var result = factory switch
        {
            nameof(Result<string>.Forbidden) => Result<string>.Forbidden(),
            nameof(Result<string>.Unauthorized) => Result<string>.Unauthorized(),
            nameof(Result<string>.NoContent) => Result<string>.NoContent(),
            nameof(Result<string>.NotFound) => Result<string>.NotFound(),
            _ => throw new InvalidOperationException()
        };

        result.StatusCode.Should().Be(expected);
    }
}

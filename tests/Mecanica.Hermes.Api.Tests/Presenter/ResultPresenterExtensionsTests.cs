using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Shared.AspNetCore.Presenter;
using Mecanica.Hermes.Shared.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Mecanica.Hermes.Api.Tests.Presenter;

/// <summary>
///     Cobre as 3 sobrecargas de <c>Present()</c> e todos os ramos de mapeamento
///     <see cref="Result" /> → <see cref="IResult" /> HTTP.
/// </summary>
[Trait("Category", "Unit")]
public class ResultPresenterExtensionsTests
{
    // --- Result (não-genérico) ---

    [Fact]
    public void Present_Ok_DeveRetornarNoContent()
    {
        Result.Ok().Present().Should().BeOfType<NoContent>();
    }

    [Fact]
    public void Present_Accepted_DeveRetornarAccepted()
    {
        Result.Accepted().Present().Should().BeOfType<Accepted>();
    }

    [Fact]
    public void Present_NotFound_DeveRetornarNotFound()
    {
        Result.NotFound().Present().Should().BeOfType<NotFound>();
    }

    [Fact]
    public void Present_BadRequest_DeveRetornarBadRequestComProblemDetails()
    {
        var result = Result.BadRequest("erro");

        var present = result.Present();

        present.Should().BeOfType<BadRequest<ApiProblemDetails>>()
            .Which.Value!.Errors.Should().ContainSingle().Which.Should().Be("erro");
    }

    [Fact]
    public void Present_Conflict_DeveRetornarConflictComProblemDetails()
    {
        var result = new Result(System.Net.HttpStatusCode.Conflict);
        result.Errors.Add("duplicado");

        var present = result.Present();

        present.Should().BeOfType<Conflict<ApiProblemDetails>>()
            .Which.Value!.Errors.Should().Contain("duplicado");
    }

    [Fact]
    public void Present_Unauthorized_DeveRetornarUnauthorized()
    {
        new Result(System.Net.HttpStatusCode.Unauthorized).Present()
            .Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public void Present_NoContentFailure_DeveRetornarNoContent()
    {
        new Result(System.Net.HttpStatusCode.NoContent).Present().Should().BeOfType<NoContent>();
    }

    [Fact]
    public void Present_InternalServerError_DeveRetornarProblem()
    {
        var result = new Result(System.Net.HttpStatusCode.InternalServerError);
        result.Errors.Add("boom");

        result.Present().Should().BeOfType<ProblemHttpResult>();
    }

    // --- Result<T> ---

    [Fact]
    public void PresentGeneric_Ok_SemMapper_DeveRetornarNoContent()
    {
        var result = Result<string>.Ok("x");

        result.Present().Should().BeOfType<NoContent>();
    }

    [Fact]
    public void PresentGeneric_NotFound_SemMapper_DeveRetornarNotFound()
    {
        var result = Result<string>.NotFound();

        result.Present().Should().BeOfType<NotFound>();
    }

    [Fact]
    public void PresentGeneric_Conflict_SemMapper_DeveRetornarConflictComProblemDetails()
    {
        var result = Result<string>.Conflict("duplicado");

        var present = result.Present();

        present.Should().BeOfType<Conflict<ApiProblemDetails>>()
            .Which.Value!.Errors.Should().Contain("duplicado");
    }

    [Fact]
    public void PresentGeneric_InternalServerError_DeveRetornarProblem()
    {
        var result = Result<string>.InternalServerError("boom");

        var present = result.Present();

        present.Should().BeOfType<ProblemHttpResult>();
    }

    [Fact]
    public void PresentGeneric_Unauthorized_SemMapper_DeveRetornarUnauthorized()
    {
        Result<string>.Unauthorized().Present().Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public void PresentGeneric_BadRequest_SemMapper_DeveRetornarBadRequest()
    {
        Result<string>.BadRequest("erro").Present()
            .Should().BeOfType<BadRequest<ApiProblemDetails>>()
            .Which.Value!.Errors.Should().Contain("erro");
    }

    [Fact]
    public void PresentGeneric_NoContentFailure_SemMapper_DeveRetornarNoContent()
    {
        Result<string>.NoContent().Present().Should().BeOfType<NoContent>();
    }

    // --- Result<T> com mapper: ramos de falha não devem invocar a função de map ---

    [Fact]
    public void PresentGenericComMapper_Failure_NaoDeveInvocarMapper()
    {
        var mapInvoked = false;
        Func<SrcDto, ApiDto> map = src =>
        {
            mapInvoked = true;
            return new ApiDto(src.Data);
        };

        Result<SrcDto>.NotFound().Present(map);

        mapInvoked.Should().BeFalse("falhas não devem invocar o mapper");
    }

    [Fact]
    public void PresentGenericComMapper_NotFound_DeveRetornarNotFound()
    {
        Result<SrcDto>.NotFound().Present(_ => new ApiDto("x")).Should().BeOfType<NotFound>();
    }

    [Fact]
    public void PresentGenericComMapper_Conflict_DeveRetornarConflict()
    {
        Result<SrcDto>.Conflict("dup").Present(_ => new ApiDto("x"))
            .Should().BeOfType<Conflict<ApiProblemDetails>>();
    }

    [Fact]
    public void PresentGenericComMapper_Unauthorized_DeveRetornarUnauthorized()
    {
        Result<SrcDto>.Unauthorized().Present(_ => new ApiDto("x"))
            .Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public void PresentGenericComMapper_BadRequest_DeveRetornarBadRequest()
    {
        Result<SrcDto>.BadRequest("erro").Present(_ => new ApiDto("x"))
            .Should().BeOfType<BadRequest<ApiProblemDetails>>();
    }

    [Fact]
    public void PresentGenericComMapper_NoContentFailure_DeveRetornarNoContent()
    {
        Result<SrcDto>.NoContent().Present(_ => new ApiDto("x"))
            .Should().BeOfType<NoContent>();
    }

    [Fact]
    public void PresentGenericComMapper_InternalServerError_DeveRetornarProblem()
    {
        Result<SrcDto>.InternalServerError("boom").Present(_ => new ApiDto("x"))
            .Should().BeOfType<ProblemHttpResult>();
    }

    [Fact]
    public void PresentGenericComConversaoManual_Ok_DeveRetornarOkComApiModel()
    {
        var result = Result<SrcDto>.Ok(new SrcDto("x"));

        var present = result.Present(src => new ApiDto(src.Data + "-mapped"));

        present.Should().BeOfType<Ok<ApiDto>>()
            .Which.Value!.Data.Should().Be("x-mapped");
    }

    [Fact]
    public void PresentGenericComConversaoManual_Ok_DeveChamarSuccessCallbackQuandoFornecido()
    {
        var result = Result<SrcDto>.Ok(new SrcDto("x"));
        var callbackCalled = false;

        result.Present(src => new ApiDto(src.Data), _ =>
        {
            callbackCalled = true;
            return Results.Created("/res/1", _);
        });

        callbackCalled.Should().BeTrue();
    }

    // --- Result<T> com conversão manual ---

    private sealed record SrcDto(string Data);

    private sealed record ApiDto(string Data);
}

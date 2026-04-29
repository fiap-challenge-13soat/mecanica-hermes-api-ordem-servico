using Mecanica.Hermes.Api.Presenter;
using Mecanica.Hermes.Domain.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Mecanica.Hermes.Api.Tests.Presenter;

/// <summary>
/// Cobre as 3 sobrecargas de <c>Present()</c> e todos os ramos de mapeamento
/// <see cref="Result"/> → <see cref="IResult"/> HTTP.
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

    // --- Result<T> com conversão manual ---

    private sealed record SrcDto(string Data);
    private sealed record ApiDto(string Data);

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

        result.Present(src => new ApiDto(src.Data), success: _ =>
        {
            callbackCalled = true;
            return Results.Created("/res/1", _);
        });

        callbackCalled.Should().BeTrue();
    }
}

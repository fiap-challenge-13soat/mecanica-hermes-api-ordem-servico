using Microsoft.AspNetCore.Mvc;

namespace Mecanica.Hermes.Api.Presenter;

public sealed class ApiProblemDetails : ProblemDetails
{
    public List<string> Errors { get; init; } = [];
}
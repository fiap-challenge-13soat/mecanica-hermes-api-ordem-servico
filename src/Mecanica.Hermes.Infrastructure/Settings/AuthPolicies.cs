namespace Mecanica.Hermes.Infrastructure.Settings;

public static class AuthPolicies
{
    public static readonly string AllowClienteScope = Environment.GetEnvironmentVariable(VariableNames.AuthClienteScope) ?? "";
    public static readonly string OnlyAdminScope = Environment.GetEnvironmentVariable(VariableNames.AuthAdminScope) ?? "";
}
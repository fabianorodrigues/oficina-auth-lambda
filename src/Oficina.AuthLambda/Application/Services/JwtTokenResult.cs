namespace Oficina.AuthLambda.Application.Services;

public sealed record JwtTokenResult(string AccessToken, int ExpiresIn);

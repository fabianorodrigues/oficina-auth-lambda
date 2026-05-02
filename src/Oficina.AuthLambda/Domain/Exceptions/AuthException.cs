namespace Oficina.AuthLambda.Domain.Exceptions;

public sealed class AuthException : Exception
{
    public AuthException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public static AuthException BadRequest(string message) => new(message, 400);

    public static AuthException Unauthorized() => new("Invalid credentials.", 401);
}

using CellApp.Services;
using System.Net.Http.Headers;

namespace CellApp.Handlers;

/// <summary>
/// Agrega automáticamente el token JWT a todas las peticiones HTTP al backend.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthStateService _auth;

    public AuthHeaderHandler(AuthStateService auth) => _auth = auth;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_auth.Token))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _auth.Token);

        return base.SendAsync(request, cancellationToken);
    }
}

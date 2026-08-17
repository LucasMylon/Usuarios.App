using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;

public interface IUsuarioService
{
    Task<CriarContaResponse> CriarContaAsync(CriarContaRequest request, CancellationToken cancellationToken = default);

    Task<AutenticarUsuarioResponse> AutenticarUsuarioAsync(AutenticarUsuarioRequest request, CancellationToken cancellationToken = default);

    Task ConfirmarEmailAsync(string token, CancellationToken cancellationToken = default);
}

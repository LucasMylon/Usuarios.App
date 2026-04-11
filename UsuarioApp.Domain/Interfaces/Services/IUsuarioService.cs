using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;

public interface IUsuarioService
{
    Task<CriarContaResponse> CriarConta(CriarContaRequest request);

    AutenticarUsuarioResponse AutenticarUsuario(AutenticarUsuarioRequest request);
}
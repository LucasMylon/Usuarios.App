using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;

public interface IUsuarioService
{
    Task<CriarContaResponse> CriarContaAsync(CriarContaRequest request, CancellationToken cancellationToken = default);

    Task<AutenticarUsuarioResponse> AutenticarUsuarioAsync(AutenticarUsuarioRequest request, CancellationToken cancellationToken = default);

    Task ConfirmarEmailAsync(string token, CancellationToken cancellationToken = default);
    Task<MinhaContaResponse> ObterMinhaContaAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task AlterarSenhaAsync(Guid usuarioId, AlterarSenhaRequest request, CancellationToken cancellationToken = default);
    Task SolicitarRedefinicaoSenhaAsync(SolicitarRedefinicaoSenhaRequest request, CancellationToken cancellationToken = default);
    Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken cancellationToken = default);
    Task SolicitarConfirmacaoTelefoneAsync(Guid usuarioId, SolicitarConfirmacaoTelefoneRequest request, CancellationToken cancellationToken = default);
    Task ConfirmarTelefoneAsync(Guid usuarioId, ConfirmarCodigoTelefoneRequest request, CancellationToken cancellationToken = default);
    Task SolicitarAlteracaoEmailAsync(Guid usuarioId, SolicitarAlteracaoEmailRequest request, CancellationToken cancellationToken = default);
    Task ConfirmarAlteracaoEmailAsync(ConfirmarAlteracaoEmailRequest request, CancellationToken cancellationToken = default);
    Task SolicitarAlteracaoTelefoneAsync(Guid usuarioId, SolicitarAlteracaoTelefoneRequest request, CancellationToken cancellationToken = default);
    Task ConfirmarAlteracaoTelefoneAsync(Guid usuarioId, ConfirmarCodigoTelefoneRequest request, CancellationToken cancellationToken = default);
    Task SolicitarRecuperacaoEmailAsync(SolicitarRecuperacaoEmailRequest request, CancellationToken cancellationToken = default);
    Task ConfirmarRecuperacaoEmailAsync(ConfirmarRecuperacaoEmailRequest request, CancellationToken cancellationToken = default);
}

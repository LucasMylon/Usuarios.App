namespace UsuarioApp.Domain.Events;

public record EmailSolicitadoEvent(
    Guid UsuarioId,
    string Nome,
    string Email,
    TipoEmailSolicitado Tipo,
    string? Token = null);

public enum TipoEmailSolicitado
{
    ConfirmacaoConta = 1,
    RedefinicaoSenha = 2,
    ConfirmacaoNovoEmail = 3,
    AvisoEmailAlterado = 4
}

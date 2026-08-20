namespace UsuarioApp.Domain.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string telefone, string mensagem, CancellationToken cancellationToken = default);
}

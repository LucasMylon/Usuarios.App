using Microsoft.Extensions.Logging;
using UsuarioApp.Domain.Interfaces;

namespace UsuariosApp.Infra.Messages.Sms;

public class DevelopmentSmsSender(ILogger<DevelopmentSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string telefone, string mensagem, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("SMS DE DESENVOLVIMENTO para {Telefone}: {Mensagem}", Mask(telefone), mensagem);
        return Task.CompletedTask;
    }

    private static string Mask(string telefone)
    {
        return telefone.Length <= 4 ? "****" : $"***{telefone[^4..]}";
    }
}

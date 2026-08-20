using UsuarioApp.Domain.Entities;

namespace UsuarioApp.Domain.Interfaces.Repositories;

public interface IUsuarioTokenRepository : IBaseRepository<UsuarioToken>
{
    Task<UsuarioToken?> GetActiveByHashAsync(
        TipoUsuarioToken tipo,
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<UsuarioToken?> GetLatestActiveAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken = default);

    Task<UsuarioToken?> GetLatestAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken = default);

    Task InvalidateActiveAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken = default);
}

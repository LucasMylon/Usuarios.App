using Microsoft.EntityFrameworkCore;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuariosApp.Infra.Data.Contexts;

namespace UsuariosApp.Infra.Data.Repositories;

public class UsuarioTokenRepository : BaseRepository<UsuarioToken>, IUsuarioTokenRepository
{
    private readonly DataContext context;

    public UsuarioTokenRepository(DataContext context) : base(context)
    {
        this.context = context;
    }

    public Task<UsuarioToken?> GetActiveByHashAsync(
        TipoUsuarioToken tipo,
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        return context.Set<UsuarioToken>()
            .Include(t => t.Usuario)
            .ThenInclude(u => u!.Perfil)
            .FirstOrDefaultAsync(t => t.Tipo == tipo
                && t.TokenHash == tokenHash
                && t.ConsumidoEmUtc == null
                && t.ExpiraEmUtc > agora,
                cancellationToken);
    }

    public Task<UsuarioToken?> GetLatestActiveAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        return context.Set<UsuarioToken>()
            .Where(t => t.UsuarioId == usuarioId
                && t.Tipo == tipo
                && t.ConsumidoEmUtc == null
                && t.ExpiraEmUtc > agora)
            .OrderByDescending(t => t.CriadoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UsuarioToken?> GetLatestAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken = default)
    {
        return context.Set<UsuarioToken>()
            .Where(t => t.UsuarioId == usuarioId && t.Tipo == tipo)
            .OrderByDescending(t => t.CriadoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidateActiveAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var tokens = await context.Set<UsuarioToken>()
            .Where(t => t.UsuarioId == usuarioId
                && t.Tipo == tipo
                && t.ConsumidoEmUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
            token.ConsumidoEmUtc = agora;

        await context.SaveChangesAsync(cancellationToken);
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuariosApp.Infra.Data.Contexts;

namespace UsuariosApp.Infra.Data.Repositories
{
    public class PerfilRepository : BaseRepository<Perfil>, IPerfilRepository
    {
        private readonly DataContext context;

        public PerfilRepository(DataContext context) : base(context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<Perfil?> GetAsync(string nome, CancellationToken cancellationToken = default)
        {
            return context.Set<Perfil>()
                .FirstOrDefaultAsync(p => p.Nome == nome, cancellationToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuariosApp.Infra.Data.Contexts;

namespace UsuariosApp.Infra.Data.Repositories
{
    public class UsuarioRepository : BaseRepository<Usuario>, IUsuarioRepository
    {
        private readonly DataContext context;

        public UsuarioRepository(DataContext context) : base(context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<bool> AnyAsync(string email, CancellationToken cancellationToken = default)
        {
           
            
                return context.Set<Usuario>()
                    .AnyAsync(u => u.Email == email, cancellationToken);
            
        }

        public Task<Usuario?> GetAsync(string email, string senha, CancellationToken cancellationToken = default)
        {
            
            
                return context.Set<Usuario>()
                    .Include(u => u.Perfil)
                    .FirstOrDefaultAsync(
                        u => u.Email == email && u.Senha == senha,
                        cancellationToken);
            
        }

        public Task<Usuario?> GetByEmailConfirmacaoTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            return context.Set<Usuario>()
                .FirstOrDefaultAsync(
                    u => u.EmailConfirmacaoToken == token,
                    cancellationToken);
        }
    }
}

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

        public bool Any(string email)
        {
           
            
                return context.Set<Usuario>()
                    .Where(u => u.Email.Equals(email))
                    .Any();
            
        }

        public Usuario? Get(string email, string senha)
        {
            
            
                return context.Set<Usuario>()
                    .Include(u => u.Perfil)
                    .Where(u => u.Email.Equals(email) && u.Senha.Equals(senha))
                    .FirstOrDefault();
            
        }
    }
}

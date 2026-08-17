using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;

namespace UsuarioApp.Domain.Interfaces.Repositories
{
    public interface IUsuarioRepository : IBaseRepository<Entities.Usuario>
    {
        Task<bool> AnyAsync(string email, CancellationToken cancellationToken = default);

        Task<Usuario?> GetAsync(string email, string senha, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByEmailConfirmacaoTokenAsync(string token, CancellationToken cancellationToken = default);
    }
}

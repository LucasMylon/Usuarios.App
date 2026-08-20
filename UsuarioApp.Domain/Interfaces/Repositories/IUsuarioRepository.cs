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

        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Usuario?> GetWithProfileByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByConfirmedPhoneAsync(string telefone, CancellationToken cancellationToken = default);

        Task<bool> AnyPhoneAsync(string telefone, Guid? exceptUsuarioId = null, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByEmailConfirmacaoTokenAsync(string token, CancellationToken cancellationToken = default);
    }
}

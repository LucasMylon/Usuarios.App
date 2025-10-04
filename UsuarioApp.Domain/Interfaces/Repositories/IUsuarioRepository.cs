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
        bool Any(string email);

        Usuario? Get(string email, string senha);
    }
}

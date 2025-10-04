using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;

namespace UsuarioApp.Domain.Interfaces.Repositories
{
    public interface IPerfilRepository : IBaseRepository<Perfil>
    {
        Perfil? Get(string nome);
    }
}

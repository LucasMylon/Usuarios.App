using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Dtos.Requests
{
    public record AutenticarUsuarioRequest

        (
            string Email,
            string Senha
        );
}

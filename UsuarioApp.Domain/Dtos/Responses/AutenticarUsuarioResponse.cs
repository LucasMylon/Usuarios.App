using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Dtos.Responses
{
    public record AutenticarUsuarioResponse
    (
            Guid Id,
            string Nome,
            string Email,
            string Perfil,
            DateTime DataHoraAcesso, 
            string AccessToken
    );

}

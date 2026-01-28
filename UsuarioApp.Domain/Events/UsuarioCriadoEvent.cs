using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Events
{
    public record UsuarioCriadoEvent
    (
        Guid UsuarioId,
        string Nome,
        string Email,
        string Token

    );
}

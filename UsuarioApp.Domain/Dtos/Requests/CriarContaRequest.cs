using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Dtos.Requests
{
    /// <summary>
    /// dto para requisição de criação de conta
    /// </summary>
    public record CriarContaRequest 
        (
            string Nome,
            string Email,
            string Senha
        )
    {
        
    }
}

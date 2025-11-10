using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Dtos.Responses
{
    /// <summary>
    /// dto para resposta de criação de conta
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Nome"></param>
    /// <param name="Email"></param>
    /// <param name="Perfil"></param>
    /// <param name="DataCriacao"></param>
    public class CriarContaResponse 
        (
            Guid Id,
            string Nome,
            string Email,
            string Perfil,
            DateTime DataCriacao

        )
    {
    }
}

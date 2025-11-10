using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;

namespace UsuarioApp.Domain.Interfaces.Services
{
    public interface IUsuarioService
    {
        CriarContaResponse CriarConta(CriarContaRequest request);

        AutenticarUsuarioResponse AutenticarUsuario(AutenticarUsuarioRequest request);
    }
}

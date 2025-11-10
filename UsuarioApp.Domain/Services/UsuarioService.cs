using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuarioApp.Domain.Interfaces.Services;
using UsuarioApp.Domain.Validators;
using UsuariosApp.Domain.Helpers;


namespace UsuariosApp.Domain.Services
{
    /// <summary>
    /// Classe de serviço para operações relacionadas a usuários.
    /// </summary>
    public class UsuarioService : IUsuarioService
    {
        //Atributos
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPerfilRepository _perfilRepository;

        //Método construtor para injeção de dependência
        public UsuarioService(IUsuarioRepository usuarioRepository, IPerfilRepository perfilRepository)
        {
            _usuarioRepository = usuarioRepository;
            _perfilRepository = perfilRepository;
        }

        public CriarContaResponse CriarConta(CriarContaRequest request)
        {
            //Criando um usuário (entidade)
            var usuario = new Usuario
            {
                Nome = request.Nome, //capturando o nome do usuário
                Email = request.Email, //capturando o email do usuário
                Senha = request.Senha, //capturando a senha do usuário
            };

            //Validar os dados do usuário
            var validator = new UsuarioValidator(_usuarioRepository);
            var result = validator.Validate(usuario);

            //verificar se ocorreram erros de validação
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors); //mensagens de erro
            }

            //Criptografar a senha do usuário

            usuario.Senha = CryptoHelper.GetSHA256(usuario.Senha);

            //Buscando o perfil com o nome 'USUARIO'
            var perfil = _perfilRepository.Get("USUARIO");

            //Associar o usuário a um perfil padrão (Usuario)
            if (perfil != null)
                usuario.PerfilId = perfil.Id;

            //Salvar o usuário no banco de dados
            _usuarioRepository.Add(usuario);

            //Retornar os dados do usuário criado
            return new CriarContaResponse(
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                perfil != null ? perfil.Nome : string.Empty,
                DateTime.Now
                );
        }

        public AutenticarUsuarioResponse AutenticarUsuario(AutenticarUsuarioRequest request)
        {

            var usuario = _usuarioRepository.Get(request.Email, CryptoHelper.GetSHA256(request.Senha));

            if (usuario == null)
            {
                throw new ApplicationException("Usuário ou senha inválidos.");
            }
            return new AutenticarUsuarioResponse
                (
                    usuario.Id,
                    usuario.Nome,
                    usuario.Email,
                    usuario.Perfil.Nome,
                    DateTime.Now,
                    JwtTokenHelper.GenerateToken(usuario.Email, usuario.Perfil.Nome)

                );
        }
    }
}

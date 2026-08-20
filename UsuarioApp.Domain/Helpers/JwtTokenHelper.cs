using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UsuarioApp.Domain.Settings;


namespace UsuariosApp.Domain.Helpers
{
    public class JwtTokenHelper
    {
        /// <summary>
        /// Método para gerar um TOKEN JWT
        /// </summary>
        public static string GenerateToken(UsuarioApp.Domain.Entities.Usuario usuario, JwtSettings settings)
        {
            //Chave secreta utilizada para assinar o TOKEN
            var key = new SymmetricSecurityKey(Convert.FromBase64String(settings.SecretKey));

            //Criprografar a assinatura do token
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //Informações do usuário do token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Perfil!.Nome),
                new Claim("security_version", usuario.VersaoSeguranca.ToString())
            };

            //Criando o TOKEN JWT
            var token = new JwtSecurityToken(
                    issuer: settings.Issuer, //emissor do token
                    audience: settings.Audience,
                    claims: claims, //informações do usuário do token
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
                    signingCredentials: credentials
                );

            //retornando o TOKEN
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

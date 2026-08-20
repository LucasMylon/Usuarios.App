using Microsoft.AspNetCore.Identity;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Interfaces.Security;

namespace Usuarios.App.API.Services;

public class AspNetPasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hash(Usuario usuario, string senha) => _hasher.HashPassword(usuario, senha);

    public bool Verify(Usuario usuario, string hash, string senha)
    {
        return _hasher.VerifyHashedPassword(usuario, hash, senha) != PasswordVerificationResult.Failed;
    }
}

using UsuarioApp.Domain.Entities;

namespace UsuarioApp.Domain.Interfaces.Security;

public interface IPasswordService
{
    string Hash(Usuario usuario, string senha);
    bool Verify(Usuario usuario, string hash, string senha);
}

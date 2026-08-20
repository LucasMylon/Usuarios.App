namespace UsuarioApp.Domain.Entities;

public class UsuarioToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public TipoUsuarioToken Tipo { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? Destino { get; set; }
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? ConsumidoEmUtc { get; set; }
    public int Tentativas { get; set; }

    public Usuario? Usuario { get; set; }
}

public enum TipoUsuarioToken
{
    RedefinicaoSenha = 1,
    ConfirmacaoTelefone = 2,
    AlteracaoEmail = 3,
    AlteracaoTelefone = 4,
    RecuperacaoEmailPorTelefone = 5
}

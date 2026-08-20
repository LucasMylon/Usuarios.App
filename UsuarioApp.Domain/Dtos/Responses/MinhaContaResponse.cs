namespace UsuarioApp.Domain.Dtos.Responses;

public record MinhaContaResponse(
    Guid Id,
    string Nome,
    string Email,
    string Perfil,
    string? Telefone,
    bool TelefoneConfirmado);

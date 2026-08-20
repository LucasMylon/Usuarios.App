namespace UsuarioApp.Domain.Dtos.Requests;

public record ConfirmarRecuperacaoEmailRequest(string Telefone, string Codigo, string NovoEmail);

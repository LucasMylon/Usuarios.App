using FluentValidation;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;
using UsuarioApp.Domain.Entities;
using UsuarioApp.Domain.Events;
using UsuarioApp.Domain.Interfaces;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuarioApp.Domain.Interfaces.Security;
using UsuarioApp.Domain.Settings;
using UsuarioApp.Domain.Validators;
using UsuariosApp.Domain.Helpers;

namespace UsuariosApp.Domain.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPerfilRepository _perfis;
    private readonly IUsuarioTokenRepository _tokens;
    private readonly IEventPublisher _events;
    private readonly ISmsSender _sms;
    private readonly IPasswordService _passwords;
    private readonly JwtSettings _jwt;
    private readonly RecoverySettings _recovery;

    public UsuarioService(
        IUsuarioRepository usuarios,
        IPerfilRepository perfis,
        IUsuarioTokenRepository tokens,
        IEventPublisher events,
        ISmsSender sms,
        IPasswordService passwords,
        JwtSettings jwt,
        RecoverySettings recovery)
    {
        _usuarios = usuarios;
        _perfis = perfis;
        _tokens = tokens;
        _events = events;
        _sms = sms;
        _passwords = passwords;
        _jwt = jwt;
        _recovery = recovery;
    }

    public async Task<CriarContaResponse> CriarContaAsync(
        CriarContaRequest request,
        CancellationToken cancellationToken = default)
    {
        var rawToken = TokenHelper.GenerateLinkToken();
        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = AccountDataHelper.NormalizeEmail(request.Email),
            Ativo = false,
            EmailConfirmacaoToken = TokenHelper.HashLinkToken(rawToken),
            EmailConfirmacaoExpiraEmUtc = DateTime.UtcNow.AddMinutes(_recovery.LinkExpirationMinutes)
        };

        var validation = await new UsuarioValidator(_usuarios).ValidateAsync(
            new Usuario { Nome = usuario.Nome, Email = usuario.Email, Senha = request.Senha },
            cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var perfil = await _perfis.GetAsync("USUARIO", cancellationToken)
            ?? throw new InvalidOperationException("O perfil padrão USUARIO não está cadastrado.");

        usuario.PerfilId = perfil.Id;
        usuario.Perfil = perfil;
        usuario.Senha = _passwords.Hash(usuario, request.Senha);
        await _usuarios.AddAsync(usuario, cancellationToken);

        await _events.PublishAsync(new EmailSolicitadoEvent(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            TipoEmailSolicitado.ConfirmacaoConta,
            rawToken), cancellationToken);

        return new CriarContaResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            perfil.Nome,
            DateTime.UtcNow);
    }

    public async Task<AutenticarUsuarioResponse> AutenticarUsuarioAsync(
        AutenticarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = AccountDataHelper.NormalizeEmail(request.Email);
        var usuario = await _usuarios.GetByEmailAsync(email, cancellationToken);

        if (usuario is null
            || !usuario.Ativo
            || usuario.Perfil is null
            || !_passwords.Verify(usuario, usuario.Senha, request.Senha))
        {
            throw new ApplicationException("Usuário ou senha inválidos.");
        }

        return new AutenticarUsuarioResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Perfil.Nome,
            DateTime.UtcNow,
            JwtTokenHelper.GenerateToken(usuario, _jwt));
    }

    public async Task ConfirmarEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ApplicationException("Token de confirmação inválido.");

        var usuario = await _usuarios.GetByEmailConfirmacaoTokenAsync(
            TokenHelper.HashLinkToken(token), cancellationToken);

        if (usuario is null
            || usuario.Ativo
            || usuario.EmailConfirmacaoExpiraEmUtc is null
            || usuario.EmailConfirmacaoExpiraEmUtc <= DateTime.UtcNow)
        {
            throw new ApplicationException("Token de confirmação inválido, expirado ou já utilizado.");
        }

        usuario.Ativo = true;
        usuario.EmailConfirmacaoToken = null;
        usuario.EmailConfirmacaoExpiraEmUtc = null;
        await _usuarios.UpdateAsync(usuario, cancellationToken);
    }

    public async Task<MinhaContaResponse> ObterMinhaContaAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        return new MinhaContaResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Perfil?.Nome ?? string.Empty,
            usuario.Telefone,
            usuario.TelefoneConfirmado);
    }

    public async Task AlterarSenhaAsync(
        Guid usuarioId,
        AlterarSenhaRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        if (!_passwords.Verify(usuario, usuario.Senha, request.SenhaAtual))
            throw new ApplicationException("Senha atual inválida.");

        AccountDataHelper.ValidatePassword(request.NovaSenha);
        usuario.Senha = _passwords.Hash(usuario, request.NovaSenha);
        usuario.VersaoSeguranca++;
        await _usuarios.UpdateAsync(usuario, cancellationToken);
    }

    public async Task SolicitarRedefinicaoSenhaAsync(
        SolicitarRedefinicaoSenhaRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.GetByEmailAsync(
            AccountDataHelper.NormalizeEmail(request.Email), cancellationToken);
        if (usuario is null || !usuario.Ativo)
            return;

        string rawToken;
        try
        {
            rawToken = await CreateLinkTokenAsync(
                usuario,
                TipoUsuarioToken.RedefinicaoSenha,
                null,
                cancellationToken);
        }
        catch (ApplicationException)
        {
            return;
        }

        await _events.PublishAsync(new EmailSolicitadoEvent(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            TipoEmailSolicitado.RedefinicaoSenha,
            rawToken), cancellationToken);
    }

    public async Task RedefinirSenhaAsync(
        RedefinirSenhaRequest request,
        CancellationToken cancellationToken = default)
    {
        AccountDataHelper.ValidatePassword(request.NovaSenha);
        var token = await GetLinkTokenAsync(
            TipoUsuarioToken.RedefinicaoSenha,
            request.Token,
            cancellationToken);
        var usuario = token.Usuario!;

        usuario.Senha = _passwords.Hash(usuario, request.NovaSenha);
        usuario.VersaoSeguranca++;
        token.ConsumidoEmUtc = DateTime.UtcNow;
        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _tokens.UpdateAsync(token, cancellationToken);
    }

    public async Task SolicitarConfirmacaoTelefoneAsync(
        Guid usuarioId,
        SolicitarConfirmacaoTelefoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        var telefone = AccountDataHelper.NormalizePhone(request.Telefone);
        if (await _usuarios.AnyPhoneAsync(telefone, usuario.Id, cancellationToken))
            throw new ApplicationException("O telefone informado não está disponível.");

        await SendSmsCodeAsync(usuario, telefone, TipoUsuarioToken.ConfirmacaoTelefone, cancellationToken);
    }

    public async Task ConfirmarTelefoneAsync(
        Guid usuarioId,
        ConfirmarCodigoTelefoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        var token = await VerifySmsCodeAsync(
            usuario,
            TipoUsuarioToken.ConfirmacaoTelefone,
            request.Codigo,
            cancellationToken);

        usuario.Telefone = token.Destino;
        usuario.TelefoneConfirmado = true;
        token.ConsumidoEmUtc = DateTime.UtcNow;
        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _tokens.UpdateAsync(token, cancellationToken);
    }

    public async Task SolicitarAlteracaoEmailAsync(
        Guid usuarioId,
        SolicitarAlteracaoEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        RequirePassword(usuario, request.SenhaAtual);
        var novoEmail = AccountDataHelper.NormalizeEmail(request.NovoEmail);
        AccountDataHelper.ValidateEmail(novoEmail);
        if (await _usuarios.AnyAsync(novoEmail, cancellationToken))
            throw new ApplicationException("O e-mail informado não está disponível.");

        var rawToken = await CreateLinkTokenAsync(
            usuario,
            TipoUsuarioToken.AlteracaoEmail,
            novoEmail,
            cancellationToken);

        await _events.PublishAsync(new EmailSolicitadoEvent(
            usuario.Id,
            usuario.Nome,
            novoEmail,
            TipoEmailSolicitado.ConfirmacaoNovoEmail,
            rawToken), cancellationToken);
    }

    public async Task ConfirmarAlteracaoEmailAsync(
        ConfirmarAlteracaoEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetLinkTokenAsync(
            TipoUsuarioToken.AlteracaoEmail,
            request.Token,
            cancellationToken);
        var usuario = token.Usuario!;
        var emailAnterior = usuario.Email;
        var novoEmail = token.Destino!;

        if (await _usuarios.AnyAsync(novoEmail, cancellationToken))
            throw new ApplicationException("Token inválido ou expirado.");

        usuario.Email = novoEmail;
        usuario.VersaoSeguranca++;
        token.ConsumidoEmUtc = DateTime.UtcNow;
        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _tokens.UpdateAsync(token, cancellationToken);

        await _events.PublishAsync(new EmailSolicitadoEvent(
            usuario.Id,
            usuario.Nome,
            emailAnterior,
            TipoEmailSolicitado.AvisoEmailAlterado), cancellationToken);
    }

    public async Task SolicitarAlteracaoTelefoneAsync(
        Guid usuarioId,
        SolicitarAlteracaoTelefoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        RequirePassword(usuario, request.SenhaAtual);
        var telefone = AccountDataHelper.NormalizePhone(request.NovoTelefone);
        if (await _usuarios.AnyPhoneAsync(telefone, usuario.Id, cancellationToken))
            throw new ApplicationException("O telefone informado não está disponível.");

        await SendSmsCodeAsync(usuario, telefone, TipoUsuarioToken.AlteracaoTelefone, cancellationToken);
    }

    public async Task ConfirmarAlteracaoTelefoneAsync(
        Guid usuarioId,
        ConfirmarCodigoTelefoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await RequireUserAsync(usuarioId, cancellationToken);
        var token = await VerifySmsCodeAsync(
            usuario,
            TipoUsuarioToken.AlteracaoTelefone,
            request.Codigo,
            cancellationToken);

        usuario.Telefone = token.Destino;
        usuario.TelefoneConfirmado = true;
        usuario.VersaoSeguranca++;
        token.ConsumidoEmUtc = DateTime.UtcNow;
        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _tokens.UpdateAsync(token, cancellationToken);
    }

    public async Task SolicitarRecuperacaoEmailAsync(
        SolicitarRecuperacaoEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var telefone = AccountDataHelper.NormalizePhone(request.Telefone);
        var usuario = await _usuarios.GetByConfirmedPhoneAsync(telefone, cancellationToken);
        if (usuario is null || !usuario.Ativo)
            return;

        try
        {
            await SendSmsCodeAsync(
                usuario,
                telefone,
                TipoUsuarioToken.RecuperacaoEmailPorTelefone,
                cancellationToken);
        }
        catch (ApplicationException)
        {
            return;
        }
    }

    public async Task ConfirmarRecuperacaoEmailAsync(
        ConfirmarRecuperacaoEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var telefone = AccountDataHelper.NormalizePhone(request.Telefone);
        var usuario = await _usuarios.GetByConfirmedPhoneAsync(telefone, cancellationToken)
            ?? throw new ApplicationException("Código inválido ou expirado.");
        var novoEmail = AccountDataHelper.NormalizeEmail(request.NovoEmail);
        AccountDataHelper.ValidateEmail(novoEmail);
        if (await _usuarios.AnyAsync(novoEmail, cancellationToken))
            throw new ApplicationException("Código inválido ou expirado.");

        var smsToken = await VerifySmsCodeAsync(
            usuario,
            TipoUsuarioToken.RecuperacaoEmailPorTelefone,
            request.Codigo,
            cancellationToken);
        smsToken.ConsumidoEmUtc = DateTime.UtcNow;
        await _tokens.UpdateAsync(smsToken, cancellationToken);

        var rawToken = await CreateLinkTokenAsync(
            usuario,
            TipoUsuarioToken.AlteracaoEmail,
            novoEmail,
            cancellationToken);
        await _events.PublishAsync(new EmailSolicitadoEvent(
            usuario.Id,
            usuario.Nome,
            novoEmail,
            TipoEmailSolicitado.ConfirmacaoNovoEmail,
            rawToken), cancellationToken);
    }

    private async Task<Usuario> RequireUserAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetWithProfileByIdAsync(usuarioId, cancellationToken);
        if (usuario is null || !usuario.Ativo)
            throw new ApplicationException("Usuário inválido.");
        return usuario;
    }

    private void RequirePassword(Usuario usuario, string senha)
    {
        if (!_passwords.Verify(usuario, usuario.Senha, senha))
            throw new ApplicationException("Senha atual inválida.");
    }

    private async Task<string> CreateLinkTokenAsync(
        Usuario usuario,
        TipoUsuarioToken tipo,
        string? destino,
        CancellationToken cancellationToken)
    {
        await EnsureRequestCooldownAsync(usuario.Id, tipo, cancellationToken);
        await _tokens.InvalidateActiveAsync(usuario.Id, tipo, cancellationToken);
        var rawToken = TokenHelper.GenerateLinkToken();
        await _tokens.AddAsync(new UsuarioToken
        {
            UsuarioId = usuario.Id,
            Tipo = tipo,
            TokenHash = TokenHelper.HashLinkToken(rawToken),
            Destino = destino,
            ExpiraEmUtc = DateTime.UtcNow.AddMinutes(_recovery.LinkExpirationMinutes)
        }, cancellationToken);
        return rawToken;
    }

    private async Task<UsuarioToken> GetLinkTokenAsync(
        TipoUsuarioToken tipo,
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new ApplicationException("Token inválido ou expirado.");
        return await _tokens.GetActiveByHashAsync(
            tipo,
            TokenHelper.HashLinkToken(rawToken),
            cancellationToken)
            ?? throw new ApplicationException("Token inválido ou expirado.");
    }

    private async Task SendSmsCodeAsync(
        Usuario usuario,
        string telefone,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken)
    {
        await EnsureRequestCooldownAsync(usuario.Id, tipo, cancellationToken);
        await _tokens.InvalidateActiveAsync(usuario.Id, tipo, cancellationToken);
        var codigo = TokenHelper.GenerateNumericCode();
        await _tokens.AddAsync(new UsuarioToken
        {
            UsuarioId = usuario.Id,
            Tipo = tipo,
            TokenHash = _passwords.Hash(usuario, codigo),
            Destino = telefone,
            ExpiraEmUtc = DateTime.UtcNow.AddMinutes(_recovery.SmsCodeExpirationMinutes)
        }, cancellationToken);
        await _sms.SendAsync(telefone, $"Seu código Usuarios.App é {codigo}. Ele expira em {_recovery.SmsCodeExpirationMinutes} minutos.", cancellationToken);
    }

    private async Task<UsuarioToken> VerifySmsCodeAsync(
        Usuario usuario,
        TipoUsuarioToken tipo,
        string codigo,
        CancellationToken cancellationToken)
    {
        var token = await _tokens.GetLatestActiveAsync(usuario.Id, tipo, cancellationToken)
            ?? throw new ApplicationException("Código inválido ou expirado.");

        if (token.Tentativas >= _recovery.MaxCodeAttempts
            || !_passwords.Verify(usuario, token.TokenHash, codigo))
        {
            token.Tentativas++;
            if (token.Tentativas >= _recovery.MaxCodeAttempts)
                token.ConsumidoEmUtc = DateTime.UtcNow;
            await _tokens.UpdateAsync(token, cancellationToken);
            throw new ApplicationException("Código inválido ou expirado.");
        }

        return token;
    }

    private async Task EnsureRequestCooldownAsync(
        Guid usuarioId,
        TipoUsuarioToken tipo,
        CancellationToken cancellationToken)
    {
        var latest = await _tokens.GetLatestAsync(usuarioId, tipo, cancellationToken);
        if (latest is not null
            && latest.CriadoEmUtc.AddSeconds(_recovery.RequestCooldownSeconds) > DateTime.UtcNow)
        {
            throw new ApplicationException("Aguarde antes de solicitar um novo código ou token.");
        }
    }
}

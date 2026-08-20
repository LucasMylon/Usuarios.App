using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;
using ValidationException = FluentValidation.ValidationException;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Usuarios.App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("account")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [AllowAnonymous]
        [HttpPost("Criar")]
        [ProducesResponseType(typeof(CriarContaResponse), 200)]
        public async Task<IActionResult> Criar(
            [FromBody] CriarContaRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _usuarioService.CriarContaAsync(request, cancellationToken);
                return CreatedAtAction(nameof(Criar), response);
            }
            catch (ValidationException e)
            {
                return BadRequest(e.Errors.Select(e => new
                { e.PropertyName, e.ErrorMessage }));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return StatusCode(500, new { Mensagem = "Ocorreu um erro inesperado." });
            }
        }

        [AllowAnonymous]
        [HttpPost("autenticar")]
        [ProducesResponseType(typeof(AutenticarUsuarioResponse), 200)]
        public async Task<IActionResult> Autenticar(
            [FromBody] AutenticarUsuarioRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _usuarioService.AutenticarUsuarioAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ApplicationException e)
            {
                // Unauthorized
                return StatusCode(401, e.Message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Internal Server Error
                return StatusCode(500, new { Mensagem = "Ocorreu um erro inesperado." });
            }
        }

        [AllowAnonymous]
        [HttpGet("confirmar-email")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ConfirmarEmail(
            [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            try
            {
                await _usuarioService.ConfirmarEmailAsync(token, cancellationToken);

                return Ok(new { Mensagem = "Email confirmado com sucesso." });
            }
            catch (ApplicationException e)
            {
                return BadRequest(new { Mensagem = e.Message });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return StatusCode(500, new { Mensagem = "Ocorreu um erro inesperado." });
            }
        }
        [HttpGet("Minha-Conta")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> MinhaConta(CancellationToken cancellationToken)
        {
            var response = await _usuarioService.ObterMinhaContaAsync(GetUsuarioId(), cancellationToken);
            return Ok(response);
        }

        [HttpPost("alterar-senha")]
        public async Task<IActionResult> AlterarSenha(
            AlterarSenhaRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.AlterarSenhaAsync(GetUsuarioId(), request, cancellationToken);
            return Ok(new { Mensagem = "Senha alterada com sucesso. Entre novamente." });
        }

        [AllowAnonymous]
        [HttpPost("esqueci-senha")]
        public async Task<IActionResult> EsqueciSenha(
            SolicitarRedefinicaoSenhaRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.SolicitarRedefinicaoSenhaAsync(request, cancellationToken);
            return Accepted(new { Mensagem = "Se a conta estiver disponível, as instruções serão enviadas." });
        }

        [AllowAnonymous]
        [HttpPost("redefinir-senha")]
        public async Task<IActionResult> RedefinirSenha(
            RedefinirSenhaRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.RedefinirSenhaAsync(request, cancellationToken);
            return Ok(new { Mensagem = "Senha redefinida com sucesso." });
        }

        [HttpPost("telefone/solicitar-confirmacao")]
        public async Task<IActionResult> SolicitarConfirmacaoTelefone(
            SolicitarConfirmacaoTelefoneRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.SolicitarConfirmacaoTelefoneAsync(GetUsuarioId(), request, cancellationToken);
            return Accepted(new { Mensagem = "Código enviado para o telefone informado." });
        }

        [HttpPost("telefone/confirmar")]
        public async Task<IActionResult> ConfirmarTelefone(
            ConfirmarCodigoTelefoneRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.ConfirmarTelefoneAsync(GetUsuarioId(), request, cancellationToken);
            return Ok(new { Mensagem = "Telefone confirmado com sucesso." });
        }

        [HttpPost("email/solicitar-alteracao")]
        public async Task<IActionResult> SolicitarAlteracaoEmail(
            SolicitarAlteracaoEmailRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.SolicitarAlteracaoEmailAsync(GetUsuarioId(), request, cancellationToken);
            return Accepted(new { Mensagem = "Confirme o novo endereço de e-mail." });
        }

        [AllowAnonymous]
        [HttpGet("email/confirmar-alteracao")]
        public async Task<IActionResult> ConfirmarAlteracaoEmail(
            [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            await _usuarioService.ConfirmarAlteracaoEmailAsync(
                new ConfirmarAlteracaoEmailRequest(token), cancellationToken);
            return Ok(new { Mensagem = "E-mail alterado com sucesso. Entre novamente." });
        }

        [HttpPost("telefone/solicitar-alteracao")]
        public async Task<IActionResult> SolicitarAlteracaoTelefone(
            SolicitarAlteracaoTelefoneRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.SolicitarAlteracaoTelefoneAsync(GetUsuarioId(), request, cancellationToken);
            return Accepted(new { Mensagem = "Código enviado para o novo telefone." });
        }

        [HttpPost("telefone/confirmar-alteracao")]
        public async Task<IActionResult> ConfirmarAlteracaoTelefone(
            ConfirmarCodigoTelefoneRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.ConfirmarAlteracaoTelefoneAsync(GetUsuarioId(), request, cancellationToken);
            return Ok(new { Mensagem = "Telefone alterado com sucesso. Entre novamente." });
        }

        [AllowAnonymous]
        [HttpPost("email/recuperar-por-telefone")]
        public async Task<IActionResult> SolicitarRecuperacaoEmail(
            SolicitarRecuperacaoEmailRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.SolicitarRecuperacaoEmailAsync(request, cancellationToken);
            return Accepted(new { Mensagem = "Se os dados estiverem disponíveis, um código será enviado." });
        }

        [AllowAnonymous]
        [HttpPost("email/confirmar-recuperacao-por-telefone")]
        public async Task<IActionResult> ConfirmarRecuperacaoEmail(
            ConfirmarRecuperacaoEmailRequest request,
            CancellationToken cancellationToken)
        {
            await _usuarioService.ConfirmarRecuperacaoEmailAsync(request, cancellationToken);
            return Accepted(new { Mensagem = "Confirme o novo endereço de e-mail para concluir." });
        }

        private Guid GetUsuarioId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var usuarioId))
                throw new UnauthorizedAccessException("Token inválido.");
            return usuarioId;
        }
    }
}


using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;
using ValidationException = FluentValidation.ValidationException;

namespace Usuarios.App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

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
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
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
            catch (Exception e)
            {
                // Internal Server Error
                return StatusCode(500, e.Message);
            }
        }

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
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}


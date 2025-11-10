using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UsuarioApp.Domain.Dtos.Requests;
using UsuarioApp.Domain.Dtos.Responses;
using UsuarioApp.Domain.Interfaces.Services;
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
        public IActionResult Criar([FromBody] CriarContaRequest request)
        {
            try
            {
                var response = _usuarioService.CriarConta(request);
               return CreatedAtAction(nameof(Criar), response);
            }
            catch (ValidationException e)
            {
                return BadRequest(e.Errors.Select(e => new 
                {e.PropertyName, e.ErrorMessage}));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
        [HttpPost("autenticar")]
        [ProducesResponseType(typeof(AutenticarUsuarioResponse), 200)]
        public IActionResult Autenticar([FromBody] AutenticarUsuarioRequest request)
        {
            try
            {
                var response = _usuarioService.AutenticarUsuario(request);
                return Ok(response);
            }
            catch (ApplicationException e)
            {
                // Unauthorized
                return StatusCode(401, e.Message);
            }
            catch (Exception e)
            {
                // Internal Server Error
                return StatusCode(500, e.Message);
            }
        }
    }
}

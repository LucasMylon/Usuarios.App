using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;

namespace UsuarioApp.Domain.Validators
{
    public class PerfilValidator : AbstractValidator<Perfil>
    {
        public PerfilValidator()
        {
            RuleFor(p => p.Nome)
                .NotEmpty()
                .WithMessage("O nome do perfil é obrigatório.")
                .Length(6, 25)
                .WithMessage("O nome do perfil deve ter entre 6 e 25 caracteres.");
        }
    }
}

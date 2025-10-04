using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;

namespace UsuarioApp.Domain.Validators
{
    public class UsuarioValidator : AbstractValidator<Usuario>
    {
        public UsuarioValidator()
        {
            RuleFor(u => u.Nome)
                .NotEmpty()
                .WithMessage("O nome do usuário é obrigatório.")
                .Length(8, 150)
                .WithMessage("O nome do usuário deve ter entre 8 e 150 caracteres.");

            RuleFor(u => u.Email)
                .NotEmpty()
                .WithMessage("O email do usuário é obrigatório.")
                .EmailAddress()
                .WithMessage("O email do usuário deve ser um endereço de email válido.");

            RuleFor(u => u.Senha)
                .NotEmpty()
                .WithMessage("A senha do usuário é obrigatória.")
                .Matches("[A-Z]")
                .WithMessage("A senha do usuário deve conter pelo menos uma letra maiúscula.")
                .Matches("[a-z]")
                .WithMessage("A senha do usuário deve conter pelo menos uma letra minúscula.")
                .Matches("[0-9]")
                .WithMessage("A senha do usuário deve conter pelo menos um número.")
                .Matches("[^a-zA-Z0-9]")
                .WithMessage("A senha do usuário deve conter pelo menos um caractere especial.");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Entities;

namespace UsuariosApp.Infra.Data.Mappings
{
    public class UsuarioMap : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("USUARIOS");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Nome).IsRequired().HasMaxLength(150).HasColumnName("NOME");
            builder.Property(u => u.Id).HasColumnName("ID");
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100).HasColumnName("EMAIL");
            builder.Property(u => u.Senha).IsRequired().HasMaxLength(255).HasColumnName("SENHA");
            builder.Property(u => u.PerfilId).IsRequired().HasColumnName("PERFIL_ID");

            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasOne(u => u.Perfil).WithMany(p => p.Usuarios).HasForeignKey(u => u.PerfilId);

        }
    }
}

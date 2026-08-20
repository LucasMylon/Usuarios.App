using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsuarioApp.Domain.Entities;

namespace UsuariosApp.Infra.Data.Mappings;

public class UsuarioTokenMap : IEntityTypeConfiguration<UsuarioToken>
{
    public void Configure(EntityTypeBuilder<UsuarioToken> builder)
    {
        builder.ToTable("USUARIO_TOKENS");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("ID");
        builder.Property(t => t.UsuarioId).HasColumnName("USUARIO_ID");
        builder.Property(t => t.Tipo).HasConversion<string>().HasMaxLength(40).HasColumnName("TIPO");
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(512).HasColumnName("TOKEN_HASH");
        builder.Property(t => t.Destino).HasMaxLength(150).HasColumnName("DESTINO");
        builder.Property(t => t.CriadoEmUtc).HasColumnName("CRIADO_EM_UTC");
        builder.Property(t => t.ExpiraEmUtc).HasColumnName("EXPIRA_EM_UTC");
        builder.Property(t => t.ConsumidoEmUtc).HasColumnName("CONSUMIDO_EM_UTC");
        builder.Property(t => t.Tentativas).HasColumnName("TENTATIVAS");

        builder.HasIndex(t => new { t.Tipo, t.TokenHash });
        builder.HasIndex(t => new { t.UsuarioId, t.Tipo, t.ExpiraEmUtc });

        builder.HasOne(t => t.Usuario)
            .WithMany(u => u.Tokens)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

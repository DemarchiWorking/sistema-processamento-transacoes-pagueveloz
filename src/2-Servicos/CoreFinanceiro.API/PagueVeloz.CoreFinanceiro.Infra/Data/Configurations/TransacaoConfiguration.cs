using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Infra.Data.Configurations
{
    public class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
    {
        public void Configure(EntityTypeBuilder<Transacao> builder)
        {
            builder.ToTable("Transacoes");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Tipo).IsRequired().HasConversion<string>();
            builder.Property(t => t.Valor).IsRequired();
            builder.Property(t => t.ReferenceId).IsRequired().HasMaxLength(100);

            //indice para buscas futuras no extrato
            builder.HasIndex(t => t.ContaId);
            builder.HasIndex(t => t.ReferenceId);
        }
    }
}
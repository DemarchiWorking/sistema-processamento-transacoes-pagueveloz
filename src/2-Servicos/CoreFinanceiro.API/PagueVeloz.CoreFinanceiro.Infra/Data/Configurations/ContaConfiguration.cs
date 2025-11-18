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
    public class ContaConfiguration : IEntityTypeConfiguration<Conta>
    {
        public void Configure(EntityTypeBuilder<Conta> builder)
        {
            builder.ToTable("Contas");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever(); // PK vem do Contas.API

            //lockotimista [postgre]
            //alerta:IMPORTANTEbuilder.UseXminAsConcurrencyToken(); corrigir

            builder.HasMany(c => c.Transacoes)
            .WithOne()
            .HasForeignKey(t => t.ContaId)
            .OnDelete(DeleteBehavior.Cascade); //se a conta se for, o historico vai junto.

            //propriedades do ledger
            builder.Property(c => c.Balance).IsRequired();
            builder.Property(c => c.ReservedBalance).IsRequired();
            //propriedades replicadas [p/ autonomia]
            builder.Property(c => c.CreditLimit).IsRequired();
            builder.Property(c => c.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            //ignoramos propriedades calculadas
            builder.Ignore(c => c.AvailableBalance);
            builder.Ignore(c => c.PurchasingPower);
        }
    }
}
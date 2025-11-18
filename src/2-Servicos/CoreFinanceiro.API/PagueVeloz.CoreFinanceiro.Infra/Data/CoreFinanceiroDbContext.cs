using MassTransit;
using Microsoft.EntityFrameworkCore;
using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Infra.Data
{
    public class CoreFinanceiroDbContext : DbContext
    {
        public DbSet<Conta> Contas { get; set; }
        public DbSet<TransacaoProcessada> TransacoesProcessadas { get; set; }
        public CoreFinanceiroDbContext(DbContextOptions<CoreFinanceiroDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            //resiliencia:
            //o outbox (p/ publicar eventos futuros)
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            //eo Inbox (p/ garantir que o ContaCriadaConsumer soh processe a mensagem uma vez||atomicamente)
            modelBuilder.AddInboxStateEntity();
        }
    }
}
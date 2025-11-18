using Microsoft.EntityFrameworkCore;
using PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces;
using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Infra.Data.Repositories
{
    public class TransacaoProcessadaRepository : ITransacaoProcessadaRepository
    {
        private readonly CoreFinanceiroDbContext _context;

        public TransacaoProcessadaRepository(CoreFinanceiroDbContext context)
        {
            _context = context;
        }

        public void Adicionar(TransacaoProcessada transacao)
        {
            _context.TransacoesProcessadas.Add(transacao);
        }

        public async Task<bool> JaProcessadaAsync(string referenceId, CancellationToken cancellationToken)
        {
            return await _context.TransacoesProcessadas
                .AsNoTracking()
                .AnyAsync(t => t.ReferenceId == referenceId, cancellationToken);
        }
    }
}
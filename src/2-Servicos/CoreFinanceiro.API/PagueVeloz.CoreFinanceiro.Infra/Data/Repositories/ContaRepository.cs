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
    public class ContaRepository : IContaRepository
    {
        private readonly CoreFinanceiroDbContext _context;

        public ContaRepository(CoreFinanceiroDbContext context)
        {
            _context = context;
        }

        public void Adicionar(Conta conta)
        {
            _context.Contas.Add(conta);
        }

        public async Task<bool> ExisteAsync(string contaId, CancellationToken cancellationToken)
        {
            return await _context.Contas
                .AsNoTracking()
                .AnyAsync(c => c.Id == contaId, cancellationToken);
        }
        public async Task<Conta?> ObterPorIdAsync(string id, CancellationToken cancellationToken)
        {
            //usamos include para trazer o historico junto, pois ele faz parte do agregado e eh necessario para add novas transacoes.
            return await _context.Contas
                .Include(c => c.Transacoes)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
    }
}

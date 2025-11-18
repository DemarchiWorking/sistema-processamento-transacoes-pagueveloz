using PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Infra.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CoreFinanceiroDbContext _context;

        public UnitOfWork(CoreFinanceiroDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
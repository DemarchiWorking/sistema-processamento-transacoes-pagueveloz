using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces
{
    public interface IContaRepository
    {
        ///<summary>
        ///adiciona uma nova conta [ledger].
        ///</summary>
        void Adicionar(Conta conta);

        ///<summary>
        ///verifica c uma conta ja existe [idempoteencia consumidor].
        ///</summary>
        Task<bool> ExisteAsync(string contaId, CancellationToken cancellationToken);

        ///<summary>
        ///busca uma conta pelo ID para ser modificada [tracking].
        /// </summary>
        Task<Conta?> ObterPorIdAsync(string id, CancellationToken cancellationToken);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
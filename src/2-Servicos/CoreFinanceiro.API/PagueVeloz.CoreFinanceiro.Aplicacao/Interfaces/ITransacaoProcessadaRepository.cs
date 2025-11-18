using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces
{
    public interface ITransacaoProcessadaRepository
    {
        ///<summary>
        ///verifica c um reference_id ja foi processado.
        ///</summary>
        Task<bool> JaProcessadaAsync(string referenceId, CancellationToken cancellationToken);

        ///<summary>
        ///adiciona um reference_id ao contexto [p/c salvo na transacao].
        ///</summary>
        void Adicionar(TransacaoProcessada transacao);
    }
}
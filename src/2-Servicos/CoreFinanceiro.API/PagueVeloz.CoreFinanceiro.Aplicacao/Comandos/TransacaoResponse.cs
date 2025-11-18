using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Comandos
{
    ///<summary>
    ///dto de resposta padrao para as transações.
    ///</summary>
    public record TransacaoResponse(
        string TransactionId,
        string Status, //"success"||"failed"
        long Balance,
        long ReservedBalance,
        long AvailableBalance,
        DateTime Timestamp,
        string? ErrorMessage
    );
}
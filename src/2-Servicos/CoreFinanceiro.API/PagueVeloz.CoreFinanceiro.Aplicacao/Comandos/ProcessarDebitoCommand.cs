using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Comandos
{
    ///<summary>
    ///comando para o Mediatr debitar.
    ///</summary>
    public record ProcessarDebitoCommand(
        string AccountId,
        long Amount,
        string Currency,
        string ReferenceId,
        JsonObject? Metadata
    ) : IRequest<TransacaoResponse>; //reutiliza dto resposta
}
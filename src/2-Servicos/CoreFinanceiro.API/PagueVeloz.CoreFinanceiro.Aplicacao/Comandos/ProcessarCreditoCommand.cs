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
    ///comando para o MediatR representa a intencao de creditar.
    ///</summary>
    public record ProcessarCreditoCommand(
        string AccountId,
        long Amount,
        string Currency,
        string ReferenceId,
        JsonObject? Metadata //jsonObject
    ) : IRequest<TransacaoResponse>;
}
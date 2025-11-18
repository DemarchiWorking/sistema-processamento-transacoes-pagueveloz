using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Comandos
{
    public record ProcessarEstornoCommand(
        string AccountId,
        long Amount,
        string Currency,
        string ReferenceId,
        JsonObject? Metadata //originalReferenceId
    ) : IRequest<TransacaoResponse>;
}
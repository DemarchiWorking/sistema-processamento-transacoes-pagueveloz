using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.CoreFinanceiro.Aplicacao.Comandos;
using PagueVeloz.CoreFinanceiro.Dominio.DTOs.Request;

namespace PagueVeloz.CoreFinanceiro.Api.Controller
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransacaoRequest request)
        {
            //roteador de comandos
            object command;

            switch (request.Operation.ToLower())
            {
                case "credit":
                    command = new ProcessarCreditoCommand(
                        request.AccountId,
                        request.Amount,
                        request.Currency,
                        request.ReferenceId,
                        request.Metadata
                    );
                    break;
                case "debit":
                    command = new ProcessarDebitoCommand( 
                        request.AccountId,
                        request.Amount,
                        request.Currency,
                        request.ReferenceId,
                        request.Metadata
                    );
                    break;
                case "reserve":
                    command = new ProcessarReservaCommand(
                        request.AccountId,
                        request.Amount,
                        request.Currency,
                        request.ReferenceId,
                        request.Metadata
                    );
                    break;

                case "capture":
                    command = new ProcessarCapturaCommand(
                        request.AccountId,
                        request.Amount,
                        request.Currency,
                        request.ReferenceId,
                        request.Metadata
                    );
                    break;
                case "reversal":
                    command = new ProcessarEstornoCommand(
                    request.AccountId,
                    request.Amount,
                    request.Currency,
                    request.ReferenceId,
                    request.Metadata
                );
                    break;
                    //return BadRequest(new { Error = $"Operação '{request.Operation}' ainda não implementada." });

                case "transfer":
                    return BadRequest(new { Error = "Operação 'transfer' deve ser chamada pelo endpoint /api/transfers." });

                default:
                    return BadRequest(new { Error = $"Operação '{request.Operation}' desconhecida." });
            }

            var response = await _mediator.Send(command);

            //retornar um Result<T> p sabe c é 200 OK, 400 Bad Request, 500...)

            //enqnt isso, assumir q o dto de resposta indica o status
            if (response is TransacaoResponse tr && tr.Status == "failed")
            {
                return BadRequest(tr);
            }

            return Ok(response);
        }
    }
}
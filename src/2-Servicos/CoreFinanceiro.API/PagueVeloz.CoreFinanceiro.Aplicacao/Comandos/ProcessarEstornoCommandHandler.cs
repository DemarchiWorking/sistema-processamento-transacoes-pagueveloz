using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces;
using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using PagueVeloz.CoreFinanceiro.Dominio.Exceptions;
using PagueVeloz.Eventos.CoreFinanceiro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Comandos
{
    public class ProcessarEstornoCommandHandler : IRequestHandler<ProcessarEstornoCommand, TransacaoResponse>
    {
        private readonly IContaRepository _contaRepository;
        private readonly ITransacaoProcessadaRepository _transacaoProcessadaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProcessarReservaCommandHandler> _logger;

        public ProcessarEstornoCommandHandler(
                IContaRepository contaRepository,
                ITransacaoProcessadaRepository transacaoProcessadaRepository,
                IUnitOfWork unitOfWork,
                IPublishEndpoint publishEndpoint,
                ILogger<ProcessarDebitoCommandHandler> logger
            )
        {
            _contaRepository = contaRepository;
            _transacaoProcessadaRepository = transacaoProcessadaRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            //alerta:_logger = logger;
        }
        public async Task<TransacaoResponse> Handle(ProcessarEstornoCommand request, CancellationToken cancellationToken)
        {
            if (await _transacaoProcessadaRepository.JaProcessadaAsync(request.ReferenceId, cancellationToken))
            {
                _logger.LogWarning("Transação {ReferenceId} já processada (idempotência).", request.ReferenceId);
                return new TransacaoResponse($"DUPLICATE-{request.ReferenceId}", "success", 0, 0, 0, DateTime.UtcNow, null);
            }

            //validar metadata[logica especifica d estorno~]
            if (request.Metadata == null ||
                !request.Metadata.TryGetPropertyValue("originalReferenceId", out var idNode) ||
                idNode?.GetValue<string>() is not string originalReferenceId ||
                string.IsNullOrWhiteSpace(originalReferenceId))
            {
                _logger.LogWarning("Estorno {ReferenceId} falhou: 'originalReferenceId' ausente no metadata.", request.ReferenceId);
                //alerta:return Falha("Metadata 'originalReferenceId' é obrigatório para estorno.", request.ReferenceId, 0, 0, 0);
                return null;
            }

            Conta? conta;
            try
            {

                conta = await _contaRepository.ObterPorIdAsync(request.AccountId, cancellationToken);
                if (conta == null)
                    return null;
                    //alerta:return Falha("Conta não encontrada.", request.ReferenceId, 0, 0, 0);


                //regra de dominio
                //passar o id original, o agregado encontra a transacao
                //e aplica a logica de compensacao.
                var transacao = conta.Estornar(request.Amount, request.ReferenceId, originalReferenceId);

                _transacaoProcessadaRepository.Adicionar(new TransacaoProcessada(request.ReferenceId));


                var evento = new EstornoEfetuadoEvent(
                    conta.Id,
                    transacao.Id,
                    request.ReferenceId,
                    originalReferenceId, //passa o iD original
                    request.Amount,
                    request.Currency,
                    request.Metadata?.ToJsonString(),
                    new SaldosContaDto(conta.Balance, conta.ReservedBalance, conta.AvailableBalance),
                    transacao.Timestamp
                );
                await _publishEndpoint.Publish(evento, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new TransacaoResponse(
                    transacao.Id,
                    "success",
                    conta.Balance,
                    conta.ReservedBalance,
                    conta.AvailableBalance,
                    transacao.Timestamp,
                    null
                );
            }
           // catch (DomainException ex) //erro de regra [ex: saldo insuficiente, Tx Original ñ encontrad]
           // {
            //    _logger.LogWarning(ex, "Falha na regra de negócio: {ReferenceId}", request.ReferenceId);
            //    conta = await _contaRepository.ObterPorIdAsync(request.AccountId, cancellationToken);
                //   return Falha(ex.Message, request.ReferenceId,
                //      conta?.Balance ?? 0,
                //       conta?.ReservedBalance ?? 0,
                //       conta?.AvailableBalance ?? 0);
           // }
            //catch (DbUpdateException ex) //lock otimista ou idempotencia
            //{
            //    // 
            // }
            catch (Exception ex) // Erro inesperado
            {
                //

                return null;
            }
        }

        //
    }
}
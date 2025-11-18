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
    public class ProcessarReservaCommandHandler : IRequestHandler<ProcessarReservaCommand, TransacaoResponse>
    {
        private readonly IContaRepository _contaRepository;
        private readonly ITransacaoProcessadaRepository _transacaoProcessadaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProcessarReservaCommandHandler> _logger;

        public ProcessarReservaCommandHandler(
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
        public async Task<TransacaoResponse> Handle(ProcessarReservaCommand request, CancellationToken cancellationToken)
        {
            if (await _transacaoProcessadaRepository.JaProcessadaAsync(request.ReferenceId, cancellationToken))
            {
                _logger.LogWarning("Transação {ReferenceId} já processada (idempotência).", request.ReferenceId);
                return new TransacaoResponse($"DUPLICATE-{request.ReferenceId}", "success", 0, 0, 0, DateTime.UtcNow, null);
            }

            Conta? conta;
            try
            {
                conta = await _contaRepository.ObterPorIdAsync(request.AccountId, cancellationToken);
                if (conta == null)
                    return null;
                   //alerta:return Falha("Conta não encontrada.", request.ReferenceId, 0, 0, 0);

                var transacao = conta.Reservar(request.Amount, request.ReferenceId, request.Currency);

                _transacaoProcessadaRepository.Adicionar(new TransacaoProcessada(request.ReferenceId));

                var evento = new ReservaEfetuadaEvent( 
                    conta.Id,
                    transacao.Id,
                    request.ReferenceId,
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
            catch (DomainException ex) // Erro de regra [Ex: saldo disponivel insuficiente]
            {
                // 
            }
            return null; //alerta:retorno null
            //catch (DbUpdateException ex) //lock otimista ou idempotencia
            // {
            //
            // }
            // 
        }

        //
    }
}
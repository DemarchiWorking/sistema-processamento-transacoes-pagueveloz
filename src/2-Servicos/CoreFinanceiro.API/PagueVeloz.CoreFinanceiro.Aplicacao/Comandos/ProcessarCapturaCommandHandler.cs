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

    public class ProcessarCapturaCommandHandler : IRequestHandler<ProcessarCapturaCommand, TransacaoResponse>
    {
        private readonly IContaRepository _contaRepository;
        private readonly ITransacaoProcessadaRepository _transacaoProcessadaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProcessarDebitoCommandHandler> _logger;

        public ProcessarCapturaCommandHandler(
            IContaRepository contaRepository,
            ITransacaoProcessadaRepository transacaoProcessadaRepository,
            IUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ILogger<ProcessarDebitoCommandHandler> logger)
        {
            _contaRepository = contaRepository;
            _transacaoProcessadaRepository = transacaoProcessadaRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<TransacaoResponse> Handle(ProcessarCapturaCommand request, CancellationToken cancellationToken)
        {
            //check idepotencia | identica
            if (await _transacaoProcessadaRepository.JaProcessadaAsync(request.ReferenceId, cancellationToken))
            {
                _logger.LogWarning("Transação {ReferenceId} já processada (idempotência).", request.ReferenceId);
                return new TransacaoResponse($"DUPLICATE-{request.ReferenceId}", "success", 0, 0, 0, DateTime.UtcNow, null);
            }

            Conta? conta;
            try
            {
                //carrega agregado
                conta = await _contaRepository.ObterPorIdAsync(request.AccountId, cancellationToken);
                if (conta == null)
                    //alert:null return Falha("Conta não encontrada.", request.ReferenceId, 0, 0, 0);
                    return null;
                //regra de dominio
                var transacao = conta.Capturar(request.Amount, request.ReferenceId, request.Currency);

                //identico
                _transacaoProcessadaRepository.Adicionar(new TransacaoProcessada(request.ReferenceId));

                //prepara evento [OUTBOX]
                var evento = new ReservaCapturadaEvent(
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

                //commit atomico
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                //retorno sucesso
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
            catch(Exception ex)
            {
                //alert: retorno null
            }
            return null; //alerta:retorno null
        }
    }
}
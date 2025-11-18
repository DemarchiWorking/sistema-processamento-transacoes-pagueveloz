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
    public class ProcessarDebitoCommandHandler : IRequestHandler<ProcessarDebitoCommand, TransacaoResponse>
    {
        private readonly IContaRepository _contaRepository;
        private readonly ITransacaoProcessadaRepository _transacaoProcessadaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProcessarDebitoCommandHandler> _logger;

        public ProcessarDebitoCommandHandler(
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

        public async Task<TransacaoResponse> Handle(ProcessarDebitoCommand request, CancellationToken cancellationToken)
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
                    return Falha("Conta não encontrada.", request.ReferenceId, 0, 0, 0);


                var transacao = conta.Debitar(request.Amount, request.ReferenceId, request.Currency);


                _transacaoProcessadaRepository.Adicionar(new TransacaoProcessada(request.ReferenceId));

                var evento = new ContaDebitadaEvent( 
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
            catch (DomainException ex) //erro de regra de negócio [ex: sldo insuficiente]
            {
            /*
                _logger.LogWarning(ex, "Falha na regra de negócio: {ReferenceId}", request.ReferenceId);
                //obrigatorio:retornar o saldo atual mesmo na falha
                conta = await _contaRepository.ObterPorIdAsync(request.AccountId, cancellationToken);
                return Falha(ex.Message, request.ReferenceId,
                    conta?.Balance ?? 0,
                    conta?.ReservedBalance ?? 0,
                    conta?.AvailableBalance ?? 0);
            }
            catch (DbUpdateException ex) // Lock Otimista ou Idempotência
            {
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    _logger.LogWarning(ex, "Conflito de idempotência [PK]: {ReferenceId}", request.ReferenceId);
                    return new TransacaoResponse($"DUPLICATE-{request.ReferenceId}", "success", 0, 0, 0, DateTime.UtcNow, null);
                }

                _logger.LogWarning(ex, "Conflito de concorrência [otimista]: {ReferenceId}", request.ReferenceId);
                return Falha("conflito de concorrencia. Tente novamente.", request.ReferenceId, 0, 0, 0);
            }
            catch (Exception ex) 
            {*/
                _logger.LogError(ex, "Erro inesperado ao processar {ReferenceId}", request.ReferenceId);
                return Falha("Erro interno do servidor.", request.ReferenceId, 0, 0, 0);
            }
        }

        //retornar o saldo atual em caso de falha [obrigatorio]
        private TransacaoResponse Falha(string mensagemErro, string referenceId, long balance, long reserved, long available)
        {
            return new TransacaoResponse(
                $"FAILED-{referenceId}", "failed",
                balance, reserved, available,
                DateTime.UtcNow, mensagemErro
            );
        }
    }
}
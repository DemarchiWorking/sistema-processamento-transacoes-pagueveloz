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
    public class ProcessarCreditoCommandHandler : IRequestHandler<ProcessarCreditoCommand, TransacaoResponse>
    {
        private readonly IContaRepository _contaRepository;
        private readonly ITransacaoProcessadaRepository _transacaoProcessadaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProcessarCreditoCommandHandler> _logger;

        public ProcessarCreditoCommandHandler(
            IContaRepository contaRepository,
            ITransacaoProcessadaRepository transacaoProcessadaRepository,
            IUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ILogger<ProcessarCreditoCommandHandler> logger)
        {
            _contaRepository = contaRepository;
            _transacaoProcessadaRepository = transacaoProcessadaRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<TransacaoResponse> Handle(ProcessarCreditoCommand request, CancellationToken cancellationToken)
        {
            if (await _transacaoProcessadaRepository.JaProcessadaAsync(request.ReferenceId, cancellationToken))
            {
                _logger.LogWarning("Transação {ReferenceId} já processada (idempotência).", request.ReferenceId);
                // Retornar 'success' pois a operação original foi um sucesso. | alert:buscar a resposta original
                return new TransacaoResponse(
                    $"DUPLICATE-{request.ReferenceId}", "success", 0, 0, 0, DateTime.UtcNow, null);
            }

            Conta? conta;
            try
            {
                //carrega agregado
                conta = await _contaRepository.ObterPorIdAsync(request.AccountId, cancellationToken);
                if (conta == null)
                    return Falha("Conta não encontrada.", request.ReferenceId);

                //regra de dominio
                var transacao = conta.Creditar(request.Amount, request.ReferenceId, request.Currency);

                //grv idepotencia
                _transacaoProcessadaRepository.Adicionar(new TransacaoProcessada(request.ReferenceId));

                // prepara evento [OUTBOX]
                var evento = new ContaCreditadaEvent(
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
                //salva a conta [v2], a transacao [nova], a transacaoProcessada[nova]
                //e  evento [Outbox] em uma 1 transacao.
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
            catch (DomainException ex) // Erro de regra de negócio
            {
                _logger.LogWarning(ex, "Falha na regra de negócio: {ReferenceId}", request.ReferenceId);
                return Falha(ex.Message, request.ReferenceId);
            }
            //catch (DbUpdateException ex) // Lock Otimista ou Idempotência (Violação de PK)
            catch (Exception ex) // Lock Otimista ou Idempotência (Violação de PK)
            {
                //checa c e violação de PK [Idempotência]
                //if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                //{
                //    _logger.LogWarning(ex, "Conflito de idempotência [PK]: {ReferenceId}", request.ReferenceId);
                //    return new TransacaoResponse($"DUPLICATE-{request.ReferenceId}", "success", 0, 0, 0, DateTime.UtcNow, null);
                //}

                //se n for, e provavelmnt conflito de concorrencia [Lock Otimista]
                // _logger.LogWarning(ex, "Conflito de concorrência ]Lock Otimista]: {ReferenceId}", request.ReferenceId);
                // return Falha("Conflito de concorrência. Tente novamente.", request.ReferenceId);
                //}
                // catch (Exception ex) 
                //{
                _logger.LogError(ex, "Erro inesperado ao processar {ReferenceId}", request.ReferenceId);
                return Falha("Erro interno do servidor.", request.ReferenceId);
            }
        }

        private TransacaoResponse Falha(string mensagemErro, string referenceId)
        {
            return new TransacaoResponse(
                $"FAILED-{referenceId}", "failed", 0, 0, 0,
                DateTime.UtcNow, mensagemErro
            );
        }
    }
}
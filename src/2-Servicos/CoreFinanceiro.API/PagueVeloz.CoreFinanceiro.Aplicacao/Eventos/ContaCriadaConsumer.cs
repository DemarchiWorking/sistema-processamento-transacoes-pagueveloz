using MassTransit;
using Microsoft.Extensions.Logging;
using PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces;
using PagueVeloz.CoreFinanceiro.Dominio.Aggregates;
using PagueVeloz.Eventos.Contas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Aplicacao.Eventos
{
    ///<summary>
    ///consumidor,ouve o evento ContaCriadaEvent
    ///</summary>
    public class ContaCriadaConsumer : IConsumer<ContaCriadaEvent>
    {
        private readonly ILogger<ContaCriadaConsumer> _logger;
        private readonly IContaRepository _contaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ContaCriadaConsumer(
            ILogger<ContaCriadaConsumer> logger,
            IContaRepository contaRepository,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _contaRepository = contaRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<ContaCriadaEvent> context)
        {
            var evento = context.Message;
            _logger.LogInformation("Recebido ContaCriadaEvent para AccountId: {AccountId}", evento.AccountId);

            //se o evento for usado mais de uma vez, não dv duplicar.
            if (await _contaRepository.ExisteAsync(evento.AccountId, context.CancellationToken))
            {
                _logger.LogWarning("Conta {AccountId} já existe. Evento duplicado.", evento.AccountId);
                return;
            }

            //Ledger
            var status = (Dominio.Enums.StatusContaFinanceira)evento.Status;

            var conta = Conta.Criar(
                evento.AccountId,
                evento.InitialBalance,
                evento.LimiteDeCredito,
                status
            );

            //persistencia
            _contaRepository.Adicionar(conta);


            //Consume so executa uma vez.
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Ledger da conta {AccountId} criado com sucesso.", conta.Id);
        }
    }
}

using PagueVeloz.CoreFinanceiro.Dominio.Enums;
using PagueVeloz.CoreFinanceiro.Dominio.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Dominio.Aggregates
{
    ///<summary>
    ///agregado cnta [ledger].
    ///[critica] usado nos saldos e regras de transacao.
    ///</summary>
    public class Conta
    {
        ///<summary>
        ///id da conta [PK], mesmo id do Contas.API.
        ///</summary>
        public string Id { get; private set; }

        ///<summary>
        ///saldo total [disponivel+reservado] em centavos.
        ///</summary>
        public long Balance { get; private set; }

        ///<summary>
        ///saldo reservado em centavos.
        ///</summary>
        public long ReservedBalance { get; private set; }

        ///<summary>
        ///limite de credito em centavos. [replicado do Contas.API]
        ///</summary>
        public long CreditLimit { get; private set; }

        ///<summary>
        ///status [replicado do Contas.API].
        ///</summary>
        public StatusContaFinanceira Status { get; private set; }

        ///<summary>
        ///</summary>
        public uint xmin { get; private set; }
        ///<summary>
        ///</summary>
        private readonly List<Transacao> _transacoes = new();
        ///<summary>
        ///</summary>
        public IReadOnlyCollection<Transacao> Transacoes => _transacoes.AsReadOnly();


        ///<summary>
        ///saldo disponivel [calculado].
        ///saldo Total | reservado
        ///</summary>
        public long AvailableBalance => Balance - ReservedBalance;

        ///<summary>
        ///poder de compra [calculado].
        ///saldo disponivel + limite de credito
        ///</summary>
        public long PurchasingPower => AvailableBalance + CreditLimit;

        private Conta() { }

        ///<summary>
        ///metodo de fabrica. usado pelo consumer para iniciar o agregado no CoreFinanceiro.
        ///</summary>
        public static Conta Criar(
            string id,
            long initialBalance,
            long creditLimit,
            StatusContaFinanceira status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            if (initialBalance < 0)
                throw new Exception("");
            //alerta:DomainException("Saldo inicial não pode ser negativo.");
            if (creditLimit < 0)
                throw new Exception("");
            //alerta:DomainException("Limite de crédito não pode ser negativo.");
            return new Conta
            {
                Id = id,
                Balance = initialBalance,
                CreditLimit = creditLimit,
                Status = status,
                ReservedBalance = 0 //contas iniciam sem saldo reservado
            };
        }
        ///<summary>
        ///executa a regra de negocio de Credito.
        ///</summary>
        public Transacao Creditar(long valor, string referenceId, string currency)
        {
            //validar regras de negocio
            if (Status == StatusContaFinanceira.Blocked || Status == StatusContaFinanceira.Inactive)
                throw new DomainException("Conta não está ativa. Operação rejeitada.");

            if (valor <= 0)
                throw new DomainException("Valor do crédito deve ser positivo.");

            // (alerta:Validacao de currency  aqui adicionar)
            Balance += valor;

            //criar e registrar o historico
            var transacao = new Transacao(Id, TipoTransacao.Credit, valor, referenceId);
            _transacoes.Add(transacao);

            return transacao;
        }
        ///<summary>
        ///executa a regra de negocio de Debito.
        ///valida o poder de compra [saldo disponível+limite de credito].
        ///</summary>
        public Transacao Debitar(long valor, string referenceId, string currency)
            {
                //validar regras de negocio
                if (Status != StatusContaFinanceira.Active)
                    throw new DomainException("Conta não está ativa. Operação rejeitada.");

                if (valor <= 0)
                    throw new DomainException("Valor do débito deve ser positivo.");

                //regra 
                //valor do debito ñ pode ser maior que o poder de compra.
                if (valor > PurchasingPower)
                {
                    //lança a excecao de dominio que sera tratada pelo Handler.
                    throw new DomainException("Saldo insuficiente (incluindo limite de crédito).");
                }

                //executar
                //debito diminui o balance total.
                //availablebalance sera recalculado [balance-reservedBalance].
                Balance -= valor;

                //criar e registrar o historico
                var transacao = new Transacao(Id, TipoTransacao.Debit, valor, referenceId);
                _transacoes.Add(transacao);

                return transacao;
            }
        ///<summary>
        ///executa a regra de negocio de reserva
        ///move o saldo de disponivel para reservado | ñ altera o saldo total [Balance].
        ///</summary>
        public Transacao Reservar(long valor, string referenceId, string currency)
        {
            //regras negocio
            if (Status != StatusContaFinanceira.Active)
                throw new DomainException("Conta não está ativa. Operação rejeitada.");

            if (valor <= 0)
                throw new DomainException("Valor da reserva deve ser positivo.");

            //reservas so podem ser feitas com saldo disponível
            if (valor > AvailableBalance)
            {
                throw new DomainException("Saldo disponível insuficiente para reserva.");
            }

            //executa
            //o saldo total [balance] ñ muda, só o reservado.
            ReservedBalance += valor;

            //criar e registrar o historico
            var transacao = new Transacao(Id, TipoTransacao.Reserve, valor, referenceId);
            _transacoes.Add(transacao);

            return transacao;
        }


        ///<summary>
        ///executa a regra de negocio de captura
        ///remove o saldo de reservadoe do saldo total.
        ///</summary>
        public Transacao Capturar(long valor, string referenceId, string currency)
        {
            //validar regras negocio
            if (Status != StatusContaFinanceira.Active)
                throw new DomainException("Conta não está ativa. Operação rejeitada.");

            if (valor <= 0)
                throw new DomainException("Valor da captura deve ser positivo.");

            //capturas so podem ser feitas com saldo reservado suficiente
            if (valor > ReservedBalance)
            {
                throw new DomainException("Saldo reservado insuficiente para captura.");
            }

            //executa
            //a captura diminui tanto o reservado quanto o saldo total.
            ReservedBalance -= valor;
            Balance -= valor;

            //criar e registrar o historico
            var transacao = new Transacao(Id, TipoTransacao.Capture, valor, referenceId);
            _transacoes.Add(transacao);

            return transacao;
        }
        ///<summary>
        ///executa a regra de negocio de Estorno [reversal]
        ///encontra a transacao original dentro do agregado e aplica a logica de compensacao.
        ///</summary>
        public Transacao Estornar(long valorEstorno, string referenceIdEstorno, string originalReferenceId)
        {
            //regras de negocio
            if (Status != StatusContaFinanceira.Active)
                throw new DomainException("Conta não está ativa. Operação rejeitada.");

            if (valorEstorno <= 0)
                throw new DomainException("Valor do estorno deve ser positivo.");

            //encontrar a transacao original dentro do agregado
            var transacaoOriginal = _transacoes.FirstOrDefault(t => t.ReferenceId == originalReferenceId);

            if (transacaoOriginal == null)
                throw new DomainException($"Transação original com ReferenceId '{originalReferenceId}' não encontrada nesta conta.");

            //aplicar a logica de compensacao
            switch (transacaoOriginal.Tipo)
            {
                case TipoTransacao.Debit:
                case TipoTransacao.Capture:
                    //reverter um debito ou captura = um novo credito || creditos nao tem validacao de saldo.
                    Balance += valorEstorno;
                    break;

                case TipoTransacao.Credit:
                    //reverter um credito = um novo debito
                    //debitos devem respeitar o poder de compra.
                    if (valorEstorno > PurchasingPower)
                        throw new DomainException("Saldo insuficiente (incluindo limite) para estornar o crédito.");

                    Balance -= valorEstorno;
                    break;

                case TipoTransacao.Reserve:
                    //reeverter uma reserva = liberar o valor reservado
                    //devemos checar se o valor ainda esta reservado.
                    if (valorEstorno > ReservedBalance)
                        throw new DomainException("Saldo reservado insuficiente para estornar a reserva.");

                    ReservedBalance -= valorEstorno;
                    //balance total  ñ muda
                    break;

                case TipoTransacao.Reversal:
                    throw new DomainException("Não é permitido estornar um estorno.");

                default:
                    throw new DomainException($"Tipo de transação original '{transacaoOriginal.Tipo}' não pode ser estornado.");
            }

            //criar e registrar o historico
            var transacao = new Transacao(Id, TipoTransacao.Reversal, valorEstorno, referenceIdEstorno);
            _transacoes.Add(transacao);

            return transacao;
        }
    }
}
    //void Debitar(long amount) {}
    //void Reservar(long amount) {}
    //void Capturar(long amount) {}


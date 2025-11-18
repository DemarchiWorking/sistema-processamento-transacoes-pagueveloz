using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Dominio.Aggregates
{
    public enum TipoTransacao
    {
        Credit,
        Debit,
        Reserve,
        Capture,
        Reversal
    }

    ///<summary>
    ///entidade filha que rastreia o historico de transacoes interno do agregado Conta.
    ///</summary>
    public class Transacao
    {
        public string Id { get; private set; }
        public string ContaId { get; private set; } //chave estrangeira
        public TipoTransacao Tipo { get; private set; }
        public long Valor { get; private set; }
        public string ReferenceId { get; private set; }
        public DateTime Timestamp { get; private set; }

        private Transacao() { }

        internal Transacao(string contaId, TipoTransacao tipo, long valor, string referenceId)
        {
            Id = $"TXN-PROC-{Guid.NewGuid():N}";
            ContaId = contaId;
            Tipo = tipo;
            Valor = valor;
            ReferenceId = referenceId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
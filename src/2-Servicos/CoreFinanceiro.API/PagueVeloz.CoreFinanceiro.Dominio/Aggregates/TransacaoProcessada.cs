using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Dominio.Aggregates
{
    ///<summary>
    ///entidade separada para a idempotencia.[proprio agregado] 
    ///Usar a PK do banco (ReferenceId) para garantir || entrega de mensagens uma vez.
    ///</summary>
    public class TransacaoProcessada
    {
        ///<summary>
        ///O reference_id da requisicao | esta e a chave primaria.
        ///</summary>
        public string ReferenceId { get; private set; }
        public DateTime Timestamp { get; private set; }

        private TransacaoProcessada() { }

        public TransacaoProcessada(string referenceId)
        {
            ReferenceId = referenceId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Dominio.DTOs.Request
{
    ///<summary>
    ///dto de entrada para o endpoint de transacoes,
    ///</summary>
    public class TransacaoRequest
    {
        public string Operation { get; set; }
        public string AccountId { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string ReferenceId { get; set; }
        public JsonObject? Metadata { get; set; }
    }
}
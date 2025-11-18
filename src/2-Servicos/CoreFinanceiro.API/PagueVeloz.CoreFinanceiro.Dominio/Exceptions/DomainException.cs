using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Dominio.Exceptions
{
    ///<summary>
    ///excecao de dominio, p/ quando uma regra de negpcio e violada.
    ///</summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
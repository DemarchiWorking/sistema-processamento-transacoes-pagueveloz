using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.CoreFinanceiro.Dominio.Enums
{
    ///<summary>
    ///enum local para o status da conta, reduzir acoplamnt
    ///</summary>
    public enum StatusContaFinanceira
    {
        Active,
        Inactive,
        Blocked
    }
}
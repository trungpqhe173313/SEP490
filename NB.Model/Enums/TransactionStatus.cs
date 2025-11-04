using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Model.Enums
{
    public enum TransactionStatus
    {
        //xuất kho
        draft,
        order,
        delivering,
        done,
        cancel,
        //nhập kho
        checking,
        @checked
    }
}

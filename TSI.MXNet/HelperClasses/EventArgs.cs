using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSI.MXNet
{
    public class ResponseErrorEventArgs : EventArgs
    {
        public string ErrorMsg { get; set; }
    }
}

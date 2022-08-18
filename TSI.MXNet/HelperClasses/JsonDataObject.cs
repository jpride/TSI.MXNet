using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSI.MXNet
{
    public class JsonDataObject
    {
        public string cmd { get; set; }
        public string info { get; set; }
        public int code { get; set; }
        public string error { get; set; }
        
    }

    public class InfoObject
    { 
        public string id { get; set; }
        public string ip { get; set; }
    }
}

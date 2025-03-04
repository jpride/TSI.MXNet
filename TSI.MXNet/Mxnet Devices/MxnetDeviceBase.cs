using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSI.MXNet
{
    public class MxnetDeviceBase
    {
        public string ip { get; set; }
        public string id { get; set; }
        public string mac { get; set; }
        public string modelname { get; set; }
    }

    public class MxnetDecoder : MxnetDeviceBase
    {
        public string streamSource { get; set; }
    }

    public class MxnetEncoder : MxnetDeviceBase
    {
    
    }

}

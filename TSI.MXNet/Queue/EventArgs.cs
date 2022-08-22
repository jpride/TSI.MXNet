using System;


namespace TSI.FourSeries.CommandQueue
{
    public class ProcessQueueEventArgs: EventArgs
    {
        public string cmd { get; set; }
    }
}

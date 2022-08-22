using System;
using System.Collections.Generic;



namespace TSI.FourSeries.CommandQueue
{
    public class CommandQueue
    {
        private Queue<string> CommandQueueObj = new Queue<string>();
        public event EventHandler<ProcessQueueEventArgs> ProcessQueueEventCall;


        public CommandQueue()
        { 
            //empty instatiator        
        }


        public void AddCommand(string cmd)
        {
            CommandQueueObj.Enqueue(cmd);
        }

        public string ProcessQueue()
        {
            if (CommandQueueObj.Count > 0)
            {
                return CommandQueueObj.Dequeue();
            }

            else
            { 
                return null;    
            }
        }

        private int GetQueueCount()
        {
            return CommandQueueObj.Count;
        }

        
    }
}

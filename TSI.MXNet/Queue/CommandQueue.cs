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

        public void ProcessQueueEvent()
        {
            if (CommandQueueObj.Count > 0)
            {
                //clever litte trick from CoPilot wherein the queue is copied the original queue cleared so that we can interate thru the queue and process anything within
                Queue<string> queueCopy = new Queue<string>(CommandQueueObj);
                CommandQueueObj.Clear();

                //interate thru queue and fire event for each entry, event is handled by cbox class 
                foreach(var item in queueCopy) 
                {
                    ProcessQueueEventArgs args = new ProcessQueueEventArgs()
                    {
                        cmd = item
                    };

                    ProcessQueueEventCall?.Invoke(this, args);
                }
            }
        }
        
        
    }
}

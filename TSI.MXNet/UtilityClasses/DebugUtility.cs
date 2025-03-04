using Crestron.SimplSharp;

namespace TSI.MXNet.UtilityClasses
{
    public static class DebugUtility
    {
        public static void DebugPrint(bool showMsgs,string msg)
        {
            if (showMsgs)
                CrestronConsole.PrintLine(msg);
            
        }
    }
}

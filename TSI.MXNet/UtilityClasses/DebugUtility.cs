using Crestron.SimplSharp;

namespace TSI.UtilityClasses
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

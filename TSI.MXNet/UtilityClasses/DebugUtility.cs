using Crestron.SimplSharp;

namespace TSI.UtilityClasses
{
    public static class DebugUtility
    {
        public static int DebugLevel = 0;
        public static void DebugPrint(bool showMsgs,string msg)
        {
            if (showMsgs)
                CrestronConsole.PrintLine(msg);
        }

        public static void DebugPrint(string msg)
        {
            if (DebugLevel == 1)
                CrestronConsole.PrintLine(msg);                
        }

        public static void SetDebugLevel(int level)
        {
            DebugLevel = level;
            CrestronConsole.PrintLine($"*******************\n");
            CrestronConsole.PrintLine($"**** DEBUG LEVEL SET: {level} ****\n");
            CrestronConsole.PrintLine($"*******************\n");
        }
    }
}

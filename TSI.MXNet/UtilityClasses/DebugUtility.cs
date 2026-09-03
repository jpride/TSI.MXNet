using System.Net;
using Crestron.SimplSharp;

namespace TSI.UtilityClasses
{
    public static class DebugUtility
    {
        private static int  _debugLevel;
        private static bool _debugEnabled;
        private static string _instanceName;

        public static string InstanceName
        {
            get { return _instanceName; }
            set { _instanceName = value; }
        }

        public enum DebugLevels
        {
            OFF             = 0,
            NOTICE          = 1,
            WARN            = 2,
            ERROR           = 3,
            FATAL           = 4
        }

        public static void DebugPrint(bool showMsgs,string msg)
        {
            if (showMsgs)
                CrestronConsole.PrintLine(msg);
        }

        public static void DebugPrint(string msg, string instanceName, DebugLevels debuglevel)
        {
            string errmsg = string.Empty;

            if (_debugEnabled)
            {
                switch (debuglevel)
                {
                    case DebugLevels.OFF:
                        errmsg = $"[LOW_PRIORITY]:[{instanceName}] - {msg}";
                        break;
                    case DebugLevels.NOTICE:
                        errmsg = $"[NOTICE]:[{instanceName}] - {msg}";
                        ErrorLog.Notice(errmsg);
                        break;
                    case DebugLevels.WARN:
                        errmsg = $"[WARN]:[{instanceName}] - {msg}";
                        ErrorLog.Warn(errmsg);
                        break;
                    case DebugLevels.ERROR:
                        errmsg = $"[ERROR]:[{instanceName}] - {msg}";
                        ErrorLog.Error(errmsg);
                        break;
                    case DebugLevels.FATAL:
                        errmsg = $"[FATAL]:[{instanceName}] - {msg}";
                        ErrorLog.Error(errmsg);
                        break;
                }

                if (debuglevel != DebugLevels.OFF)
                    CrestronConsole.PrintLine($"{errmsg}");
            }

        }

        public static void DebugPrint(string msg, DebugLevels debuglevel)
        {
            string errmsg = string.Empty;

            if (_debugEnabled)
            {
                switch (debuglevel)
                {
                    case DebugLevels.OFF:
                        errmsg = $"[LOW_PRIORITY]:[{InstanceName}] - {msg}";
                        break;
                    case DebugLevels.NOTICE:
                        errmsg = $"[NOTICE]:[{InstanceName}] - {msg}";
                        ErrorLog.Notice(errmsg);
                        break;
                    case DebugLevels.WARN:
                        errmsg = $"[WARN]:[{InstanceName}] - {msg}";
                        ErrorLog.Warn(errmsg);
                        break;
                    case DebugLevels.ERROR:
                        errmsg = $"[ERROR]:[{InstanceName}] - {msg}";
                        ErrorLog.Error(errmsg);
                        break;
                    case DebugLevels.FATAL:
                        errmsg = $"[FATAL]:[{InstanceName}] - {msg}";
                        ErrorLog.Error(errmsg);
                        break;
                }

                if (debuglevel != DebugLevels.OFF)
                    CrestronConsole.PrintLine($"{errmsg}");
            }

        }

        public static void SetDebugLevel(ushort level)
        {
            _debugLevel = level;
            CrestronConsole.PrintLine($"*******************\n");
            CrestronConsole.PrintLine($"**** {InstanceName} DEBUG LEVEL SET: {level} ****\n");
            CrestronConsole.PrintLine($"*******************\n");
        }

        public static void SetDebugState(bool state)
        {
            _debugEnabled = state;

            if (state)
            {
                CrestronConsole.PrintLine($"*******************\n");
                CrestronConsole.PrintLine($"**** {InstanceName} DEBUG ENABLED ****\n");
                CrestronConsole.PrintLine($"*******************\n");
            }
            else
            {
                CrestronConsole.PrintLine($"*******************\n");
                CrestronConsole.PrintLine($"**** {InstanceName} DEBUG DISABLED ****\n");
                CrestronConsole.PrintLine($"*******************\n");
            }
        }
    }
}


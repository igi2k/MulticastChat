using System;
using System.Runtime.InteropServices;

namespace MulticastChat
{
    enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    class SafetySwitch
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(CtrlTypeHandler handler, bool add);

        public delegate bool CtrlTypeHandler(CtrlType sig);
        private static readonly object startLock = new object();
        static CtrlTypeHandler _handler;

        public static void Enable(CtrlTypeHandler handler)
        {

            lock (startLock)
            {
                if (_handler != null){
                    return;
                }
                _handler += handler;
                SetConsoleCtrlHandler(_handler, true);
            }
        }
    }
}

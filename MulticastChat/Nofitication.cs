using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using MulticastChat.Properties;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MulticastChat
{
    class Notification : IDisposable
    {
        NotifyIcon trayNotify;
        Icon mainIcon;
        Icon[] message;
        Thread messageNotify;

        public event EventHandler Click
        {
            add { trayNotify.Click += value; }
            remove { trayNotify.Click -= value; }
        }

        AutoResetEvent reset = new AutoResetEvent(false);
        AutoResetEvent endFlash = new AutoResetEvent(false);

        public Notification(string text = null)
        {
            mainIcon = Resources.main;
            message = new Icon[]{
               Resources.message,
               Resources.empty
            };

            if (text == null)
            {
                AssemblyName name = Assembly.GetExecutingAssembly().GetName();
                text = string.Format("{0} v{1}", name.Name, name.Version.ToString());
            }

            trayNotify = new NotifyIcon();
            trayNotify.Icon = mainIcon;
            trayNotify.Text = text;
            messageNotify = new Thread(new ThreadStart(MessageNotify));
            trayNotify.Visible = true;
        }

        public void BeginIconFlash()
        {
            if (!messageNotify.IsAlive)
            {
                messageNotify.Start();
            }
            else if (!loop)
            {
                reset.Set();
            }
        }

        public void EndIconFlash()
        {
            if (loop && messageNotify.IsAlive)
            {
                loop = false;
                endFlash.Set();
            }
        }

        public void TrayNotify(string text, string title = null, ToolTipIcon icon = ToolTipIcon.None)
        {
            trayNotify.ShowBalloonTip(1000, title, text, icon);
        }

        bool loop = true;
        void MessageNotify()
        {
            do
            {
                {
                    int size = message.Length;
                    int i = 0;
                    do
                    {
                        trayNotify.Icon = message[i];
                        i = (i + 1) % size;
                        endFlash.WaitOne(500);
                    } while (loop);
                }
                trayNotify.Icon = mainIcon;
                reset.WaitOne();
                loop = true;
            } while (true);
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        public void MainWindowFocus()
        {
            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
            SetForegroundWindow(hWnd);
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, ShowWindowCommand.Restore);
            }
        }

        public void Dispose()
        {
            messageNotify.Abort();
            trayNotify.Dispose();
        }

        enum ShowWindowCommand : int
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>      
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
            /// the window is not actived.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
            /// that owns the window is not responding. This flag should only be
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }
    }
}
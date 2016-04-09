using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using MulticastChat.Properties;
using System.Reflection;

namespace MulticastChat
{
    class NotifyIconNotification : INotification
    {
        readonly NotifyIcon _trayNotify;
        readonly Icon _mainIcon;
        readonly Icon[] _message;
        readonly Thread _messageNotify;
        
        public IToolTip ToolTip { get; set; }
        bool _loop = true;

        public event EventHandler Click
        {
            add { _trayNotify.Click += value; }
            remove { _trayNotify.Click -= value; }
        }

        AutoResetEvent _reset = new AutoResetEvent(false);
        AutoResetEvent _endFlash = new AutoResetEvent(false);

        public NotifyIconNotification(string text = null)
        {
            _mainIcon = Resources.main;
            _message = new Icon[]{
               Resources.message,
               Resources.empty
            };

            if (text == null)
            {
                var name = Assembly.GetExecutingAssembly().GetName();
                text = string.Format("{0} v{1}", name.Name, name.Version.ToString());
            }

            _trayNotify = new NotifyIcon { Icon = _mainIcon, Text = text };
            _messageNotify = new Thread(new ThreadStart(MessageNotify));
            _trayNotify.Visible = true;
            
            ToolTip = new DefaultNotifyTip(_trayNotify);
        }

        public void BeginIconFlash()
        {
            if (!_messageNotify.IsAlive)
            {
                _messageNotify.Start();
            }
            else if (!_loop)
            {
                _reset.Set();
            }
        }

        public void EndIconFlash()
        {
            if (!_loop || !_messageNotify.IsAlive)
            {
                return;
            }
            _loop = false;
            _endFlash.Set();
        }
        
        void MessageNotify()
        {
            do
            {
                var size = _message.Length;
                var i = 0;
                do {
                    _trayNotify.Icon = _message[i];
                    i = (i + 1) % size;
                    _endFlash.WaitOne(500);
                } while(_loop);
                
                _trayNotify.Icon = _mainIcon;
                _reset.WaitOne();
                _loop = true;
            } while (true);
        }

        public void Dispose()
        {
            //TODO: change this
            _messageNotify.Abort();
            _trayNotify.Dispose();
            
            ToolTip.Dispose();
        }
        
        private class DefaultNotifyTip : IToolTip
        {
            private readonly NotifyIcon _trayNotify;
            public DefaultNotifyTip(NotifyIcon trayNotify)
            {
                _trayNotify = trayNotify;
                _trayNotify.BalloonTipShown += (s, e) => OnVisibilityChange(true);
                _trayNotify.BalloonTipClosed += (s, e) => OnVisibilityChange(false);
                _trayNotify.BalloonTipClicked += (s, e) => OnClick();
            }
            
            void OnVisibilityChange(bool visible)
            {
                if(VisibilityChanged != null)
                {
                    VisibilityChanged(this, new Visibility(visible));
                }
            }
            
            void OnClick(){
                if(Click != null)
                {
                    Click(this, EventArgs.Empty);
                }
            }
            
            public void Show(string text, string title, ToolTipIcon icon)
            {
                _trayNotify.ShowBalloonTip(1000, title, text, icon);
            }
            
            public event EventHandler<Visibility> VisibilityChanged;
            public event EventHandler Click;
            
            public void Dispose()
            {
            }
        }
        
        public void Show(string text, string title = null, ToolTipIcon icon = ToolTipIcon.None)
        {
            ToolTip.Show(text, title, icon);
        }
    }
}
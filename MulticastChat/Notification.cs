using System;
using System.Windows.Forms;

namespace MulticastChat
{
    public class Visibility : EventArgs
    {
        public bool Visible { get; private set; }
        public Visibility(bool visible)
        {
            Visible = visible;
        }
    }
    
    public interface IToolTip : IDisposable
    {
        void Show(string text, string title = null, ToolTipIcon icon = ToolTipIcon.None);
        event EventHandler<Visibility> VisibilityChanged;
        event EventHandler Click;
    }
    
    public interface INotification : IDisposable
    {
        void Show(string text, string title = null, ToolTipIcon icon = ToolTipIcon.None);
        event EventHandler Click;
        void BeginIconFlash();
        void EndIconFlash();
    }

    class DummyNotification : INotification
    {
        public event EventHandler Click;

        public void BeginIconFlash()
        {
        }

        public void Dispose()
        {
        }

        public void EndIconFlash()
        {
        }

        public void Show(string text, string title = null, ToolTipIcon icon = ToolTipIcon.None)
        {
        }
    }
}
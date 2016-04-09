using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Diagnostics;
using Multicast;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MulticastChat
{

    internal enum State
    {
        Online, Offline, Message, Discover
    }

    class Program : IDisposable
    {
        const bool _DEBUG = true;

        internal IDictionary<IPAddress, string> UserCache = new Dictionary<IPAddress, string>();
        internal IMulticastService Service;
        internal string Status;

        internal INotification Notification;

        Program(int port)
        {

            new Thread(new ThreadStart(InitNotification)).Start();
            GUI.Status(Status);
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Service = new MulticastServiceAsync("224.5.6.7", port);
            
            var dummy = new Dummy(this);
            ObserverHandler observer = new ObserverHandler(dummy.ClientData);

            Service.Observer += observer;

            while (Notification == null) { Application.DoEvents(); };

            Debug.WriteLine("Init.");
        }

        void InitNotification()
        {
            Notification = new NotifyIconNotification();
            Notification.Click += NotificationClick;
            Application.Run();
        }

        void NotificationClick(object sender, EventArgs e)
        {
            Notification.EndIconFlash();
            GUI.MainWindowFocus();
        }

        void Start()
        {
            if (!Service.Start(!_DEBUG)){
                return;
            }
            Service.Send(getData(State.Online, Environment.UserName));
            Service.Send(getData(State.Discover));
            string line;
            do
            {
                line = Console.ReadLine();
                GUI.Status(Status, "users: " + UserCache.Count);
                if (!string.IsNullOrEmpty(line))
                {
                    Service.Send(getData(State.Message, line));
                }
            } while (line != null);
        }

        static void Main(string[] args)
        {
            int port = 8000;
            if (args.Length > 0)
            {
                Int32.TryParse(args[0], out port);
            }
            Program program = new Program(port);
            SafetySwitch.Enable(program.Handler);
            program.Start();
        }

        bool Handler(CtrlType sig)
        {
            Console.WriteLine(sig);
            Dispose();
            return false;
        }

        internal static byte[] getData(State state)
        {
            return getData(state, null);
        }

        internal static byte[] getData(State state, String message)
        {
            byte[] msg = (message == null) ? new byte[0] : Encoding.UTF8.GetBytes(message);
            byte[] data = new byte[msg.Length + 1];
            data[0] = (byte)state;
            Array.Copy(msg, 0, data, 1, msg.Length);
            return data;
        }

        public void Dispose()
        {
            try
            {
                Service.Stop();
                Service.Send(getData(State.Offline, Environment.UserName));
                Notification.Dispose();             
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            
        }
    }

    class Dummy : IObserver<IClientData>
    {
        Program _program;

        public Dummy(Program program)
        {
            _program = program;
        }

        public IObserver<IClientData> ClientData()
        {
            return this;
        }

        public void OnCompleted()
        {
            Debug.WriteLine("Server shutdown event.");
        }

        public void OnError(Exception error)
        {
            Debug.WriteLine("error: " + error.Message);
        }

        public void OnNext(IClientData clientData)
        {
            byte[] data = clientData.data;
            if(data.Length <= 0){
                return;
            }
            var clientAddress = clientData.GetSource().Address;
            var message = Encoding.UTF8.GetString(data, 1, data.Length - 1);
            _program.Status = clientData.GetSource().ToString();

            var time = DateTime.Now.ToString("[HH:mm:ss] ");

            switch (data[0])
            {
                case (int)State.Message:
                    string user;
                    if (!_program.UserCache.TryGetValue(clientAddress, out user))
                    {
                        user = clientAddress.ToString();
                        _program.Service.Send(clientAddress, Program.getData(State.Discover));
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(time + user + ": " + message);
                    if(!clientData.Local)
                    {
                        _program.Notification.Show(message, user);
                        _program.Notification.BeginIconFlash();
                    }
                    break;
                case (int)State.Online:
                    _program.UserCache[clientAddress] = message;
                    if (clientData.flags == System.Net.Sockets.SocketFlags.Multicast)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(time + message + " is online");
                    }
                    break;
                case (int)State.Offline:
                    _program.UserCache.Remove(clientAddress);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(time + message + " left...");
                    break;
                case (int)State.Discover:
                    _program.Service.Send(clientAddress, Program.getData(State.Online, Environment.UserName));
                    break;
                default:
                    Console.WriteLine(clientData.GetSource().Address + "[" + (MulticastService.isLocal(clientData.GetSource().Address) ? "local" : "remote") + "]: " + message);
                    break;
            }

            GUI.Status(_program.Status, "users: " + _program.UserCache.Count);
            Console.ResetColor();
        }
    }
}

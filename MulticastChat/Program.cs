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

        internal IDictionary<IPAddress, string> userCache = new Dictionary<IPAddress, string>();
        internal IMulticastService service;
        internal string status;

        internal Notification notification;

        Program(int port)
        {

            new Thread(new ThreadStart(InitNotification)).Start();
            GUI.Status(status);
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            service = new MulticastServiceAsync("224.5.6.7", port);
            Dummy dummy = new Dummy(this);
            ObserverHandler observer = new ObserverHandler(dummy.ClientData);

            service.Observer += observer;

            while (notification == null) { Application.DoEvents();  };
            notification.Click += new EventHandler(notification_Click);

            Debug.WriteLine("Init.");
        }

        void InitNotification()
        {
            notification = new Notification();
            Application.Run();
        }

        void notification_Click(object sender, EventArgs e)
        {
            MouseEventArgs mouse = (MouseEventArgs)e;
            notification.EndIconFlash();
            notification.MainWindowFocus();
        }

        void Start()
        {
            if (service.Start(!_DEBUG))
            {
                service.Send(getData(State.Online, Environment.UserName));
                service.Send(getData(State.Discover));
                string line;
                do
                {
                    line = Console.ReadLine();
                    GUI.Status(status, "users: " + userCache.Count);
                    if (line != null && line.Length > 0)
                    {
                        service.Send(getData(State.Message, line));
                    }
                } while (line != null);
            }
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
            service.Stop();
            service.Send(getData(State.Offline, Environment.UserName));
            notification.Dispose();
        }
    }

    class Dummy : IObserver<IClientData>
    {
        Program program;

        public Dummy(Program program)
        {
            this.program = program;
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
            if (data.Length > 0)
            {
                IPAddress clientAddress = clientData.GetSource().Address;
                string message = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                program.status = clientData.GetSource().ToString();

                string time = DateTime.Now.ToString("[HH:mm:ss] ");

                switch (data[0])
                {
                    case (int)State.Message:
                        string user;
                        if (!program.userCache.TryGetValue(clientAddress, out user))
                        {
                            user = clientAddress.ToString();
                            program.service.Send(clientAddress, Program.getData(State.Discover));
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(time + user + ": " + message);
                        program.notification.TrayNotify(message, user);
                        program.notification.BeginIconFlash();
                        break;
                    case (int)State.Online:
                        program.userCache[clientAddress] = message;
                        if (clientData.flags == System.Net.Sockets.SocketFlags.Multicast)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(time + message + " is online");
                        }
                        break;
                    case (int)State.Offline:
                        program.userCache.Remove(clientAddress);
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(time + message + " left...");
                        break;
                    case (int)State.Discover:
                        program.service.Send(clientAddress, Program.getData(State.Online, Environment.UserName));
                        break;
                    default:
                        Console.WriteLine(clientData.GetSource().Address + "[" + (MulticastService.isLocal(clientData.GetSource().Address) ? "local" : "remote") + "]: " + message);
                        break;
                }

                GUI.Status(program.status, "users: " + program.userCache.Count);
                Console.ResetColor();
            }
        }
    }
}

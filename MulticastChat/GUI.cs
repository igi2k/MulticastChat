using System;
using System.Windows.Forms;
using System.Drawing;
using System.Resources;
using System.Reflection;
using System.Threading;
using MulticastChat.Properties;

namespace MulticastChat
{
    class GUI
    {
        //TODO: use structure instead message
        public static void Status(string message, string title = null)
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            //TODO: must be relative against the screen buffer
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.Blue;
            //create ribbon
            Console.Write(new String(' ', Console.BufferWidth));

            Console.Title = (title == null) ? "" : title;

            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
            Console.SetCursorPosition(left, top);
            Console.ResetColor();
        }
    }
}

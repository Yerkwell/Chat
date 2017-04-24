using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace Chat_client
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Chat chat = new Chat("91.79.219.144", 25000);
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            Chat chat = new Chat(ipAddr, 25000);
            Application.Run(new LoginForm(chat));
            if (chat.logged)
            Application.Run(new Form1(chat));
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace Chat_client
{
    public partial class Form1 : Form
    {
        Chat chat;
        bool Scrolling = true;
        public Form1(Chat c)
        {
            this.chat = c;
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            chat.setout(this);
            chat.start_chatting();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textBox1.Text))
            {
                chat.send(new Command("say", textBox1.Text));
                textBox1.Text = "";
            }
            textBox1.Focus();
        }
        delegate void AddToListBoxCallBack(String message);
        public void addtext(String text)
        {
            if (listBox1.InvokeRequired)
            {
                AddToListBoxCallBack d = new AddToListBoxCallBack(addtext);
                Invoke(d, new object[] { text });
            }
            else
            {
                listBox1.Items.Add(text);
                listBox1.TopIndex++;
            }
        }
        delegate void RefreshListBox(String[] strings);
        public void set_userlist(String[] users)
        {
            if (listBox2.InvokeRequired)
            {
                RefreshListBox d = new RefreshListBox(set_userlist);
                Invoke(d, new object[] { users });
            }
            else
            {
                listBox2.Items.Clear();
                for (int i = 0; i < users.Length; i++)
                {
                    listBox2.Items.Add(users[i]);
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            chat.die();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                button1_Click(this, null);
                e.Handled = true;
            }
        }
    }
    public class Command
    {
        public String command;
        int p_length;
        private String _parametr;
        public String parametr
        {
            get
            {
                return _parametr;
            }
            set
            {
                _parametr = value;
                p_length = _parametr.Length;
            }
        }
        public Command() { }
        public Command(String com, String par)
        {
            command = com;
            parametr = par;
        }
        public Command(String com)
        {
            command = com;
            parametr = "";
        }
        public static Command[] form_commands(String message)
        {
            Command[] coms = new Command[0];        //В принципе, едва ли тут будет больше двух, так что сильно расширять не придётся
            int i = 0;
            while (message != "")
            {
                Array.Resize(ref coms, i + 1);
                coms[i] = new Command();
                coms[i].command = message.Substring(message.IndexOf('/') + 1, message.IndexOf('[') - 1);
                coms[i].p_length = Convert.ToInt32(message.Substring(message.IndexOf('[') + 1, message.IndexOf(']') - message.IndexOf('[') - 1));
                message = message.Substring(message.IndexOf(']') + 1);
                if (coms[i].p_length > 0)                     //Ну, есть ли там вообще параметр (/die[0] какой-нибудь...)
                {
                    coms[i].parametr = message.Substring(message.IndexOf(' ') + 1, coms[i].p_length);       //Там в начале ещё пробел останется
                    message = message.Substring(coms[i].p_length + 1);
                }
            }
            return coms;
        }
        public static Command form_command(String message)
        {
            Command com = new Command();
            com.command = message.Substring(message.IndexOf('/') + 1, message.IndexOf('[') - 1);
            com.p_length = Convert.ToInt32(message.Substring(message.IndexOf('[') + 1, message.IndexOf(']') - message.IndexOf('[') - 1));
            message = message.Substring(message.IndexOf(']') + 1);
            if (com.p_length > 0)                     //Ну, есть ли там вообще параметр (/die[0] какой-нибудь...)
            {
                com.parametr = message.Substring(message.IndexOf(' ') + 1, com.p_length);       //Там в начале ещё пробел останется
                message = message.Substring(com.p_length + 1);
            }
            return com;
        }
        public static String form_message(Command com)
        {
            return "/" + com.command + "[" + Convert.ToString(com.p_length) + "]" + ((com.p_length > 0) ? (" " + com.parametr) : "");
        }
    }
    public class Chat
    {
        public Socket work_socket;
        public bool isup;
        bool chatting;
        public bool logged;
        IPAddress server_ip;
        int server_port;
        Form1 chat_form;
        public Chat(IPAddress ip, int port)
        {
            Start f = new Start();
            Thread t = new Thread((ThreadStart)delegate
            {
                f.ShowDialog();
            });
            t.Start();
            server_ip = ip; //IPAddress.Parse(ip);
            server_port = port;
            logged = isup = chatting = false;
            try
            {
                //               IPHostEntry ipHost = Dns.GetHostEntry("localhost");
                //               IPAddress ipAddr = IPAddress.Parse("192.168.100.8");    //ipHost.AddressList[0];
                IPEndPoint ipEndPoint = new IPEndPoint(server_ip, server_port);
                work_socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                connect(ipEndPoint);
            }
            catch (Exception)
            {
                isup = false;
                f.SetText("Не удалось подключиться к серверу");
            }
            String local_ip = "";
            foreach (IPAddress ipi in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ipi.AddressFamily == AddressFamily.InterNetwork)
                    local_ip = ipi.ToString();
            }
            local_ip = local_ip.Remove(local_ip.LastIndexOf('.') + 1);            //Будем тыркаться во все, что поделать
            for (int i = 2; i < 10; i++)
            {
                if (!isup)              //Вдруг сервер тут, рядом
                    try
                    {
                        f.SetText("Поиск локального сервера");
                        server_ip = IPAddress.Parse(local_ip + i.ToString());
                        reconnect();
                    }
                    catch (SocketException)
                    {
                        isup = false;
                    }
                else
                    break;
            }
            if (!isup)
            {
                f.SetText("Не удалось подключиться к серверу");
                server_ip = ip; //IPAddress.Parse(ip);
            }
            f.closeform();
        }
        public bool login(String name, String pass)              //Процедура логина происходит в окне логина
        {
            String message;
            byte[] msg = new byte[1024];
            try
            {
                message = name + "|" + Convert.ToString(pass.GetHashCode());              //Ну вот такой разделитель
                send(new Command("log", message));
                while (true)
                {
                    work_socket.Receive(msg);
                    message = Encoding.UTF8.GetString(msg);
                    message = message.Remove(message.IndexOf('\0'));
                    Command com = Command.form_command(message);
                    switch (com.command)
                    {
                        case "OK":
                            return true;
                        case "NO":
                            return false;
                        case "down":
                            return isup = false;
                        default:
                            return false;
                    }
                }
            }
            catch(System.Net.Sockets.SocketException)
            {
                return isup = false;
            }
        }
        public void setout(Form1 form)
        {
            chat_form = form;
        }
        public void reconnect()
        {
            work_socket.Close();
            work_socket.Dispose();
            work_socket = new Socket(server_ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp); 
            //Не знаю, насколько идеологически правильно, но, чтобы заново подключиться к синхронному сокету,
            //я его просто удаляю и содзаю новый.
            try
            {
    //            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
   //             IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint ipEndPoint = new IPEndPoint(server_ip, server_port);
                connect(ipEndPoint);
            }
            catch (SocketException)
            {
                isup = false;
            }
        }
        void connect(IPEndPoint ipEndPoint)
        {
            work_socket.Connect(ipEndPoint);
            isup = true;
        }
        public void start_chatting()                //Начать чатиться!
        {
            Thread t = new Thread(waiting);
            t.Start();
            chatting = true;
        }
        ~Chat()
        {
            if (work_socket.Connected)
            {
                work_socket.Shutdown(SocketShutdown.Both);
                work_socket.Close();
            }
        }
        public void die()
        {
            send(new Command("exit"));
            isup = false;
        }
        void waiting()
        {
            String message = "";
            int bytes_rec = 0;
            while (isup)
            {
                byte[] msg = new byte[1024];
                try
                {
                    bytes_rec = work_socket.Receive(msg);
                    message = Encoding.UTF8.GetString(msg);
                    message = message.Remove(message.IndexOf('\0'));            //Там много ноликов, убираем их
                }
                catch (System.Net.Sockets.SocketException)
                {
                    message = "/down[0]";                    //Упал сервер, значит
                }
                Command[] todo = Command.form_commands(message); //Там может прийти сразу несколько команд. Что поделать, будем обрабатывать по очереди
                for (int i = 0; i < todo.Length; i++)
                {
                    switch (todo[i].command)
                    {
                        case "die":
                            {
                                isup = false;
                                break;
                            }
                        case "down":
                            {
                                if (chatting)
                                    print("Сервер отключился");
                                isup = false;
                                break;
                            }
                        case "say":
                            {
                                if (chatting)
                                    publish(todo[i].parametr);
                                break;
                            }
                        case "updlist":
                            {
                                String[] clients = todo[i].parametr.Split('|');     //Он там делится на кусочки этим разделителем
                                chat_form.set_userlist(clients);
                                break;
                            }
                    }
                }
            }
        }
        void print(String message)
        {
            chat_form.addtext(message);
        }
        void publish(String message)
        {
            if (isup)
                chat_form.addtext(message);                                                             //Вывод сообщения на экран
        }
        public void send(Command com)
        {
            if (isup)                   //Если нет подключения, то нечего и пытаться отправить
            {
                String message = Command.form_message(com);
                byte[] bytes = new byte[1024];
                byte[] msg = Encoding.UTF8.GetBytes(message);
                try
                {
                    work_socket.Send(msg);
                }
                catch (SocketException)
                {
                    if (chatting)
                        print("Невозможно отправить сообщение: нет соединения с сервером!");
                }
            }
            else
            {
                if (chatting)
                print("Невозможно отправить сообщение: нет соединения с сервером!");
            }
        }
    }
}

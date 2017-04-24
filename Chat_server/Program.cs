using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Data.OleDb;

namespace Chat_server
{
    class Program
    {
        static void Main(string[] args)
        {
            Chat chat = new Chat();
            if (chat.isup)
                chat.start();
        }
    }
    class client
    {
        public static int counter = 0;
        int id;
        public Socket socket;
        public String name;
        public client(Socket s, String login)
        {
            counter++;
            id = counter;
            socket = s;
            name = login;
        }
    }
    class InvalidCommandException: Exception
    {
        String _command;
        public String command
        {
            get
            {
                return command;
            }
        }
        InvalidCommandException(String message)
        {
            _command = message;
        }
    }
    class Command
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
            p_length = par.Length;
        }
        public Command(String com)
        {
            command = com;
            p_length = 0;
            parametr = "";
        }
        public static Command[] form_commands(String message)
        {
            Command[] coms = new Command[0];        //В принципе, едва ли тут будет больше двух, так что сильно расширять не придётся
            int i = 0;
            while (message != "")
            {
                Array.Resize(ref coms, i+1);
                coms[i] = new Command();
                coms[i] = form_command(message);
            }
            return coms;
        }
        public static Command form_command(String message)
        {
            Command com = new Command();
            try
            {
                com.command = message.Substring(message.IndexOf('/') + 1, message.IndexOf('[') - 1);
                com.p_length = Convert.ToInt32(message.Substring(message.IndexOf('[') + 1, message.IndexOf(']') - message.IndexOf('[') - 1));
                message = message.Substring(message.IndexOf(']') + 1);
                if (com.p_length > 0)                     //Ну, есть ли там вообще параметр (/die[0] какой-нибудь...)
                {
                    com.parametr = message.Substring(message.IndexOf(' ') + 1, com.p_length);       //Там в начале ещё пробел останется
                    message = message.Substring(com.p_length + 1);
                }
            }
            catch (InvalidCommandException ex)
            {
                com.command = "error";
                com.parametr = ex.command;
            }
            return com;
        }
        public static String form_message(Command com)
        {
            return "/" + com.command + "[" + Convert.ToString(com.p_length) + "]" + ((com.p_length > 0) ? (" " + com.parametr) : "");
        }
    }
    class Chat
    {
        StreamWriter log_file;
        Socket listening_socket;
        List<client> clients;
        List<String> log;
        public bool isup;
        object tolock = new object();
        public Chat()
        {
            log = new List<String>();
            log_file = new StreamWriter("log.txt", true);
            clients = new List<client>();
            try
            {
                            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
                            IPAddress ipAddr = ipHost.AddressList[0];
                //           IPAddress ipAddr = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0]; //ipHost.AddressList[0];
                //IPAddress ipAddr = IPAddress.Parse("192.168.100.6");//IPAddress.Parse("127.0.0.1");
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 25000);
                listening_socket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listening_socket.Bind(ipEndPoint);
                isup = true;
            }
            catch (SocketException)
            {
                logit("The server is not up! =(");
                isup = false;
            }
        }
        ~Chat()
        {
            logit("The server is down");
            send_toall(new Command("down"));                    //Кончился сервер
            SaveLog();
        }
        public void start()               //Начало работы сервера
        {
            listening_socket.Listen(10);
            isup = true;
            logit("The server is up! =)");
            listen();
        }
        void logit(String message)
        {
            String mes = DateTime.Now.ToString() + ": " + message;
            log.Add(mes);
            log_file.WriteLine(mes);
            Console.WriteLine(message);
        }
        void listen()
        {
            while (isup)
            {
                try
                {
                    Socket cl = listening_socket.Accept();
                    Thread t = new Thread(newer);
                    t.Start(cl);        //Начало общения с новым клиентом (знакомство там и всё прочее)   
                }
                catch(SocketException)
                {
                }
            }
        }                   //Прослушка сокета
        bool toallow(String name, String pass)           //Определение, пускать или не пускать пользователя в чат
        {
            bool allow = true;
            lock (tolock)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].name != name)
                        continue;
                    else                                    //Если находим клиента с таким именем
                    {
                        allow = false;              //То не пускаем
                        break;              //А дальше проверять уже нет смысла
                    }
                }
            }
            if (allow)              //Если такого ещё нет, ищем по базе
            {
                OleDbConnection o = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=D:\Tmp\users.mdb");
                o.Open();
                OleDbCommand command = new OleDbCommand("SELECT * FROM users WHERE l = '" + name + "' AND p = '" + pass + "'", o);
                OleDbDataReader reader = command.ExecuteReader();           //Получаем результат
                allow = reader.Read();      //Если что-нибудь нашлось, то ок
                o.Close();
            }
            return allow;
        }
        void newer(object param)
        {
            Socket cl = (Socket)param;              //Сокет подключившегося клиента
            client c = null;                //Проще заранее создать ему местечко
            bool allow = false;
            bool repeat = false;                //Ожидание повторных запросов на авторизацию
            try
            {
                do
                {
                    String message = receive(cl);         //Получаем команду
                    repeat = true;
                    Command[] todo = Command.form_commands(message);            //Преобразуем полученное сообщение в команды
                    for (int i = 0; i < todo.Length; i++)
                    {
                        switch (todo[i].command)
                        {
                            case "no":             //Отказался логиниться
                                {
                                    repeat = false;
                                    cl.Shutdown(SocketShutdown.Both);
                                    cl.Close();
                                    cl.Dispose();
                                    break;
                                }
                            case "log":
                                {
                                    repeat = false;
                                    String[] cl_params = todo[i].parametr.Split('|');  //Делим на логин и пароль
                                    allow = toallow(cl_params[0], cl_params[1]);
                                    if (allow)                      //Ну, и, по идее, мы пускаем, либо нет
                                    {
                                        c = new client(cl, cl_params[0]);
                                        send(c, new Command("OK"));
                                        clients.Add(c);
                                        send_toall(new Command("say", (c.name + " вошёл в чат")));
                                        send_toall(form_userlist());                        //Рассылаем всем новый список контактов
                                        Talk(c);                    //Пошло общение
                                    }
                                    else
                                    {
                                        repeat = true;
                                        send(cl, new Command("NO"));
                                    }
                                    break;
                                }
                            case "toreg":
                                {
                                    String name = todo[i].parametr;
                                    OleDbConnection o = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=D:\Tmp\users.mdb");
                                    o.Open();
                                    OleDbCommand com = new OleDbCommand("SELECT * FROM users WHERE l = '" + todo[i].parametr + "'", o);
                                    OleDbDataReader reader = com.ExecuteReader();           //Получаем результат
                                    allow = !reader.Read();      //Если что-нибудь нашлось, то не ок
                                    o.Close();
                                    if (allow)
                                        send(cl, new Command("OK"));
                                    else
                                        send(cl, new Command("NO"));
                                    break;
                                }
                            case "reg":
                                {
                                    String[] fields = todo[i].parametr.Split('|');
                                    OleDbConnection o = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=D:\Tmp\users.mdb");
                                    o.Open();
                                    String commm = "INSERT INTO users (l, p, g) VALUES ('" + fields[0] + "','" + fields[1] + "','" + fields[2] + "')";
                                    OleDbCommand com = new OleDbCommand(commm, o);
                                    com.ExecuteNonQuery();           //Заносим в БД
                                    o.Close();
                                    break;
                                }
                        }
                    }
                }
                while (repeat);
            }
            catch (SocketException)         //Он мог по ходу там отвалиться где-нибудь
            {
                if (allow)          //Если мы его уже записали, то надо выкинуть
                {
                    Kill(c);
                }
                else                //Иначе просто очищаем сокет
                {
                    cl.Shutdown(SocketShutdown.Both);
                    cl.Close();
                    cl.Dispose();
                }
            }
        }               //Начало работы с новым клиентом
        String receive(Socket s)                   //Получить сообщение от сокета
        {
            byte[] msg = new byte[1024];
            s.Receive(msg);
            String message = Encoding.UTF8.GetString(msg);
            message = message.Remove(message.IndexOf('\0'));        //Там много нуль-терминаторов под конец
            return message;
        }
        void send(Socket s, Command com)                           //Команда сокету
        {
            String message = Command.form_message(com);
            logit("serv to Unknown: " + message);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            s.Send(msg);
        }
        void send(client cl, Command com)                           //Команда клиенту
        {
            String message = Command.form_message(com);
            logit("serv to " + cl.name + ": " + message);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            cl.socket.Send(msg);
        }
        void send_toall(Command com)                                //Команда всем
        {
            String message = Command.form_message(com);
            logit("serv to everyone: " + message);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            for (int i=0; i<clients.Count; i++)
                clients[i].socket.Send(msg);
        }
 /*       void send_mes(String author, client cl, String message)      //Послать сообщение от автора клиенту
        {
            message = "/say " + author + ": " + message;
            logit(message + " : " + cl.name);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            cl.socket.Send(msg);
        }
        void send_serv(client cl, String message)           //Послать сервисное сообщение клиенту
        {
            logit(message + " : " + cl.name);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            cl.socket.Send(msg);
        }
        void send_serv(Socket s, String message)
        {
            logit(message + " : Unknown");
            byte[] msg = Encoding.UTF8.GetBytes(message);
            s.Send(msg);
        }           //Послать сервисное сообщение на сокет (если он ещё не стал клиентом)
        void sendtoall_mes(String author, String message)       //Послать сообщение от автора всем
        {
            message = "/say " + author + ": " + message;
            logit(message);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            for (int i=0; i<clients.Count; i++)
            clients[i].socket.Send(msg);

        }
        void sendtoall_serv(String message)                 //Послать сервисное сообщение всем
        {
            logit(message);
            byte[] msg = Encoding.UTF8.GetBytes(message);
            for (int i=0; i<clients.Count; i++)
                clients[i].socket.Send(msg);
        }*/
        void disconnect(client cl)
        {
            cl.socket.Shutdown(SocketShutdown.Both);
            cl.socket.Close();
            clients.Remove(cl);
        }
        void Talk(object obj)                           //Общение (получение сообщений от клиента)
        {
            String message = "";
            client cl = (client)obj;
            byte[] bytes = new byte[1024];                  //Сюда придёт сообщение
            int bytes_rec = 0;
            bool repeat = true;
            try
            {
                while (repeat)
                {
                    bytes_rec = cl.socket.Receive(bytes);
                    message = Encoding.UTF8.GetString(bytes, 0, bytes_rec);
                    Command[] todo = Command.form_commands(message);
                    for (int i = 0; i < todo.Length; i++)
                    {
                        switch (todo[i].command)
                        {
                            case "exit":
                                {
                                    repeat = false;
                                    Kill(cl);
                                    break;
                                }
                            case "say":
                                {
                                    repeat = true;
                                    todo[i].parametr = cl.name + ": " + todo[i].parametr;           //Дописываем имя отправителя
                                    send_toall(todo[i]);
                                    break;
                                }
                        }
                    }
                }
            }
            catch (SocketException)                             //Если вдруг чего, просто убиваем клиента
            {
                Kill(cl);
            }
        }
        Command form_userlist()
        {
            String userlist = "";
            for (int i = 0; i < clients.Count; i++)
            {
                userlist += clients[i].name+"|";
            }
            if (userlist != "")
                userlist = userlist.Remove(userlist.Length - 1);
            Command c_userlist = new Command("updlist", userlist);
            return c_userlist;
        }
        void Kill(client cl)               //Убиваем клиента
        {
            try                         //Он мог уже сам отвалиться. Ну и ладушки тогда
            {
                send(cl, new Command("die"));
            }
            catch (SocketException)
            {
            }
            lock (tolock)
            {
                clients.Remove(cl);                                 //Удаляем клиента из списка
            }
            send_toall(new Command("say", (cl.name + " вышел из чата")));
            send_toall(form_userlist());
            cl.socket.Shutdown(SocketShutdown.Both);
            cl.socket.Close();
        }
        void SaveLog()
        {
            logit("closing_logfile");
        }
    }
}
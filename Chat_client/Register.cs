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

namespace Chat_client
{
    public partial class Register : Form
    {
        Socket s;
        public Register(Socket s)
        {
            this.s = s;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ValidName();
        }
        bool ValidName()
        {
            if (!String.IsNullOrWhiteSpace(login.Text))
            {
                Command com = new Command("toreg", login.Text);
                s.Send(Encoding.UTF8.GetBytes(Command.form_message(com)));
                byte[] msg = new byte[1024];
                s.Receive(msg);
                String message = Encoding.UTF8.GetString(msg);
                message = message.Remove(message.IndexOf('\0'));
                com = Command.form_command(message);
                if (com.command == "NO")
                {
                    label5.Text = "Имя уже занято";
                    return false;
                }
                else
                {
                    label5.Text = "Имя свободно";
                    return true;
                }
            }
            else
            {
                label5.Text = "Имя не введено";
                return false;
            }
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (pass2.Text != pass.Text)
                label5.Text = "Пароли не совпадают";
            else
                label5.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (pass2.Text == pass.Text && ValidName())
            {
                Command com = new Command("reg", login.Text + "|" + Convert.ToString((pass.Text).GetHashCode()) + "|" + gender.Text);
                s.Send(Encoding.UTF8.GetBytes(Command.form_message(com)));
            }
            Close();
        }
    }
}

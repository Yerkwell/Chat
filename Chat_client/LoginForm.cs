using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_client
{
    public partial class LoginForm : Form
    {
        Chat chat;
        public LoginForm(Chat c)
        {
            chat = c;
            InitializeComponent();
        }
        bool ValidLogin()                   //Проверка логина на валидность
        {
            return !String.IsNullOrWhiteSpace(loginBox.Text);
        }
        bool ValidPass()
        {
            return !String.IsNullOrWhiteSpace(passBox.Text);
        }
        private void enterButton_Click(object sender, EventArgs e)
        {
            if (chat.isup && ValidLogin() && ValidPass())
            {
                reconnect_button.Visible = false;
                if (chat.login(loginBox.Text, passBox.Text))            //Пытаемся залогиниться
                {
                    chat.logged = true;
                    Close();
                }
                else
                {
                    Info.Text = "Неверное имя и/или пароль";
                }
            }
            else if (!ValidLogin())
            {
                reconnect_button.Visible = false;
                Info.Text = "Некорректное имя";
            }
            else if (!ValidPass())
            {
                reconnect_button.Visible = false;
                Info.Text = "Некорректный пароль";
            }
            if (!chat.isup)
            {
                Info.Text = "Отсутствует соединение с сервером";
                reconnect_button.Visible = true;
            }
        }

        private void loginBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                enterButton_Click(this, null);
                e.Handled = true;
            }
        }
        private void passBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                enterButton_Click(this, null);
                e.Handled = true;
            }
        }
        private void checkBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                enterButton_Click(this, null);
                e.Handled = true;
            }
        }

        private void LoginForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (chat.isup && !chat.logged)          //Если он к серверу подключен, но не залогинился
                chat.send(new Command("no"));
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            if (!chat.isup)
            {
                Info.Text = "Не удалось подключиться к серверу";
                reconnect_button.Visible = true;
            }
        }

        private void reconnect_button_Click(object sender, EventArgs e)
        {
            chat.reconnect();
            if (chat.isup)
            {
                Info.Text = "";
                reconnect_button.Visible = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                passBox.PasswordChar = (Char)0;
            else
                passBox.PasswordChar = 'o';
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Register r = new Register(chat.work_socket);
            r.ShowDialog();
        }

    }
}

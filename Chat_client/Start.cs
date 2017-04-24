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
    public partial class Start : Form
    {
        public Start()
        {
            InitializeComponent();
        }

        private void Start_Load(object sender, EventArgs e)
        {

        }
        delegate void SetTextCallBack(String text);
        public void SetText(String text)                         //Это всё делается, чтобы не вылетало ошибки, что значение изменяется
        {                                                   //не в том потоке, в котором создан элемент
            if (label1.InvokeRequired)
            {
                SetTextCallBack d = new SetTextCallBack(SetText);
                Invoke(d, new object[] { text });
            }
            else
            {
                label1.Text = text;
            }
        }
        delegate void KillForm();
        public void closeform()
        {
            if (this.InvokeRequired)
            {
                KillForm d = new KillForm(closeform);
                Invoke(d);
            }
            else
            {
                Close();
            }
        }
    }
}

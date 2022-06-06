using System;
using System.IO;
using System.Windows.Forms;

namespace HNS_Alarm
{
    public partial class apikeyForm : Form
    {
        string path = "";
        public apikeyForm(String Enviromentpath)
        {
            InitializeComponent();
            path = Enviromentpath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("You need to add an API key");
                return;
            }
            string APIenc = StringCipher.Encrypt(textBox1.Text, Environment.UserName);
            StreamWriter streamw1 = new StreamWriter(path + "\\API.key");
            streamw1.Write(APIenc);
            streamw1.Dispose();
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To get your API key, open Bob and go to settings via the cog icon.\nGo to \"Network & Connection\"\nCopy your API key and paste it here.", "How to find API Key");
        }
    }
}

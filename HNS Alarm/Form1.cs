using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace HNS_Alarm
{
    public partial class Form1 : Form
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\HNSAlarm";
        string apikey = "";
        int blocks = 2000;

        string expname = "";
        int expblock = 0;
        public Form1()
        {
            InitializeComponent();
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            sync();
        }
        private void sync()
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;

            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine("python3 \"" + Environment.CurrentDirectory + "\\main.py\" " + apikey + " 12039 " + blocks);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            string output = cmd.StandardOutput.ReadToEnd();
            if (!output.Contains("HSD not running") && output.Contains("Next"))
            {
                output = output.Substring(output.IndexOf("Next"));
                string[] lines = output.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                StatusLabel.Text = "";
                int i = 0;
                foreach (string line in lines)
                {
                    if (i <= 3)
                    {
                        StatusLabel.Text += line + "\n";
                    }
                    switch (i)
                    {
                        case 0:
                            expname = line.Substring(20);

                            break;
                        case 1:
                            expblock = int.Parse(line.Substring(13));
                            break;
                        default:
                            break;
                    }
                    i += 1;
                }

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!File.Exists(path + "\\API.key"))
            {

                apikeyForm apikey = new apikeyForm(path);
                apikey.ShowDialog();
            }
            StreamReader streamr1 = new StreamReader(path + "\\API.key");
            string apienc = streamr1.ReadLine();
            streamr1.Dispose();
            apikey = StringCipher.Decrypt(apienc, Environment.UserName);
            if (File.Exists(path + "\\settings.txt"))
            {
                StreamReader streamr3 = new StreamReader(path + "\\settings.txt");
                streamr3.ReadLine();
                blocks = int.Parse(streamr3.ReadLine());
                synctimer.Interval = int.Parse(streamr3.ReadLine());
                expiretimer.Interval = int.Parse(streamr3.ReadLine());
                streamr1.Dispose();
            }
            else
            {
                StreamWriter streamw2 = new StreamWriter(path + "\\settings.txt");
                streamw2.WriteLine("Leave in the same format (Blocks till alarm, inverval (in ms) to get names from bob (after the first run through),interval (in ms) to check for name expirations (after the first run through)");
                streamw2.WriteLine("2000");
                streamw2.WriteLine("1000000000");
                streamw2.WriteLine("1000000");
                streamw2.Dispose();
            }

            if (File.Exists(path + "\\names.txt"))
            {
                StreamReader streamr3 = new StreamReader(path + "\\names.txt");
                expname=streamr3.ReadLine();
                expblock = int.Parse(streamr3.ReadLine());
                streamr3.Close();
                label1.Text = "Name: " + expname + "\nExpires: "+expblock.ToString();
            }

            sync();
            checkexp();

        }

        private void synctimer_Tick(object sender, EventArgs e)
        {
            sync();
        }

        private void expiretimer_Tick(object sender, EventArgs e)
        {
            checkexp();
        }

        private void checkexp()
        {
            if (expblock > 0)
            {
                string apiinfo = ExecuteCurl("curl 'https://api.handshakeapi.com/hsd'");
                int index = apiinfo.IndexOf("height");
                apiinfo = apiinfo.Substring(index);
                string[] split = apiinfo.Split(new Char[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                int height = int.Parse(split[1]);
                if (height > expblock - blocks)
                {
                    //MessageBox.Show(expname + " will be expiring in " + (expblock - height).ToString() + " blocks.\nPlease renew it to prevent losing it.", "HNS Alarm");

                    notifyIcon1.ShowBalloonTip(100000, "HNS Alarm", expname + " will be expiring in " + (expblock - height).ToString() + " blocks.\nPlease renew it to prevent losing it.", ToolTipIcon.Warning);
                    expiretimer.Stop();
                    pausedToolStripMenuItem.Checked = true;
                }
            }
        }

        public static string ExecuteCurl(string curlCommand, int timeoutInSeconds = 60)
        {
            if (string.IsNullOrEmpty(curlCommand))
                return "";

            curlCommand = curlCommand.Trim();

            // remove the curl keworkd
            if (curlCommand.StartsWith("curl"))
            {
                curlCommand = curlCommand.Substring("curl".Length).Trim();
            }

            // this code only works on windows 10 or higher
            {

                curlCommand = curlCommand.Replace("--compressed", "");

                // windows 10 should contain this file
                var fullPath = System.IO.Path.Combine(Environment.SystemDirectory, "curl.exe");

                if (System.IO.File.Exists(fullPath) == false)
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    throw new Exception("Windows 10 or higher is required to run this application");
                }

                // on windows ' are not supported. For example: curl 'http://ublux.com' does not work and it needs to be replaced to curl "http://ublux.com"
                List<string> parameters = new List<string>();


                // separate parameters to escape quotes
                try
                {
                    Queue<char> q = new Queue<char>();

                    foreach (var c in curlCommand.ToCharArray())
                    {
                        q.Enqueue(c);
                    }

                    StringBuilder currentParameter = new StringBuilder();

                    void insertParameter()
                    {
                        var temp = currentParameter.ToString().Trim();
                        if (string.IsNullOrEmpty(temp) == false)
                        {
                            parameters.Add(temp);
                        }

                        currentParameter.Clear();
                    }

                    while (true)
                    {
                        if (q.Count == 0)
                        {
                            insertParameter();
                            break;
                        }

                        char x = q.Dequeue();

                        if (x == '\'')
                        {
                            insertParameter();

                            // add until we find last '
                            while (true)
                            {
                                x = q.Dequeue();

                                // if next 2 characetrs are \' 
                                if (x == '\\' && q.Count > 0 && q.Peek() == '\'')
                                {
                                    currentParameter.Append('\'');
                                    q.Dequeue();
                                    continue;
                                }

                                if (x == '\'')
                                {
                                    insertParameter();
                                    break;
                                }

                                currentParameter.Append(x);
                            }
                        }
                        else if (x == '"')
                        {
                            insertParameter();

                            // add until we find last "
                            while (true)
                            {
                                x = q.Dequeue();

                                // if next 2 characetrs are \"
                                if (x == '\\' && q.Count > 0 && q.Peek() == '"')
                                {
                                    currentParameter.Append('"');
                                    q.Dequeue();
                                    continue;
                                }

                                if (x == '"')
                                {
                                    insertParameter();
                                    break;
                                }

                                currentParameter.Append(x);
                            }
                        }
                        else
                        {
                            currentParameter.Append(x);
                        }
                    }
                }
                catch
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    throw new Exception("Invalid curl command");
                }

                StringBuilder finalCommand = new StringBuilder();

                foreach (var p in parameters)
                {
                    if (p.StartsWith("-"))
                    {
                        finalCommand.Append(p);
                        finalCommand.Append(" ");
                        continue;
                    }

                    var temp = p;

                    if (temp.Contains("\""))
                    {
                        temp = temp.Replace("\"", "\\\"");
                    }
                    if (temp.Contains("'"))
                    {
                        temp = temp.Replace("'", "\\'");
                    }

                    finalCommand.Append($"\"{temp}\"");
                    finalCommand.Append(" ");
                }


                using (var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "curl.exe",
                        Arguments = finalCommand.ToString(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.SystemDirectory
                    }
                })
                {
                    proc.Start();

                    proc.WaitForExit(timeoutInSeconds * 1000);

                    return proc.StandardOutput.ReadToEnd();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(path + "\\names.txt"))
            {
                File.Delete(path + "\\names.txt");
            }
            StreamWriter streamw1 = new StreamWriter(path + "\\names.txt");
            streamw1.WriteLine(expname);
            streamw1.WriteLine(expblock.ToString());
            streamw1.Close();
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void pausedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pausedToolStripMenuItem.Checked)
            {
                pausedToolStripMenuItem.Checked = false;
                expiretimer.Start();
            }
            else
            {
                pausedToolStripMenuItem.Checked = true;
                expiretimer.Stop();
            }
        }
    }
    public static class StringCipher
    {
        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public static string Encrypt(string plainText, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate256BitsOfRandomEntropy();
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}

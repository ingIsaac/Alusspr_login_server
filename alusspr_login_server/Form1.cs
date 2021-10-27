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
using System.Timers;
using System.Diagnostics;
using ConsoleControl;

namespace alusspr_login_server
{
    public partial class Form1 : Form
    {
        private byte[] send_data = new byte[1];
        private byte[] data = new byte[1024];
        private Socket s_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private List<object[]> client_sockets = new List<object[]>();
        private bool close = true;
        private ConsoleControl.ConsoleControl console;
        int port = 6400;

        public Form1()
        {
            InitializeComponent();         
            this.Text = Text + " - " + getSoftwareVersion();
            notifyIcon1.BalloonTipClicked += NotifyIcon1_BalloonTipClicked;
            notifyIcon1.DoubleClick += NotifyIcon1_DoubleClick;
            this.FormClosing += Form1_FormClosing;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            //Console Setup
            console = new ConsoleControl.ConsoleControl();
            console.Dock = DockStyle.Fill;
            console.Font = new Font("Microsoft Sans Serif", 10);
            panel1.Controls.Add(console);
        }

        private void sendConsoleOutputValue(string value, Color color)
        {
            console.WriteOutput(value, color);
            console.InternalRichTextBox.ScrollToCaret();
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Result != null)
            {
                string r = e.Result.ToString();
                sendConsoleOutputValue(r, Color.White);
            }
            s_socket.Bind(new IPEndPoint(IPAddress.Any, port));
            s_socket.Listen(50);
            s_socket.BeginAccept(new AsyncCallback(acceptCallBack), null);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Visible = true;
            this.Hide();
            e.Cancel = close;
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
        }

        private void NotifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            if (p.Length > 1)
            {
                MessageBox.Show("[Error] solo puede existir una instancia de este programa.", "AlussPR Login Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            InitServer();
        }

        private void InitServer()
        {
            System.Timers.Timer timer = new System.Timers.Timer(10000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            sendConsoleOutputValue("Iniciando Servidor...\n", Color.White);
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Socket socket;
            foreach (object[] x in client_sockets)
            {
                socket = (Socket)x[0];
                try
                {
                    send_data = Encoding.Default.GetBytes("s");
                    socket.BeginSend(send_data, 0, send_data.Length, SocketFlags.None, new AsyncCallback(sendCallBack), socket);
                }
                catch (Exception)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    client_sockets.Remove(x);
                    deleteItem(x[1].ToString());
                    label1.Text = "Usuarios conectados: " + listView1.Items.Count;
                    log(x[1].ToString(), false);
                }
            }
        }

        private void sendCallBack(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private void deleteItem(string item)
        {
            foreach(ListViewItem x in listView1.Items)
            {
                if(x.Text == item)
                {
                    listView1.Items.Remove(x);
                }
            }
        }

        private void acceptCallBack(IAsyncResult AR)
        {
            try
            {
                Socket socket = s_socket.EndAccept(AR);
                socket.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(reciveCallBack), socket);
                //Continue
                s_socket.BeginAccept(new AsyncCallback(acceptCallBack), null);
            }
            catch (Exception)
            {
                //Do nothing
            }
        }

        private void reciveCallBack(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            int r = socket.EndReceive(AR);
            string user = string.Empty;
            if (r > 0)
            {
                byte[] d = new byte[r];
                Array.Copy(data, d, r);
                user = Encoding.Default.GetString(d);
            }
            if (user != string.Empty)
            {
                if (client_sockets.Where(x => x[1].ToString() == user).FirstOrDefault() == null)
                {
                    client_sockets.Add(new object[] { socket, user });
                    if (listView1.InvokeRequired)
                    {
                        listView1.Invoke((MethodInvoker)delegate
                        {
                            listView1.Items.Add(user);
                        });
                    }
                    else
                    {
                        listView1.Items.Add(user);
                    }
                    notifyIcon1.BalloonTipText = user;
                    notifyIcon1.ShowBalloonTip(100);
                    label1.Text = "Usuarios conectados: " + listView1.Items.Count;
                    log(user, true);
                }
                else
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
        }

        public void log(string user, bool connected)
        {
            if (user.Length > 1)
            {
                if (connected)
                {
                    sendConsoleOutputValue("[" + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "] -Conectado: " + user + "\n", Color.FromArgb(0, 192, 0));
                }
                else
                {
                    sendConsoleOutputValue("[" + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + "] -Desconectado: " + user + "\n", Color.Red);
                }

            }                      
        }

        public string getSoftwareVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            }
            catch (Exception)
            {
                return "0.0.0.0";
            }
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            close = false;
            this.Close();
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (CheckForInternetConnection())
            {
                e.Result = "Servidor Iniciado (Puerto: " + port + ")...\n";
            }
            else
            {
                e.Result = "Servidor Iniciado (Puerto: " + port + ")(Aviso: se detectarón problemas de conexión)...\n";
            }
        }
    }
}

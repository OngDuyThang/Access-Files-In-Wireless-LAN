using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();

            LoginForm.ShowDialog();
            textBox1.Text = LoginForm.textBox1.Text;
            textBox2.Text = LoginForm.textBox2.Text;

            CheckForIllegalCrossThreadCalls = false;
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.StartPosition = FormStartPosition.CenterScreen;

                Connect();
                LoadData();
            }
            else
            {
                Close();
            }           
        }

        Login LoginForm = new Login();
        Chat ChatForm;

        private void Server_Load(object sender, EventArgs e)
        {
            button1.Text = "Starting...";
            panel1.BackColor = Color.FromArgb(100, 0, 0, 0);
        }       

        IPEndPoint IP;
        Socket server;
        List<Socket> ClientList;

        private void button1_Click(object sender, EventArgs e)
        {           
        }

        private void Connect()
        {
            ClientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32(textBox2.Text));
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            server.Bind(IP);

            Thread Listen = new Thread(() => {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        ClientList.Add(client);

                        this.Invoke((MethodInvoker)delegate
                        {
                            Client_List.Items.Add(client.RemoteEndPoint.ToString());
                        });
                        string ConnectStr = "Data Source=" + textBox1.Text + ",1433;Network Library=DBMSSOCN;Initial Catalog=DocumentsSystem;User ID=sa;Password=ongduythang;";
                        Send(client, ConnectStr);

                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32(textBox2.Text));
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }
            });
            Listen.IsBackground = true;
            Listen.Start();
        }

        private void Send(Socket client, string str)
        {
            client.Send(Serialize(str));
        }        

        private byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        private void LoadData()
        {
            using (SqlConnection cn = GetConnection())
            {
                string query = "SELECT ID, FileName, Extension FROM Documents";
                SqlDataAdapter adp = new SqlDataAdapter(query, cn);
                DataTable dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    dataGridView1.DataSource = dt;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.ShowDialog();
            textBox3.Text = dlg.FileName;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFile(textBox3.Text);
            MessageBox.Show("Đã lưu!", "Saved", MessageBoxButtons.OK);
            LoadData();
        }

        private void SaveFile(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {                
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                var fi = new FileInfo(filePath);
                string name = fi.Name;
                string extn = fi.Extension;

                string query = "INSERT INTO Documents(FileName,Data,Extension)VALUES(@name,@data,@extn)";

                using (SqlConnection cn = GetConnection())
                {
                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.Add("@name", SqlDbType.VarChar).Value = name;
                    cmd.Parameters.Add("@data", SqlDbType.VarBinary).Value = buffer;
                    cmd.Parameters.Add("@extn", SqlDbType.Char).Value = extn;
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection("Data Source=" + textBox1.Text + ",1433;Network Library=DBMSSOCN;Initial Catalog=DocumentsSystem;User ID=sa;Password=ongduythang;");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var selectedRow = dataGridView1.SelectedRows;
            foreach (var row in selectedRow)
            {
                int id = (int)((DataGridViewRow)row).Cells[0].Value;
                OpenFile(id);
            }
        }

        private void OpenFile(int id)
        {
            using (SqlConnection cn = GetConnection())
            {
                string query = "SELECT Data, FileName, Extension FROM Documents WHERE ID=@id";
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var name = reader["FileName"].ToString();
                    var data = (byte[])reader["data"];
                    var extn = reader["Extension"].ToString();
                    var newFileName = name.Replace(extn, DateTime.Now.ToString("ddMMyyyyhhmmss")) + extn;
                    File.WriteAllBytes(newFileName, data);
                    System.Diagnostics.Process.Start(newFileName);
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ChatForm = new Chat();
            ChatForm.Show();
            button5.Enabled = false;

            ChatForm.FormClosed += ChatForm_FormClosed;

            ChatForm.button1.Click += Button1_Click;
        }

        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            button5.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var selectedRow = dataGridView1.SelectedRows;
            foreach (var row in selectedRow)
            {
                int id = (int)((DataGridViewRow)row).Cells[0].Value;
                DeleteFile(id);
            }           
            LoadData();
        }

        private void DeleteFile(int id)
        {
            using (SqlConnection cn = GetConnection())
            {
                string query = "DELETE FROM Documents WHERE ID=@id";
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //==============================================CHAT FORM=======================================================//
        private void Button1_Click(object sender, EventArgs e)
        {
            if (ChatForm.textBox1.Text != "")
            {
                foreach (Socket client in ClientList)
                {
                    Send(client, ChatForm.textBox1.Text);
                }
                ChatForm.richTextBox1.Text += "Server: " + ChatForm.textBox1.Text + "\n";
                ChatForm.textBox1.Clear();
            }
        }

        private void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    string message = (string)Deserialize(data);
                    ChatForm.richTextBox1.Text += "Client " + client.RemoteEndPoint.ToString() + ": " + message + "\n";
                }
            }
            catch
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Client_List.Items.Remove(client.RemoteEndPoint.ToString());
                });
                ClientList.Remove(client);
                client.Close();             
            }
        }

        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        
    }
}

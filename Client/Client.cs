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

namespace Client
{
    public partial class Client : Form
    {
        public Client()
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
            }
            else
            {
                Close();
            }
        }

        Login LoginForm = new Login();
        Chat ChatForm;

        private void Client_Load(object sender, EventArgs e)
        {
            button1.Text = "Connecting...";
            panel1.BackColor = Color.FromArgb(100, 0, 0, 0);
        }

        IPEndPoint IP;
        Socket client;

        private string message, ConnectStr;

        private void button1_Click(object sender, EventArgs e)
        {
           
        }

        private void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse(textBox1.Text), Convert.ToInt32(textBox2.Text));
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                client.Connect(IP);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối server!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
        }

        private bool received = false;

        private void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    message = (string)Deserialize(data);

                    if (received == false)
                    {
                        ConnectStr = message;
                        textBox3.Text = ConnectStr;
                        LoadData(ConnectStr);
                        button4.Enabled = true;
                        received = true;
                    }
                    else
                    {
                        ChatForm.richTextBox1.Text += "Server: " + message + "\n";
                    }
                }
            }
            catch
            {
                client.Close();
            }
        }

        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void LoadData(string message)
        {
            using (SqlConnection cn = GetConnection(message))
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
            var selectedRow = dataGridView1.SelectedRows;
            foreach (var row in selectedRow)
            {
                int id = (int)((DataGridViewRow)row).Cells[0].Value;
                OpenFile(id);
            }
        }

        private void OpenFile(int id)
        {
            using (SqlConnection cn = GetConnection(message))
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

        private SqlConnection GetConnection(string Key)
        {
            return new SqlConnection(connectionString: Key);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            ChatForm = new Chat();
            ChatForm.Show();
            button3.Enabled = false;

            ChatForm.FormClosed += ChatForm_FormClosed;

            ChatForm.button1.Click += Button1_Click;
        }

        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            button3.Enabled = true;
        }

        //==============================================CHAT FORM=======================================================//
        private void Button1_Click(object sender, EventArgs e)
        {
            if (ChatForm.textBox1.Text != "")
            {
                Send(ChatForm.textBox1.Text);
                ChatForm.richTextBox1.Text += "Client " + client.LocalEndPoint.ToString() + ": " + ChatForm.textBox1.Text + "\n";
                ChatForm.textBox1.Clear();
            }
        }

        private void Send(string str)
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

        private void button4_Click(object sender, EventArgs e)
        {
            LoadData(ConnectStr);
        }

    }
}

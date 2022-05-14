using System;
using System.Windows.Forms;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace chatClient
{
    /* 
     * author: Jacob Peterson
     * original authored date: 05/12/2022
     */

    public partial class ChatClient : Form
    {
        TcpClient client = new TcpClient();
        NetworkStream serverStream = default(NetworkStream);
        string message = null;
        //string username = null;

        public ChatClient()
        {
            InitializeComponent();
            //Shown += ChatClient_Shown;
        }

        //private void ChatClient_Shown(object sender, EventArgs e)
        //{
        //    LoginScreen loginScreen = new LoginScreen();
        //    loginScreen.Show();

        //    username = loginScreen.Username;
        //    //using (LoginScreen loginScreen = new LoginScreen())
        //    //{
        //    //    loginScreen.Show();
        //    //    if (loginScreen.DialogResult == DialogResult.OK)
        //    //    {
        //    //        username = loginScreen.Username;
        //    //    }
        //    //}
        //}

        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                client.Connect("127.0.0.1", 8675);
                serverStream = client.GetStream();

                //once a user logs in, disable their ability to edit the username or connect again
                btnLogin.Enabled = false;
                txtUsername.Enabled = false;

                //send their username as the first communication to the server
                byte[] outgoingMessage = Encoding.Default.GetBytes(txtUsername.Text + "~");
                serverStream.Write(outgoingMessage, 0, outgoingMessage.Length);

                //start a thread to poll for messages
                Thread clientThread = new Thread(getMessage);
                clientThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnLogin.Enabled = true;
                txtUsername.Enabled = true;
            }
        }

        //upon clicking send, write out the message and clear the composition box (if there's a message to send)
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtComposeMessage.Text.Length > 0)
            {
                byte[] outStream = Encoding.Default.GetBytes(txtComposeMessage.Text + "~");
                serverStream.Write(outStream, 0, outStream.Length);
                txtComposeMessage.Clear();
            }
        }

        //constantly poll for new incoming messages
        private void getMessage()
        {
            while (true)
            {
                serverStream = client.GetStream();
                int clientBufferSize;
                byte[] inStream = new byte[65536];
                clientBufferSize = client.ReceiveBufferSize;
                serverStream.Read(inStream, 0, clientBufferSize);
                string returndata = Encoding.Default.GetString(inStream);
                message = returndata;
                displayMessage();
            }

            serverStream.Close();
        }

        //write out the currently stored message to the txtChat box
        private void displayMessage()
        {
            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(displayMessage));
            else
            {
                txtChat.AppendText(message);
                txtChat.AppendText(Environment.NewLine);
            }
        }
    }
}
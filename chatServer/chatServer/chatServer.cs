using System.Net.Sockets;
using System.Text;
using System.Net;

namespace chatServer
{
    /* 
     * author: Jacob Peterson
     * original authored date: 05/12/2022
     */

    class ChatServer
    {
        //store connectedClient information - <string username, ConnectedClient client info>
        public static Dictionary<string, ConnectedClient> connectedClients = new Dictionary<string, ConnectedClient>();

        static void Main()
        {
            //create a local TcpListener to wait for incoming connections on the local IP, port 8675
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8675);

            //TcpClient to handle new incoming connections
            TcpClient client;

            //start the server so it's ready to accept connections
            server.Start();
            Console.WriteLine("Server started...");

            //always await new connections
            while (true)
            {
                // currently treating clientInitializationString as solely a username - in the future, could expand to more data
                string clientInitializationString;
                byte[] clientInData = new byte[65536];
                client = server.AcceptTcpClient();

                Console.WriteLine("Client connected, initializing...");

                NetworkStream clientStream = client.GetStream();
                
                clientStream.Read(clientInData, 0, (int)client.ReceiveBufferSize);
                clientInitializationString = System.Text.Encoding.ASCII.GetString(clientInData);
                clientInitializationString = clientInitializationString.Substring(0, clientInitializationString.IndexOf("~"));

                Console.WriteLine("Client initialization string: " + clientInitializationString);

                ConnectedClient newClient = new ConnectedClient(clientInitializationString, client);

                //if the username is already in use, reject the connection and close it out
                if (connectedClients.ContainsKey(newClient.Username)) {
                    messageOut("Connection rejected: Duplicate username detected. Please reconnect with a different username.", "SERVER", newClient);
                } 
                else
                {
                    connectedClients.Add(newClient.Username, newClient);
                    messageOut(newClient.Username + " connected!", "SERVER");
                    ClientManager clientManager = new ClientManager();
                    clientManager.InitializeClientThread(newClient);
                }
              
            }

            Console.ReadLine();

        }

        // send a message out to all connected users
        public static void messageOut(string message, string senderUsername)
        {
            foreach (KeyValuePair<string, ConnectedClient> connectedClient in connectedClients)
            {
                messageOut(message, senderUsername, connectedClient.Value);
            }
        }

        // send a message out to a specific user
        public static void messageOut(string message, string senderUsername, ConnectedClient connectedClient)
        {
            byte[] outData = new byte[65536];
            string outMessage;

            NetworkStream outStream = connectedClient.ClientTcpConnection.GetStream();

            outMessage = senderUsername + " at " + DateTime.Now + ": " + message;

            Console.WriteLine(outMessage);

            outData = Encoding.Default.GetBytes(outMessage);

            outStream.Write(outData, 0, outData.Length);
        }
    }

    //class to store a connected client's information
    public class ConnectedClient
    {
        //string clientID;
        string username;
        TcpClient clientTcpConnection;

        public ConnectedClient(string username, TcpClient tcpClient)
        {
            //this.clientID = Guid.NewGuid().ToString();
            this.username = username;
            this.clientTcpConnection = tcpClient;
        }

        //public string ClientID { get { return clientID; } }
        public string Username { get { return username; } }
        public TcpClient ClientTcpConnection { get { return clientTcpConnection; } }
    }

    //class to control each connected client in a committed thread
    public class ClientManager
    {
        ConnectedClient connectedClient;

        public void InitializeClientThread(ConnectedClient clientToStart)
        {
            connectedClient = clientToStart;
            Thread clientThread = new Thread(ChatManager);
            clientThread.Start();
        }

        private void ChatManager()
        {
            byte[] clientInData = new byte[65536];
            string inMessage;

            //await incoming messages
            while (true)
            {
                try
                {
                    //receive incoming message from the connected client
                    NetworkStream clientStream = connectedClient.ClientTcpConnection.GetStream();

                    clientStream.Read(clientInData, 0, (int)connectedClient.ClientTcpConnection.ReceiveBufferSize);
                    inMessage = System.Text.Encoding.Default.GetString(clientInData);
                    inMessage = inMessage.Substring(0, inMessage.IndexOf("~"));

                    //send that message out to all connected clients
                    ChatServer.messageOut(inMessage, connectedClient.Username);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Message error: " + ex.ToString());
                }
            }
        }
    }
}
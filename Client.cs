using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client Instance { set; get; }

    private GameManager gameManager;
    public string clientName;
    private static readonly int portNumber = 6007;
    private static string serverAddress = "CHANGEME";
    private Thread clientReceiveThread;
    private bool socketReady;
    private Socket sender;
    private static byte[] buffer = new byte[256];


    public enum GameHeaders
    {
        USER = 0,
        MOVE = 1,
        ENDT = 2,
        CHAT = 3
    }

    private void Awake()
    {
        if (Client.Instance == null) // no instance 
        {
            Instance = GetComponent<Client>();
            DontDestroyOnLoad(this.gameObject);
        }
        else                        // otherwise destroy my existance
        {
            Destroy(this.gameObject);
        }
    }
    
    // connect to the game server
    public bool ConnectToServer()
    {
        if (socketReady) return false;

        // create IPAddress object from ip address
        if (!IPAddress.TryParse(serverAddress, out var serverIp))
        {
            Debug.LogError("Invalid IP Address");
        }

        try
        {
            CreateSocket(serverIp);
            clientReceiveThread = new Thread(() => SendNWait((int)GameHeaders.USER, clientName));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();            
        }
        catch (Exception e)
        {
            Debug.LogError("On client connect: " + e.Message);
        }

        return socketReady;
    }
    private void CreateSocket(IPAddress serverIp)
    {
        try
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPEndPoint localEndPoint = new IPEndPoint(serverIp, portNumber);

            // Creation TCP/IP Socket using  
            // Socket Class Costructor 
            sender = new Socket(serverIp.AddressFamily,
                       SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Connect Socket to the remote  
                // endpoint using method Connect() 
                sender.Connect(localEndPoint);

                // We print EndPoint information  
                // that we are connected 
                Debug.Log("Socket connected to -> " +
                              sender.RemoteEndPoint.ToString());

                // Send data to server
                // USER|xxx
                socketReady = true;
            }

            catch (Exception e)
            {
                Debug.Log("Unexpected exception : " + e.ToString());
                GameManager.Instance.Quit(0);
                // SERVER IS NOT ONLINE
            }
        }
        catch (SocketException e)
        {
            Debug.Log("SocketException : " + e.ToString());
        }

    }

    // ONLY USE FOR START
    // REASON: paring players togther
    // MUST wait on main thread
    private void SendNWait(int header, string data)
    {
        Send(header, data);
        // Data buffer 
        byte[] messageReceived = new byte[1024];

        // Wait for data from server
        int byteRecv = sender.Receive(messageReceived);
        if (byteRecv > 0)
        {
            OnIncomingData(Encoding.ASCII.GetString(messageReceived));
        }

    }
    // check socket to server for messages on every frame 
    void Update()
    {
        if (socketReady)
            if (sender.Available > 0)
            {
                // Data buffer 
                byte[] messageReceived = new byte[1024];
                // Wait for data from server
                int byteRecv = sender.Receive(messageReceived);
                if (byteRecv > 0)
                {
                    OnIncomingData(Encoding.ASCII.GetString(messageReceived));
                }
            }
    }

    // Send messages to the server
    public void Send(int header, string data)
    {
        if (!socketReady) return;

        string message = "";

        switch (header)
        {
            case (int)GameHeaders.USER: // Join game
                message = "USER|";
                break;
            case (int)GameHeaders.MOVE: // Move
                message = "MOVE|";
                break;
            case (int)GameHeaders.ENDT: // Rematch
                message = "REMT|";
                break;
            case (int)GameHeaders.CHAT: // Message
                message = "CHAT|";
                break;
        }
        message += data;
        // Creation of message that 
        // we will send to Server 
        byte[] messageSent = Encoding.ASCII.GetBytes(message);
        Debug.Log("Sent: " + message);
        int byteSent = sender.Send(messageSent);
    }

    // Read messages from the server
    public void OnIncomingData(string data)
    {
        gameManager = GameManager.Instance;
        data = SanatizeString(data);
        Debug.Log("Client: " + data);
        string[] aData = data.Split('|');

        // Execute in main thread, unless in START (utualizes gamemanager)
        if (aData[0] != "START")
        {
            if (clientReceiveThread != Thread.CurrentThread)
            {
                Debug.LogWarning("Not on main thread! Aborting!!");
                clientReceiveThread.Abort();
            }
        }
        switch (aData[0])
        {
            case "START":
                gameManager.isWhite = aData[1].Contains("WHITE");
                gameManager.oponentUsername = aData[2];
                Debug.Log("Opponent name: " + gameManager.oponentUsername);
                Debug.Log("Is White = " + gameManager.isWhite);
                gameManager.startTrigger = true;
                break;
            case "MOVE":
                Debug.Log("MOVE");
                // move pieces
                CheckersBoard.Instance.ForceMove(aData);
                break;
            case "REMT":
                Debug.Log("Rematch?");
                gameManager.startTrigger = true;
                gameManager.isWhite = !gameManager.isWhite; // switch sides
                GameManager.Instance.StartGame();   // start game
                break;
            case "CHAT":
                // update chat log
                GameStat.Instance.ChatMessage(aData[1], false);
                break;
            case "QUIT":
                GameManager.Instance.Quit(1);
                break;
            default:
                Debug.LogError("Received a header outside of range");
                break;
        }
    }

    // close socket on each instance of the game closing or a user quiting
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
    private void OnDisable()
    {
        CloseSocket();
    }
    public void CloseSocket()
    {
        if (!socketReady) return;

        clientReceiveThread.Abort();
        sender.Shutdown(SocketShutdown.Both);
        sender.Close();
        socketReady = false;
    }

    // Incoming data is buffed to MASSIVE size
    // usernames must be sanitized
    public string SanatizeString(string tooBig)
    {
        string clean = "";
        for (int i = 0; i < tooBig.Length; i++)
        {
            if (tooBig[i] != '\0')
                clean += tooBig[i];
            else
                break;
        }
        return clean;
    }

}

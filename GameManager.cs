using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { set; get; }

    public GameObject mainMenu;
    public GameObject hostPrompt;
    public GameObject userPrompt;
    public GameObject clientObject;

    private static string[] errorMessages = new string[2]
    {
        "Fatal: Could not connect to server!",
        "Oops: Your oppenent has quit on you!"
    };

    public Text serverDown;
    public int serverError = -1;

    public bool startTrigger = false;
    public byte[] incomingDataTrigger = null;
    public bool isOnline;
    public bool isWhite;
    public InputField nameInput;
    public string oponentUsername;

    private Client client;

    enum GameHeaders
    {
        USER = 0,
        MOVE = 1,
        ENDT = 2,
        CHAT = 3
    }

    private void Start()
    {
        if (GameManager.Instance == null) // no instance 
        {
            Instance = GetComponent<GameManager>();
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance.serverError > -1) 
        {
            // 에러 보여주기
            serverDown.text = errorMessages[Instance.serverError];
            serverDown.gameObject.SetActive(true);

            // Destroy previous and set as instance
            Destroy(Instance.gameObject);
            Instance = GetComponent<GameManager>();
            DontDestroyOnLoad(gameObject);

        }
        else // otherwise destroy my existance
        {
            Destroy(this.gameObject);
        }

        isWhite = true;
        hostPrompt.SetActive(false);
        userPrompt.SetActive(false);
        
    }

    private void Update()
    {

        if (startTrigger)
            StartGame(); // this is very bad but works ¯\_(ツ)_/¯
                        // thank you threads :)
    }
    public void PromptUsername()
    {
        mainMenu.SetActive(false);
        userPrompt.SetActive(true);
    }

    public void ConnectButton()
    {
        IniatlizeConnection();
        serverDown.gameObject.SetActive(false);
        mainMenu.SetActive(false);
        userPrompt.SetActive(false);
    }


    // connect to server and send username
    public void IniatlizeConnection()
    {
        ConnectToServer();
        hostPrompt.SetActive(true);
    }

    // 
    public void ConnectToServer()
    {
        isOnline = true;
        try
        {
            client = Client.Instance;
            client.clientName = nameInput.text;
            if (client.clientName == "")
                client.clientName = "Anonymous";

            client.ConnectToServer();
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }

        mainMenu.SetActive(false);
        hostPrompt.SetActive(true);
    }

    /**
     * Error Codes
     *  -1 : everything is fine
     *   0 : could not connect to server
     *   1 : opponent quit
     */ 
    public void Quit(int errorCode)
    {
        // Close socket if online
        if (client != null)
            client.CloseSocket();

        Debug.LogWarning($"Error Code: {errorCode}");

        Instance.serverError = errorCode;
        SceneManager.LoadScene("Menu");
    }

    public void BackButton()
    {
        isOnline = false;
        mainMenu.SetActive(true);
        userPrompt.SetActive(false);
        hostPrompt.SetActive(false);
    }

    public void StartGame()
    {
        startTrigger = false;
        SceneManager.LoadScene("Game");
    }
}


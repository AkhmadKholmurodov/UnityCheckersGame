using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class CheckersBoard : MonoBehaviour
{
    public static CheckersBoard Instance { set; get; }
    private GameManager gameManager;

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;
    public GameObject moveIndicatorPrefab;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

    public bool isWhite;       // white piece
    public bool isWhiteTurn;   // white turn
    private bool hasKilled;
    public bool killPieceTrigger = false;

    public bool isOnline;

    private Piece selectedPiece;
    private Piece movedPiece;
    private List<Piece> forcedPieces;
    private List<GameObject> moveIndicators;

    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag;

    private void Start()
    {
        GenerateBoard();
        forcedPieces = new List<Piece>();
        moveIndicators = new List<GameObject>();
        isWhiteTurn = true;
        isWhite = true;
        gameManager = GameManager.Instance;
        Instance = GetComponent<CheckersBoard>();

        if (gameManager)
        {
            isWhite = gameManager.isWhite;
            isOnline = gameManager.isOnline;
        }
        
        if (isOnline && !isWhite)
        {
            FlipTable();
        }
    }

    private void Update()
    {
        updateMouseOver();
        forcedPieces = ScanForPossibleKill();
        if (forcedPieces.Count != moveIndicators.Count)
        {
            foreach (Piece piece in forcedPieces)
            {
                GameObject moveIndicator = Instantiate(moveIndicatorPrefab) as GameObject;
                moveIndicator.transform.position = new Vector3(piece.transform.position.x, 0.01f, piece.transform.position.z);
                moveIndicators.Add(moveIndicator);
            }
        }
        if ((isWhite) ? isWhiteTurn : !isWhiteTurn)
        {
            if(isOnline && !Banner.alert.gameOver)
                Banner.alert.ShowTurn(isWhite == isWhiteTurn);

            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if (selectedPiece != null)
                updatePickedUp(selectedPiece);

            // Left click to select piece
            if (Input.GetMouseButtonDown(0))
                SelectPiece(x, y);

            if (Input.GetMouseButtonUp(0))
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
        }
    }

    /** updateMouseOver()
     * Raycast from main camera to mouse position
     * In game space
     */ 
    private void updateMouseOver()
    {
        if(!Camera.main)
        {
            Debug.LogError("Unable to find main camera!");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            // mouse is over the board
            mouseOver.x = (int)(hit.point.x - boardOffset.x);
            mouseOver.y = (int)(hit.point.z - boardOffset.z); // board is on the floor ! wall
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }

    private void updatePickedUp(Piece p)
    {
        if (!Camera.main)
        {
            Debug.LogError("Unable to find main camera!");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    private void SelectPiece(int x, int y)
    {
        // out of bounds
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece p = pieces[x, y];
        if(p != null && p.isWhite == isWhite)
        {
            if (forcedPieces.Count == 0)
            {
                selectedPiece = p;
                startDrag = mouseOver;
            }
            else
            {
                // Look for piece under our forced pieces list
                if (forcedPieces.Find(fp => fp == p) == null)
                    return;

                selectedPiece = p;
                startDrag = mouseOver;
            }
        }
    }

    public void ForceMove(string[] raw)
    {
        TryMove(Int32.Parse(raw[1]), Int32.Parse(raw[2]), Int32.Parse(raw[3]), Int32.Parse(raw[4]));
    }
    private void TryMove(int x1, int y1, int x2, int y2)
    {
        hasKilled = false;
        forcedPieces = ScanForPossibleKill();
        // Multiplayer Support !!
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        selectedPiece = pieces[x1, y1];

        // Check out of bounds
        if (x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
        {
            if (selectedPiece != null)
                MovePiece(selectedPiece, x1, y1);

            selectedPiece = null;
            startDrag = Vector2.zero;
            return;
        }

        if (selectedPiece != null)
        {
            // Not moved
            if (endDrag == startDrag)
            {
                MovePiece(selectedPiece, x1, y1);
                selectedPiece = null;
                startDrag = Vector2.zero;
                return;
            }

            // Check for valid move
            if(selectedPiece.ValidMove(pieces, x1, y1, x2, y2))
            {
                // Valid move && Kill?
                if(Mathf.Abs(x1-x2) == 2)
                {
                    // remove killed piece
                    Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    DestroyImmediate(p.gameObject);
                    hasKilled = true;
                }

                // Supposed to kill something?
                if(forcedPieces.Count != 0 && !hasKilled)
                {
                    MovePiece(selectedPiece, x1, y1);
                    selectedPiece = null;
                    startDrag = Vector2.zero;
                    return;
                }

                pieces[x2, y2] = selectedPiece;
                pieces[x1, y1] = null;
                MovePiece(selectedPiece, x2, y2);
                movedPiece = selectedPiece;
                if (Client.Instance)
                {
                    string m = $"{x1}|{y1}|{x2}|{y2}";
                    Client.Instance.Send(1, m);
                    EndTurn();
                }
            }
            else
            {
                MovePiece(selectedPiece, x1, y1);
                selectedPiece = null;
                startDrag = Vector2.zero;
            }
        }
    }

    private void EndTurn()
    {
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        if(selectedPiece != null) // Promotion
        {
            // White -> King
            if(selectedPiece.isWhite && !selectedPiece.isKing && y == 7)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
            // Black -> King
            if (!selectedPiece.isWhite && !selectedPiece.isKing && y == 0)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
        }

        selectedPiece = null;
        startDrag = Vector2.zero;

        foreach(GameObject moveIndicator in moveIndicators)
        {
            DestroyImmediate(moveIndicator);
        }
        moveIndicators.Clear();

        // Allow second move
        if (ScanForPossibleKill(x, y).Count != 0 && hasKilled)
        {
            Debug.Log("Found a possible kill");
            return;
        }

        hasKilled = false;
        movedPiece = null;
        isWhiteTurn = !isWhiteTurn;

        if (!isOnline)
            isWhite = !isWhite;  // Swap turn locally

        
        CheckVictory();
    }

    private void CheckVictory() 
    {
        var ps = FindObjectsOfType<Piece>();
        bool hasWhite = false, hasBlack = false;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].isWhite)
                hasWhite = true;
            else
                hasBlack = true;
        }

        if (!hasWhite)
            Victory(false);
        else if (!hasBlack)
            Victory(true);
    }

    public void Victory(bool isWhite)
    {
        GameStat gameStat = GameStat.Instance;
        string winner;
        if (isWhite)
        {
            if (isOnline)
            {
                if (this.isWhite)
                    winner = gameStat.whoAmI;
                else
                    winner = gameStat.opponent;
            }
            else
                winner = "White";

        }
        else
        {
            if (isOnline)
            {
                if (!this.isWhite)
                    winner = gameStat.whoAmI;
                else
                    winner = gameStat.opponent;
            }
            else
                winner = "Black";
        }
        GameStat.Instance.EndOfGame();
        Banner.alert.WinnerPopUp(winner);
    }
    
    private List<Piece> ScanForPossibleKill(int x, int y)
    {
        forcedPieces = new List<Piece>();

        if(pieces[x,y].isForceToMove(pieces, x, y))
            forcedPieces.Add(pieces[x,y]);

        return forcedPieces;
    }

    private List<Piece> ScanForPossibleKill()
    {
    forcedPieces = new List<Piece>();
        // Scan all the pieces
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (pieces[i, j] != null && pieces[i, j].isWhite == isWhiteTurn)
                    if (pieces[i, j].isForceToMove(pieces, i, j))
                        if (movedPiece == null || movedPiece == pieces[i, j])
                        {
                            //Debug.Log(movedPiece == null ? "Null" : "not null");
                            forcedPieces.Add(pieces[i, j]);
                        }
        return forcedPieces;
    }

    /** GenerateBoard()
     * White Team and Black team on Start
     * will be intialized to correct position via MovePiece()
     */ 
    private void GenerateBoard()
    {
        // Generate White team
        for(int y = 0; y < 3; y++)
        {
            bool oddRow = (y % 2 == 0);
            for(int x = 0; x < 8; x+=2)
            {
                // Generate our Piece
                GeneratePiece( (oddRow)? x : x + 1 , y);
            }
        }

        // Generate Black team
        for (int y = 7; y > 4; y--)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                // Generate our Piece
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }
    }

    /**
     * Create and Move piece 
     * Changes selected prefab based on side of the board (y>3)
     */ 
    private void GeneratePiece(int x, int y)
    {
        bool isWhite = (y > 3) ? false : true;
        GameObject go = Instantiate((isWhite)?whitePiecePrefab:blackPiecePrefab) as GameObject;
        go.transform.SetParent(transform);
        Piece p = go.GetComponent<Piece>();
        pieces[x, y] = p;
        MovePiece(p, x, y);
    }

    /**
     * Tranform position based on
     * board offset, peice offset and even/odd row (tile color)
     */
    private void MovePiece(Piece p, int x, int y)
    {
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
    }

    private void FlipTable()
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        mainCamera.transform.position = new Vector3(2, 10, 0);
        mainCamera.transform.eulerAngles = new Vector3(
            mainCamera.transform.eulerAngles.x,
            mainCamera.transform.eulerAngles.y + 180f,
            mainCamera.transform.eulerAngles.z
            );
    }
}

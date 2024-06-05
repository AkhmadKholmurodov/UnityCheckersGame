using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool isWhite;
    public bool isKing;
    public GameObject moveIndicatorPrefab;

    public bool ValidMove(Piece[,] board, int x1, int y1, int x2, int y2)
    {
        // If you are moving on top of another piece
        if (board[x2, y2] != null)
            return false;

        int deltaMoveX = (int)Mathf.Abs(x1 - x2); // number of jumps
        int deltaMoveY = y2 - y1;

        // White Team Move
        if (isWhite || isKing)
        {
            if(deltaMoveX == 1) // in any x direction
            {
                if (deltaMoveY == 1) // positive y direction
                    return true;
            }
            else if(deltaMoveX == 2) // skip over piece
            {
                if(deltaMoveY == 2)
                {
                    Piece p = board[(x1+x2)/2, (y1+y2)/2]; // that is not the same color
                    if (p != null && p.isWhite != isWhite)
                        return true;
                }
            }
        }
        
        if (!isWhite || isKing)
        {
            if (deltaMoveX == 1) 
            {
                if (deltaMoveY == -1) 
                    return true;
            }
            else if (deltaMoveX == 2)
            {
                if (deltaMoveY == -2)
                {
                    Piece p = board[(x1 + x2) / 2, (y1 + y2) / 2]; 
                    if (p != null && p.isWhite != isWhite)
                        return true;
                }
            }
        }

        return false;
    }

    // Double Jump
    public bool isForceToMove(Piece[,] board, int x, int y)
    {

        if(isWhite || isKing)
        {

            if(x >= 2 && y <=5)
            {
                Piece p = board[x - 1, y + 1];
                // if there is a piece and ! same color
                if(p != null && p.isWhite != isWhite)
                {
                    // Check if its possible to land after jump
                    int landX = x - 2;
                    int landY = y + 2;
                    if (board[landX, landY] == null && !IsOutOfBounds(landX, landY))
                    {
                        return true;
                    }
                        
                }
            }
            // Top Right
            if (x <= 5 && y <= 5)
            {
                Piece p = board[x + 1, y + 1];
                // if there is a piece and ! same color
                if (p != null && p.isWhite != isWhite)
                {
                    // Check if its possible to land after jump
                    int landX = x + 2;
                    int landY = y + 2;
                    if (board[landX, landY] == null && !IsOutOfBounds(landX, landY))
                    {
                        return true;
                    }
                }
            }
        }
        // Black Team
        if(!isWhite || isKing)
        {
            // Bottom Left
            if (x >= 2 && y >= 2)
            {
                Piece p = board[x - 1, y - 1];
                // if there is a piece and ! same color
                if (p != null && p.isWhite != isWhite)
                {
                    // Check if its possible to land after jump
                    int landX = x - 2;
                    int landY = y - 2;
                    if (board[landX, landY] == null && !IsOutOfBounds(landX, landY))
                    {
                        return true;
                    }
                }
            }
            // Bottom Right
            if (x <= 5 && y >= 2)
            {
                Piece p = board[x + 1, y - 1];
                // if there is a piece and ! same color
                if (p != null && p.isWhite != isWhite)
                {
                    // Check if its possible to land after jump
                    int landX = x + 2;
                    int landY = y - 2;
                    if (board[landX, landY] == null && !IsOutOfBounds(landX, landY))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        if (x > 8 || x < 0)
            return true;
        if (y > 8 || y < 0)
            return true;
        return false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Banner : MonoBehaviour
{
    public static Banner alert { get; set; }
    private Text display;
    private Image rend;
    void Awake()
    {
        alert = GetComponent<Banner>();
        display = transform.GetChild(0).GetComponent<Text>();
        rend = GetComponent<Image>();
    }


    public void OpponentPopUp(string opponent, string me)
    {
        // only show for online
        if (opponent != "" && me != "")
        {
            display.text = $"{me} vs {opponent}";
            GameStat.Instance.ChatMessage($"has joined", false);
        }
        else // local
            display.text = "Practice";

        StartCoroutine(hideAfter(2));
    }

    public bool gameOver = false;
    public void WinnerPopUp(string winner)
    {
        gameOver = true;
        display.text = winner + " has won!";
        // static don't fade out
        show(display, rend);
        // rematch?
    }

    // Local: White || Black Turn
    // Online: Your Move
    bool completeFade = true;
    public void ShowTurn(bool isPlaying)
    {
        /// CURRRENTLY FLASHING
        if (isPlaying && completeFade && !gameOver)
        {
            completeFade = false;
            display.text = "Your Move";
            StartCoroutine(FadeOut(2f, display, rend));
        }
    }

    private IEnumerator hideAfter(int sec)
    {
        // reset
        Image rend = GetComponent<Image>();
        Color temp = rend.color;
        temp.a = 1.0f;
        rend.color = temp;

        yield return new WaitForSeconds(sec);

        StartCoroutine(FadeOut(1f, display, rend));

    }

    public IEnumerator FadeOut(float t, Text i, Image j)
    {
        show(i, j);
        while (i.color.a > 0.0f)
        {
            i.color = new Color(i.color.r, i.color.g, i.color.b, i.color.a - (Time.deltaTime / t));
            j.color = new Color(j.color.r, j.color.g, j.color.b, j.color.a - (Time.deltaTime / t));
            yield return null;
        }
        completeFade = true;
    }

    private void show(Text i, Image j)
    {
        i.color = new Color(i.color.r, i.color.g, i.color.b, 1);
        j.color = new Color(j.color.r, j.color.g, j.color.b, 1);
    }
}

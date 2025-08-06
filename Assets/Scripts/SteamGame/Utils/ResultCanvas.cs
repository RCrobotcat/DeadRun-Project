using System;
using UnityEngine;
using UnityEngine.UI;

public class ResultCanvas : MonoBehaviour
{
    public GameObject resultPanel;

    public Text winnerText;
    public Text winnerTotalScoreText;

    public Text yourTotalScoreText;
    public Text yourCommentText;

    public Button QuitBtn;

    public Camera cam;

    private void Start()
    {
        QuitBtn.onClick.AddListener(() => { Application.Quit(); });
    }

    private void Update()
    {
        if (LobbyController.Instance != null)
        {
            if (LobbyController.Instance.LocalPlayerObjectController != null)
            {
                if (LobbyController.Instance.LocalPlayerObjectController.isEnd)
                    if (!Cursor.visible)
                        Cursor.visible = true;
            }
        }
    }

    public void ShowResult(string winner, int winnerTotalScore, int yourTotalScore)
    {
        cam.gameObject.SetActive(true);
        resultPanel.SetActive(true);

        winnerText.text = winner;
        winnerTotalScoreText.text = winnerTotalScore.ToString();

        yourTotalScoreText.text = yourTotalScore.ToString();
        if (winnerTotalScore == yourTotalScore)
        {
            yourCommentText.text = "Congratulations! You are the winner!";
        }
        else if (yourTotalScore < winnerTotalScore)
        {
            yourCommentText.text = "You lost, better luck next time!";
        }
    }
}
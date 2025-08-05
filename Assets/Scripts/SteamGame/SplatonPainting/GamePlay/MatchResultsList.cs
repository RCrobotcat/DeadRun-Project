using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultsList : NetworkBehaviour
{
    public GameObject resultPanel;
    public Transform resultsContent;

    public Text winnerText;

    public GameObject matchResultItemPrefab;

    private MyNetworkManager _myNetworkManager;

    private MyNetworkManager MyNetworkManager
    {
        get
        {
            if (_myNetworkManager != null)
            {
                return _myNetworkManager;
            }

            return _myNetworkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    public void ShowMatchResults()
    {
        Dictionary<int, float> playerScores = new Dictionary<int, float>();

        foreach (var player in MyNetworkManager.GamePlayers)
        {
            PlayerSplatonPainting playerSplatonPainting = player.GetComponent<PlayerSplatonPainting>();
            float playerPaintedAreas = playerSplatonPainting.paintingParticles.PaintAreas;

            GameObject matchResultItemObj = Instantiate(matchResultItemPrefab, resultsContent);
            MatchResultItem matchResultItem = matchResultItemObj.GetComponent<MatchResultItem>();
            matchResultItem.SetupMatchResultItem(player.playerName, playerPaintedAreas);

            playerScores.Add(player.playerID, playerPaintedAreas);
        }

        // Determine the winner
        if (NetworkServer.active)
        {
            string winnerName = "";
            int winnderID = -1;
            float maxPaintedAreas = -1f;
            PlayerObjectController winner = null;
            foreach (var entry in playerScores)
            {
                if (entry.Value > maxPaintedAreas)
                {
                    maxPaintedAreas = entry.Value;
                    winnderID = entry.Key;
                    winner = MyNetworkManager.GamePlayers.Find((p) => p.playerID == winnderID);
                    winnerName = winner.playerName;
                }
            }

            winner.CurrentScore++;
            winnerText.text = $"Winner: {winnerName} with {maxPaintedAreas:F2} m²!";

            RpcMatchResults(winnerName, maxPaintedAreas);
        }

        resultPanel.transform.localPosition.To(new Vector3(0, -90, 0), 0.6f,
            (pos) => { resultPanel.transform.localPosition = pos; });

        if (NetworkServer.active)
            RpcShowMatchResults();
    }

    public void HideMatchResults()
    {
        resultPanel.SetActive(false);
    }

    [ClientRpc]
    private void RpcMatchResults(string winnerName, float maxPaintedAreas)
    {
        if (!isClientOnly)
            return;

        winnerText.text = $"Winner: {winnerName} with {maxPaintedAreas:F2} m²!";
        foreach (var player in MyNetworkManager.GamePlayers)
        {
            PlayerSplatonPainting playerSplatonPainting = player.GetComponent<PlayerSplatonPainting>();
            float playerPaintedAreas = playerSplatonPainting.paintingParticles.PaintAreas;

            GameObject matchResultItemObj = Instantiate(matchResultItemPrefab, resultsContent);
            MatchResultItem matchResultItem = matchResultItemObj.GetComponent<MatchResultItem>();
            matchResultItem.SetupMatchResultItem(player.playerName, playerPaintedAreas);
        }
    }

    [ClientRpc]
    private void RpcShowMatchResults()
    {
        if (!isClientOnly)
            return;
        
        resultPanel.transform.localPosition.To(new Vector3(0, -90, 0), 0.6f,
            (pos) => { resultPanel.transform.localPosition = pos; });
    }
}
using UnityEngine;
using UnityEngine.UI;

public class MatchResultItem : MonoBehaviour
{
    public Text playerName;
    public Text playerPaintedAreas;
    
    public void SetupMatchResultItem(string name, float paintedAreas)
    {
        playerName.text = name;
        playerPaintedAreas.text = $"{paintedAreas:F2} m²";
    }
}
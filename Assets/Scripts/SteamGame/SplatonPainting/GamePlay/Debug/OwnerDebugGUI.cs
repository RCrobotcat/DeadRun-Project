using UnityEngine;

public class OwnerDebugGUI : MonoBehaviour
{
    public Paintable paintable;
    Rect texRect = new Rect(10, 10, 256, 256);

    void OnGUI()
    {
        if (paintable != null && paintable.getOwnerTexture() != null)
        {
            GUI.DrawTexture(texRect, paintable.getOwnerTexture(), ScaleMode.ScaleToFit, false);
        }
    }
}
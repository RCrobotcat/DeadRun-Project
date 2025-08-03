using UnityEngine;

public class OwnerDebugGUI : MonoBehaviour
{
    public Paintable paintable;
    Rect texRect = new Rect(10, 10, 256, 256);

    void OnGUI()
    {
        if (paintable != null && paintable.getExtend() != null)
        {
            GUI.DrawTexture(texRect, paintable.getExtend(), ScaleMode.ScaleToFit, false);
        }
    }
}
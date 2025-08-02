using Mirror;
using UnityEngine;

public enum PaintingColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Purple,
    Orange
}

public class SpawnPosPainting : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSpawnedPlayerIDChanged))]
    public int spawnedPlayerID = -1;

    [SyncVar(hook = nameof(OnPaintingColorChanged))]
    public PaintingColor paintingColor;

    void OnSpawnedPlayerIDChanged(int oldValue, int newValue)
    {
        Debug.Log("SpawnPosPainting: SpawnedPlayerID changed from " + oldValue + " to " + newValue);
    }

    void OnPaintingColorChanged(PaintingColor oldValue, PaintingColor newValue)
    {
        Debug.Log("SpawnPosPainting: PaintingColor changed from " + oldValue + " to " + newValue);
    }
}
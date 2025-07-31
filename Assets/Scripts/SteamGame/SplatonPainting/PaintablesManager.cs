using System.Collections.Generic;

public class PaintablesManager : Singleton<PaintablesManager>
{
    public List<Paintable> paintables = new List<Paintable>();

    public int GetPaintableID(Paintable paintable)
    {
        return paintables.IndexOf(paintable);
    }

    public Paintable GetPaintableByID(int id)
    {
        if (id < 0 || id >= paintables.Count)
        {
            return null;
        }

        return paintables[id];
    }

    public void RegisterPaintable(Paintable paintable)
    {
        if (!paintables.Contains(paintable))
        {
            paintables.Add(paintable);
        }
    }

    public void UnregisterPaintable(Paintable paintable)
    {
        if (paintables.Contains(paintable))
        {
            paintables.Remove(paintable);
        }
    }
}
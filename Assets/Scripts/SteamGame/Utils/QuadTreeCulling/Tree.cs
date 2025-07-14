using UnityEngine;

public class Tree
{
    public Bounds bound;
    public Node root;
    public int maxDepth = 6;
    public int maxChildCount = 4;

    public Tree(Bounds bound)
    {
        this.bound = bound;
        this.root = new Node(bound, 0, this);
    }

    public void InsertData(Transform data)
    {
        root.InsertData(data);
    }

    public void DrawBound()
    {
        root.DrawBound();
    }

    public void TriggerMove(Plane[] planes)
    {
        root.TriggerMove(planes);
    }
}
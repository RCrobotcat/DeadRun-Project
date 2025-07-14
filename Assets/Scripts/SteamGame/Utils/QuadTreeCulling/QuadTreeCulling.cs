using UnityEngine;

public class QuadTreeCulling : MonoBehaviour
{
    public int n = 10;
    public Bounds mainBound;
    [HideInInspector] public Tree tree;
    bool startEnd = false; // 是否初始化完毕
    public Camera cam;

    Plane[] planes; // 视锥体的6个面

    void Start()
    {
        InitTree();
    }

    void Update()
    {
        if (startEnd)
        {
            GeometryUtility.CalculateFrustumPlanes(cam, planes);

            // 四叉树剔除
            tree.TriggerMove(planes);
        }
    }

    public void InitTree()
    {
        planes = new Plane[6];
        Bounds bounds = new Bounds(mainBound.center, new Vector3(2 * n, 0, 2 * n));

        tree = new Tree(bounds);
        startEnd = true;
    }

    private void OnDrawGizmos()
    {
        if (startEnd)
        {
            tree.DrawBound();
        }
        else
        {
            Gizmos.DrawWireCube(mainBound.center, mainBound.size);
        }
    }
}
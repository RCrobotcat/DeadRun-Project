using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Bounds bound;
    public int currentDepth;
    public Tree tree;
    public List<Transform> datas = new List<Transform>();
    public Node[] childs;

    public Vector2[] bif = new Vector2[]
    {
        new Vector2(-1, 1),
        new Vector2(1, 1),
        new Vector2(-1, -1),
        new Vector2(1, -1)
    };

    public Node(Bounds bound, int currentDepth, Tree tree)
    {
        this.bound = bound;
        this.currentDepth = currentDepth;
        this.tree = tree;
    }

    public void InsertData(Transform data)
    {
        // 层级没到上限 且 没有子节点 可以创建子节点
        if (currentDepth < tree.maxDepth && childs == null)
        {
            CreatChild();
        }

        if (childs != null)
        {
            for (int i = 0; i < childs.Length; i++)
            {
                // 判断数据的位置是否归属于该子节点的区域
                if (childs[i].bound.Contains(data.position))
                {
                    // 继续去下一层查找
                    childs[i].InsertData(data);
                    break;
                }
            }
        }
        else
        {
            datas.Add(data);
        }
    }

    public void CreatChild()
    {
        childs = new Node[tree.maxChildCount];
        for (int i = 0; i < tree.maxChildCount; i++)
        {
            Vector3 center = new Vector3(bif[i].x * bound.size.x / 4, 0, bif[i].y * bound.size.z / 4);
            Vector3 size = new Vector3(bound.size.x / 2, 0, bound.size.z / 2);
            Bounds childbound = new Bounds(center + bound.center, size);
            childs[i] = new Node(childbound, currentDepth + 1, tree);
        }
    }

    public void DrawBound()
    {
        // 有数据画蓝色框框
        if (datas.Count != 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bound.center, bound.size - Vector3.one * 0.1f);
        }
        else // 没数据画绿色框框
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bound.center, bound.size - Vector3.one * 0.1f);
        }

        // 有子物体让子物体去画
        if (childs != null)
        {
            for (int i = 0; i < childs.Length; ++i)
            {
                childs[i].DrawBound();
            }
        }
    }

    public void TriggerMove(Plane[] planes)
    {
        // 有子物体让子物体去判断是否重叠
        if (childs != null)
        {
            for (int i = 0; i < childs.Length; ++i)
            {
                childs[i].TriggerMove(planes);
            }
        }

        for (int i = 0; i < datas.Count; i++)
        {
            bool active = GeometryUtility.TestPlanesAABB(planes, bound);
            datas[i].gameObject.SetActive(active);
            datas[i].GetComponent<Renderer>().enabled = active;
        }
    }
}
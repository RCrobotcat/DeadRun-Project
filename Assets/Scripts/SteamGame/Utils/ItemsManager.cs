using System.Collections.Generic;
using UnityEngine;

public class ItemsManager : MonoBehaviour
{
    public GameObject items;

    public List<GameObject> dropItemsPrefabs;

    public GameObject FindDropItemByTableItemName(string name)
    {
        return dropItemsPrefabs.Find(item => item.name == name);
    }
}
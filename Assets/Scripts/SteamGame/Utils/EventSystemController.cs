using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemController : MonoBehaviour
{
    public EventSystem eventSystem;
    
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    private void Update()
    {
        // 检测到两个以上的EventSystem时只保留一个
        var eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystems.Length > 1)
        {
            for (int i = 0; i < eventSystems.Length; i++)
            {
                if(eventSystems[i] != eventSystem)
                    DestroyImmediate(eventSystems[i].gameObject);
            }
        }
    }
}
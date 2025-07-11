using UnityEngine;

public class InteractiveObject : MonoBehaviour
{
    public string Io_name;

    private bool isMouseOver = false;
    public Material outlineMaterial;
    public Material emiMaterial;

    public Material[] originalMaterials; // 原始材质列表
    public Material[] materialsWithOutline; // 包含 Outline 的材质列表

    private void Awake()
    {
        if (string.IsNullOrEmpty(Io_name))
        {
            Io_name = this.name;
        }


        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
        {
            originalMaterials = renderer.materials;

            // 创建包含 Outline 的材质列表
            materialsWithOutline = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                materialsWithOutline[i] = originalMaterials[i];
            }
        }
    }

    private void Update()
    {
        MouseDetect();
    }

    private void MouseDetect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            InteractiveObject interactiveObject = hit.collider.GetComponent<InteractiveObject>();
            if (interactiveObject == this)
            {
                if (!isMouseOver)
                {
                    isMouseOver = true;

                    InteractionEvents.OnMouseHover?.Invoke(this);
                }

                return;
            }
        }

        if (isMouseOver)
        {
            isMouseOver = false;

            InteractionEvents.OnMouseExit?.Invoke(this);
        }
    }

    private void OnMouseDown()
    {
        if (isMouseOver)
            InteractionEvents.OnMouseClick?.Invoke(this);
    }
}
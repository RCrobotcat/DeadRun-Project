using UnityEngine;

public class InteractionManager : Singleton<InteractionManager>
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    private void OnEnable()
    {
        InteractionEvents.OnMouseHover += HandleObjectEmission_ON;
        InteractionEvents.OnMouseExit += HandleObjectEmission_OFF;
        InteractionEvents.OnMouseClick += HandleMouseClick;
    }

    private void OnDisable()
    {
        InteractionEvents.OnMouseHover -= HandleObjectEmission_ON;
        InteractionEvents.OnMouseExit -= HandleObjectEmission_OFF;
        InteractionEvents.OnMouseClick -= HandleMouseClick;
    }

    //HandleObjectEmission_ON  HandleObjectEmission_OFF
    //这两个函数用来控制高光和描边 应该不用改了 ；有性能问题或者内存泄露的可能性
    private void HandleObjectEmission_ON(InteractiveObject obj)
    {
        if (obj == null || obj.materialsWithOutline == null || obj.materialsWithOutline.Length == 0)
        {
            Debug.LogError("Invalid InteractiveObject or materialsWithOutline is not set.");
            return;
        }

        Renderer renderer = obj.GetComponent<Renderer>();


        // 材质数组
        Material[] materials = renderer.materials;
        if (materials.Length <= 1)
        {
            Debug.LogError("Materials does not have enough slots.");
            return;
        }

        Material materialInstance_emi = new Material(obj.emiMaterial);
        Material materialInstance_out = new Material(obj.outlineMaterial);
        // 设置高亮属性
        // materialInstance_emi.SetFloat("_is_HighLighted", 1.0f);
        Debug.Log($"length: {materials.Length}.");
        // 更新材质数组
        materials[materials.Length - 1] = materialInstance_emi;
        materials[materials.Length - 2] = materialInstance_out;

        renderer.materials = materials;

        //Debug.Log($"Mouse is hovering over: {obj.gameObject.name}, highlight and outline applied.");
    }

    private void HandleObjectEmission_OFF(InteractiveObject obj)
    {
        if (obj == null || obj.originalMaterials == null || obj.originalMaterials.Length == 0)
        {
            Debug.LogError("Invalid InteractiveObject or originalMaterials is not set.");
            return;
        }

        Renderer renderer = obj.GetComponent<Renderer>();


        // 还原原始材质
        renderer.materials = obj.originalMaterials;

        //Debug.Log($"Mouse exited: {obj.gameObject.name}, highlight removed.");
    }

    private void HandleMouseClick(InteractiveObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("InteractiveObject is null.");
            return;
        }

        Debug.Log($"Clicked Object: {obj.Io_name}");
        PlaneRotation.Instance.planeToRotate = obj.gameObject;
    }
}
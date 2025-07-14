using System;
using Mirror;
using UnityEngine;

public class InteractiveObject : NetworkBehaviour
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
        if (PlaneRotation.Instance.planeToRotate != gameObject && transform.rotation != Quaternion.identity)
            ResetPlaneRotation();

        if (LobbyController.Instance != null &&
            LobbyController.Instance.LocalPlayerObjectController != null &&
            LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Trapper)
        {
            MouseDetect();
            MouseClick();
        }
    }

    private void ResetPlaneRotation()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.identity;

        transform.rotation =
            Quaternion.Lerp(currentRotation, targetRotation, 0.1f);
    }

    private void MouseDetect()
    {
        if (isServer)
        {
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            if (Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            float maxDistance = 100f;
            int mask = LayerMask.GetMask("Ground");
            QueryTriggerInteraction query = QueryTriggerInteraction.Collide;
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red);

            physicsScene.Raycast(ray.origin, ray.direction, out hit, maxDistance, mask, query);
            if (hit.collider != null)
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
        else
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
    }

    private void MouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isMouseOver)
            {
                InteractionEvents.OnMouseClick?.Invoke(this);
            }
        }
    }

    private void OnMouseDown()
    {
        if (isMouseOver)
            InteractionEvents.OnMouseClick?.Invoke(this);
    }
}
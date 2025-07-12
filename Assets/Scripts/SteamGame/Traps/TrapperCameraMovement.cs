using System;
using UnityEngine;

public class TrapperCameraMovement : MonoBehaviour
{
    float horizontal, vertical, elevation;
    public float MoveSpeed = 5f; // 摄像机的移动速度
    public float MouseRotateSpeed = 3f; // 鼠标控制摄像机旋转的灵敏度

    Vector3 lastMousePosition;

    bool isRightMouseDragging;

    float yaw = 0f; // 水平旋转
    float pitch = 0f; // 垂直旋转

    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        elevation = 0;

        if (Input.GetKey(KeyCode.Space))
            elevation = 1;
        if (Input.GetKey(KeyCode.LeftControl))
            elevation = -1;

        // 右键拖拽用于旋转视角
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseDragging = true;
            lastMousePosition = Input.mousePosition;

            Vector3 angles = transform.rotation.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRightMouseDragging = false;
        }

        // 控制视角旋转
        if (isRightMouseDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            yaw += delta.x * MouseRotateSpeed * Time.deltaTime;
            pitch -= delta.y * MouseRotateSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -89f, 89f); // 防止上下旋转过头

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            lastMousePosition = Input.mousePosition;
        }
    }

    void FixedUpdate()
    {
        Vector3 dir = transform.forward * vertical + transform.right * horizontal + transform.up * elevation;
        transform.position += dir * MoveSpeed * Time.fixedDeltaTime;
    }
}
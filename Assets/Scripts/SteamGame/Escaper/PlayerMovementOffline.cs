﻿using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementOffline : MonoBehaviour
{
    [Header("Movement Settings")] public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    private Rigidbody rb;
    private Vector3 horizontalVelocity;

    Vector3 lastGroundedPosition;
    Vector3 resPosition = Vector3.zero;

    GameObject objPlayerIsNear = null;
    public GameObject ObjPlayerIsNear => objPlayerIsNear;

    ItemsManager itemsManager;

    public Animator _animator;

    // Jumping and Gravity
    private bool isGrounded;
    public float jumpForce = 5f;
    public float gravityMultiplier = 2f;

    [HideInInspector] public bool isAiming = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 锁定旋转 X、Z，避免物理碰撞时翻滚
        rb.freezeRotation = true;

        itemsManager = FindObjectOfType<ItemsManager>();
    }

    void Update()
    {
        isGrounded = CheckIfGrounded();
        if (isGrounded)
        {
            UpdateResPos();
        }

        if (transform.position.y < -5f)
            transform.position = resPosition;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            Jump();

        Transform cam = Camera.main.transform;
        Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        //Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 inputDir = (camForward * v + camRight * h).normalized;

        horizontalVelocity = Vector3.zero;
        if (inputDir.magnitude >= 0.1f)
        {
            if (!isAiming)
            {
                // 计算并平滑朝向
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref turnSmoothVelocity,
                    turnSmoothTime
                );
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

                // 计算水平速度向量
                horizontalVelocity = transform.forward * moveSpeed;
            }
            else
            {
                horizontalVelocity = inputDir * moveSpeed;
            }
        }

        _animator.SetFloat("Speed", horizontalVelocity.magnitude);
        _animator.SetBool("Grounded", isGrounded);

        if (rb.linearVelocity.y < 0)
        {
            _animator.SetBool("Falling", true);
            _animator.SetBool("Jumping", false);
        }
        else
            _animator.SetBool("Falling", false);
    }

    private void UpdateResPos()
    {
        lastGroundedPosition = transform.position;
        Transform[] allResPos = GameObject.FindGameObjectsWithTag("ResPos")
            .Select(go => go.transform)
            .ToArray();

        float minDistance = float.MaxValue;
        foreach (var resPos in allResPos)
        {
            float dis = Vector3.Distance(lastGroundedPosition, resPos.position);
            if (minDistance > dis)
            {
                minDistance = dis;
                resPosition = resPos.position;
            }
        }
    }

    void FixedUpdate()
    {
        // 保持原有的 y 向速度（重力、跳跃等由物理系统处理）
        Vector3 newVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
        rb.linearVelocity = newVelocity;

        // 优化下落速度，确保下落时不会过快（通过增加重力加成来控制下落）
        // if (rb.linearVelocity.y < 0)
        // {
        //     newVelocity.y += Physics.gravity.y * gravityMultiplier * Time.fixedDeltaTime;
        // }
        //
        // rb.linearVelocity = newVelocity;

        //ApplyBuoyancyForce();
    }

    void Jump()
    {
        _animator.SetBool("Jumping", true);

        // 跳跃时清除当前垂直速度（防止上次跳跃的速度干扰）
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    bool CheckIfGrounded()
    {
        Collider[] objectsDetected;
        objectsDetected = Physics.OverlapSphere(transform.position, 2f,
            LayerMask.GetMask("Ground") | LayerMask.GetMask("Interactable"));

        if (objectsDetected.Length > 0)
            return true;
        return false;
    }

    // void ApplyBuoyancyForce()
    // {
    //     Buoyancy buoyancy = GetComponent<Buoyancy>();
    //     buoyancy.Forces.Clear();
    //     foreach (var point in buoyancy.Voxels)
    //     {
    //         buoyancy.ApplyBuoyancyForce(point);
    //     }
    // }
}
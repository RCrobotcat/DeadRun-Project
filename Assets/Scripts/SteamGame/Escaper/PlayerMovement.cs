using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public partial class PlayerMovement : NetworkBehaviour
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

    [HideInInspector] [SyncVar(hook = nameof(OnEquipItemChanged))]
    public string currentEquippedItem;

    ItemsManager itemsManager;

    public Animator _animator;

    public Transform equipItemSlot;

    // Jumping and Gravity
    private bool isGrounded = false;
    public float jumpForce = 5f;
    public float gravityMultiplier = 2f;
    public float jumpTimeInterval = 1f; // 跳跃间隔时间
    private float jumpTimer = 0f;

    [HideInInspector] public bool isAiming = false;
    public GunShooting gun;

    public bool isDead = false;

    public PaintingShooting paintingShooting;

    public bool isEnd = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 锁定旋转 X、Z，避免物理碰撞时翻滚
        rb.freezeRotation = true;

        itemsManager = FindObjectOfType<ItemsManager>();

        OnEquipItemChanged(null, currentEquippedItem);

        jumpTimer = jumpTimeInterval;
    }

    void Update()
    {
        if (isEnd)
            return;
        
        HandleOutlineTimerLogic();

        if (!isLocalPlayer) // 确保只在本地玩家上执行
            return;

        if (isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (GetComponent<PlayerObjectController>().role == PlayerRole.Trapper)
            return;

        HandlePaintingShootingEnable();

        isGrounded = CheckIfGrounded();
        if (isGrounded)
        {
            UpdateResPos();
        }

        if (transform.position.y < -7f)
        {
            transform.position = resPosition + Vector3.up * 0.5f;
            playerObjectController.FellCount++;
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && jumpTimer <= 0f)
            Jump();

        if (jumpTimer > 0f)
            jumpTimer -= Time.deltaTime;

        Transform cam = Camera.main.transform;
        Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        //Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 inputDir = (camForward * v + camRight * h).normalized;

        PlayMovementSounds(h, v);

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

        Collider[] objectsDetected;
        LayerMask interactableMask = LayerMask.GetMask("Interactable") | LayerMask.GetMask("Items");

        if (isServer)
        {
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();

            // 准备一个足够大的缓存（根据你场景中最大的交互物体数调整大小）
            Collider[] buffer = new Collider[16];

            // 调用 OverlapSphere，将碰撞体写入 buffer，返回命中数量
            int hitCount = physicsScene.OverlapSphere(
                transform.position,
                2f,
                buffer,
                interactableMask,
                QueryTriggerInteraction.UseGlobal
            );

            // 将有效结果复制到 objectsDetected
            objectsDetected = new Collider[hitCount];
            Array.Copy(buffer, objectsDetected, hitCount);
        }
        else
        {
            objectsDetected = Physics.OverlapSphere(transform.position, 2f, interactableMask);
        }


        GameObject objShortestDistance = null;
        foreach (var obj in objectsDetected)
        {
            NetworkIdentity netId = obj.GetComponent<NetworkIdentity>();
            if (netId != null && netId.netId != 0)
            {
                if (objShortestDistance == null)
                {
                    objShortestDistance = obj.gameObject;
                }
                else
                {
                    Vector3 colliderOffset = this.GetComponent<BoxCollider>().center;
                    Vector3 objShortestColliderOffset = objShortestDistance.GetComponent<BoxCollider>().center;

                    float newDist = Vector3.Distance(this.transform.position + colliderOffset,
                        obj.transform.position + colliderOffset);
                    float oldDist = Vector3.Distance(this.transform.position + colliderOffset,
                        objShortestDistance.transform.position + objShortestColliderOffset);

                    if (newDist < oldDist)
                    {
                        objShortestDistance = obj.gameObject;
                    }
                }
            }
        }

        objPlayerIsNear = objShortestDistance;
    }

    void HandleOutlineTimerLogic()
    {
        if (outlineShowTimer > 0)
        {
            outlineShowTimer -= Time.deltaTime;
            if (outlineShowTimer <= 0)
            {
                foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Player");
            }
        }

        if (outlineShowTimerLocal > 0)
        {
            outlineShowTimerLocal -= Time.deltaTime;
            if (outlineShowTimerLocal <= 0)
            {
                foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Player");
            }
        }
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
        if (isEnd)
            return;
        
        if (!isLocalPlayer)
            return;

        if (isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

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

        if (NetworkServer.active)
        {
            if (gameObject.scene.name == "Scene_3_1v1")
                ApplyBuoyancyForce();
        }
        else if (!NetworkServer.active)
        {
            if (SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
                ApplyBuoyancyForce();
        }
    }

    void Jump()
    {
        _animator.SetBool("Jumping", true);
        if (SoundController.Instance != null)
            SoundController.Instance.sfxSource_walk.Stop();

        // 跳跃时清除当前垂直速度（防止上次跳跃的速度干扰）
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpTimer = jumpTimeInterval;
    }

    bool CheckIfGrounded()
    {
        Collider[] objectsDetected;
        if (isServer)
        {
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();

            // 准备一个足够大的缓存（根据你场景中最大的交互物体数调整大小）
            Collider[] buffer = new Collider[16];

            // 调用 OverlapSphere，将碰撞体写入 buffer，返回命中数量
            int hitCount = physicsScene.OverlapSphere(
                transform.position,
                1f,
                buffer,
                LayerMask.GetMask("Ground") | LayerMask.GetMask("Interactable") | LayerMask.GetMask("Obstacles"),
                QueryTriggerInteraction.UseGlobal
            );

            // 将有效结果复制到 objectsDetected
            objectsDetected = new Collider[hitCount];
            Array.Copy(buffer, objectsDetected, hitCount);
        }
        else
        {
            objectsDetected = Physics.OverlapSphere(transform.position, 2f,
                LayerMask.GetMask("Ground") | LayerMask.GetMask("Interactable") | LayerMask.GetMask("Obstacles"));
        }

        if (objectsDetected.Length > 0)
            return true;
        return false;
    }

    void OnEquipItemChanged(string oldItem, string newItem)
    {
        if (itemsManager != null)
        {
            foreach (Transform item in equipItemSlot)
            {
                Destroy(item.gameObject);
            }

            if (newItem != "")
            {
                Transform newObj = Instantiate(itemsManager.items.transform.Find(newItem), equipItemSlot);
                newObj.transform.name = newItem;
                newObj.gameObject.SetActive(true);
            }
        }
    }

    void ApplyBuoyancyForce()
    {
        Buoyancy buoyancy = GetComponent<Buoyancy>();
        buoyancy.Forces.Clear();
        foreach (var point in buoyancy.Voxels)
        {
            buoyancy.ApplyBuoyancyForce(point);
        }
    }

    void PlayMovementSounds(float h, float v)
    {
        if (h > 0.1f || h < -0.1f || v > 0.1f || v < -0.1f)
        {
            if (SoundController.Instance != null)
            {
                if (NetworkServer.active)
                {
                    if (gameObject.scene.name != "PersistentScene")
                    {
                        if (gameObject.scene.name == "Scene_3_1v1")
                        {
                            if (isGrounded && !SoundController.Instance.IsSFXWalkPlaying())
                                SoundController.Instance.PlayFootstep_grass(0.5f, 2.2f);
                        }
                        else
                        {
                            if (isGrounded && !SoundController.Instance.IsSFXWalkPlaying() &&
                                rb.linearVelocity.y < 0.1f)
                                SoundController.Instance.PlayFootstep_floor(1.2f, 2.2f);
                        }
                    }
                }
                else
                {
                    if (SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
                    {
                        if (isGrounded && !SoundController.Instance.IsSFXWalkPlaying())
                            SoundController.Instance.PlayFootstep_grass(0.5f, 2.2f);
                    }
                    else
                    {
                        if (isGrounded && !SoundController.Instance.IsSFXWalkPlaying() && rb.linearVelocity.y < 0.1f)
                            SoundController.Instance.PlayFootstep_floor(1.2f, 2.2f);
                    }
                }
            }
        }
    }

    void HandlePaintingShootingEnable()
    {
        if (NetworkServer.active)
        {
            if (gameObject.scene.name == "Scene_5_Painting")
            {
                paintingShooting.enabled = true;
                gun.enabled = false;
            }
            else
            {
                paintingShooting.enabled = false;
                gun.enabled = true;
            }
        }
        else
        {
            if (SceneManager.GetSceneByName("Scene_5_Painting").isLoaded)
            {
                paintingShooting.enabled = true;
                gun.enabled = false;
            }
            else
            {
                paintingShooting.enabled = false;
                gun.enabled = true;
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnBullet(Vector3 position, Vector3 direction)
    {
        GameObject bullet = Instantiate(gun.bulletPrefab, position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDirection(direction);
        NetworkServer.Spawn(bullet);
        bullet.GetComponent<MeshRenderer>().enabled = true;
        bullet.transform.GetChild(0).GetComponent<TrailRenderer>().enabled = true;

        gun.shootingFire.GetComponent<ParticleSystem>()
            .Spawn(gun.shootingFirePoint, gun.shootingFirePoint.localPosition, Quaternion.identity);
    }

    [ClientRpc]
    public void RPCSpawnBulletFromHost(Vector3 position, Vector3 direction)
    {
        if (!isClientOnly)
            return;

        GameObject bullet = Instantiate(gun.bulletPrefab, position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDirection(direction);
        bullet.GetComponent<MeshRenderer>().enabled = true;
        bullet.transform.GetChild(0).GetComponent<TrailRenderer>().enabled = true;

        gun.shootingFire.GetComponent<ParticleSystem>()
            .Spawn(gun.shootingFirePoint, gun.shootingFirePoint.localPosition, Quaternion.identity);
    }

    [ClientRpc]
    public void RpcSetPainting(Vector3 direction, bool state)
    {
        if (!isClientOnly)
            return;

        if (state)
        {
            paintingShooting.inkParticle.transform.rotation = Quaternion.LookRotation(direction + Vector3.up * 0.05f);
            paintingShooting.inkParticle.Play();
        }
        else
        {
            paintingShooting.inkParticle.Stop();
        }
    }

    [ClientRpc]
    public void RpcSetPaintingDirection(Vector3 direction)
    {
        if (!isClientOnly)
            return;

        paintingShooting.inkParticle.transform.rotation = Quaternion.LookRotation(direction + Vector3.up * 0.05f);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPainting(Vector3 direction, bool state)
    {
        if (state)
        {
            paintingShooting.inkParticle.transform.rotation = Quaternion.LookRotation(direction + Vector3.up * 0.05f);
            paintingShooting.inkParticle.Play();
        }
        else
        {
            paintingShooting.inkParticle.Stop();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPaintingDirection(Vector3 direction)
    {
        paintingShooting.inkParticle.transform.rotation = Quaternion.LookRotation(direction + Vector3.up * 0.05f);
    }
}
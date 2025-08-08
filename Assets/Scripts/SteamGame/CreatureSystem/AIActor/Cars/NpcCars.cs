using UnityEngine;
using CleverCrow.Fluid.BTs.Trees;
using CleverCrow.Fluid.BTs.Tasks;
using System.Collections.Generic;
using Mirror;
using Random = UnityEngine.Random;

public class NpcCars : AIActor
{
    [Range(0.1f, 100f)] public float interactiveRadius = 10f;

    bool interative = false;

    WayPointNavigator wayPointNavigator;

    public float attackInterval = 0.5f;
    private float attackTimer = 0f;

    protected override void Start()
    {
        base.Start();

        InitAI();

        wayPointNavigator = GetComponent<WayPointNavigator>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (attackTimer <= 0)
            {
                if (NetworkServer.active)
                    other.transform.GetComponent<PlayerMovement>().AttackPlayerRpc(20f);
                else
                    other.transform.GetComponent<PlayerMovement>().AttackPlayerCmd(20f);
                attackTimer = attackInterval;
            }
        }
    }

    protected override void Update()
    {
        if (NetworkServer.active)
        {
            if (LobbyController.Instance.localPlayerObject != null)
            {
                if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_4")
                    return;
            }
        }

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        base.Update();

        UpdateInteractiveStatus();
        brain.Tick();
    }

    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            .Selector()
            .Do("Interact", () =>
            {
                if (interative)
                {
                    //ShowHeadBarUI();
                    return TaskStatus.Success;
                }
                else
                {
                    //HideHeadBarUI();
                    return TaskStatus.Failure;
                }
            })
            .Do("Update AI", () =>
            {
                Vector3 targetPos = Vector3.zero;
                List<WayPoint> waypoints = WayPointManager.Instance.GetWayPoints();

                if (waypoints != null && waypoints.Count > 0)
                {
                    if (!wayPointNavigator.HasPath)
                    {
                        Vector3 des = waypoints[Random.Range(0, waypoints.Count)].Position;
                        wayPointNavigator.SetDestination(des);
                    }

                    targetPos = Vector3.Scale(wayPointNavigator.currentWayPointPosition, new Vector3(1, 0, 1));
                }

                Vector3 acceleration = steeringBehaviors.Arrive(targetPos);

                // Avoidance
                if (collisionSensor != null)
                {
                    Vector3 acclerationDir = acceleration.normalized;
                    collisionSensor.GetCollisionFreeDirection(acclerationDir, out acclerationDir);
                    acclerationDir *= acceleration.magnitude;
                    acceleration = acclerationDir;
                }

                steeringBehaviors.LookMoveDirection();
                steeringBehaviors.Steer(acceleration);

                return TaskStatus.Success;
            })
            .End()
            .Build();
    }

    void UpdateInteractiveStatus()
    {
        ActorTypeFilter playerFilter = (actor) => actor is PlayerActor;

        List<Actor> targets = null;
        if (ActorManager.Instance != null)
        {
            targets = ActorManager.Instance.GetActorsWithinRange(this, transform.position, interactiveRadius,
                playerFilter);
        }

        if (targets != null && targets.Count > 0)
        {
            interative = true;
        }
        else
        {
            interative = false;
        }
    }
}
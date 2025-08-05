using UnityEngine;
using CleverCrow.Fluid.BTs.Trees;
using CleverCrow.Fluid.BTs.Tasks;
using System.Collections.Generic;
using Mirror;

public class NPCShip : AIActor
{
    [Range(0.1f, 100f)] public float interactiveRadius = 10f;

    bool interative = false;

    WayPointNavigator wayPointNavigator;

    protected override void Start()
    {
        base.Start();

        InitAI();

        wayPointNavigator = GetComponent<WayPointNavigator>();
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

                steeringBehaviors.Steer(acceleration);
                steeringBehaviors.LookMoveDirection();

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
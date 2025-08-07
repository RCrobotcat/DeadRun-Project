using System.Collections.Generic;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallPainter : AIActor
{
    public Actor attackTarget = null;

    [Header("Paint Settings")] public float minRadius = 0.05f;
    public float maxRadius = 0.2f;

    protected override void Start()
    {
        base.Start();

        InitAI();
    }

    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.layer == 3) // Layer 3 is "Ground"
        {
            var contacts = other.contacts;
            foreach (var contact in contacts)
            {
                if (contact.thisCollider == null) continue;
                if (contact.thisCollider.gameObject != gameObject) continue;

                Paintable paintable = other.gameObject.GetComponent<Paintable>();
                if (paintable != null)
                {
                    if (PaintManager.Instance != null)
                    {
                        float radius = Random.Range(minRadius, maxRadius);
                        PaintManager.Instance.paint(paintable, contact.point, radius, 1, 0.5f, Color.black);

                        SyncPaint(paintable, contact.point, radius, 1, 0.5f, Color.black);
                    }
                }
            }
        }
    }

    protected override void Update()
    {
        if (NetworkServer.active)
        {
            if (LobbyController.Instance.localPlayerObject != null)
            {
                if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_5_Painting")
                    return;
            }
        }

        base.Update();

        UpdateAttackTarget();

        if (animator != null)
            animator.SetFloat("Height", transform.position.y);
    }

    protected override void FixedUpdate()
    {
        if (NetworkServer.active)
        {
            if (LobbyController.Instance.localPlayerObject != null)
            {
                if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_5_Painting")
                    return;
            }
        }

        base.FixedUpdate();

        brain.Tick();
    }

    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            .Selector()
            .Sequence("Attack Branch")
            .Condition("Have Target?", () => { return HaveAttackTarget(); })
            .Selector("Try Attack")
            .Sequence("Attack Process")
            .Condition("In Attack Range?", () => { return isInAttackRange(attackTarget); })
            .Do("Attack", () => // attack behavior Leaf
            {
                DoAttack(attackTarget);
                return TaskStatus.Success;
            })
            .End()
            .Do("Pursuit", () => // pursuit behavior Leaf
            {
                DoPursuit(attackTarget);
                return TaskStatus.Success;
            })
            .End()
            .End()
            .Do("Wander", () => // wander behavior Leaf
            {
                DoGroundWander();
                return TaskStatus.Success;
            })
            .Build();
    }

    void DoGroundWander()
    {
        // if (animator != null)
        // {
        //     animator.SetBool("Attack", false);
        // }

        Vector3 acceleration = wanderBehaviors.GetSteering();

        if (collisionSensor != null)
        {
            Vector3 accelerationDir = acceleration.normalized;
            collisionSensor.GetCollisionFreeDirection(accelerationDir, out accelerationDir);
            accelerationDir *= acceleration.magnitude;
            acceleration = accelerationDir;
        }

        steeringBehaviors.Steer(acceleration);
        steeringBehaviors.LookMoveDirection();
    }

    void DoAttack(Actor actor)
    {
        if (actor == null) return;

        // if (animator != null)
        // {
        //     animator.SetBool("Attack", true);
        // }

        steeringBehaviors.Steer(Vector3.zero);
        steeringBehaviors.LookAtDirection(attackTarget.transform.position - transform.position);
    }

    void DoPursuit(Actor actor)
    {
        if (actor == null) return;

        //if (animator != null) animator.SetBool("Attack", false);

        Vector3 acceleration = pursueBehaviors.GetSteering(actor.GetRigidbody());

        if (collisionSensor != null)
        {
            Vector3 accelerationDir = acceleration.normalized;
            collisionSensor.GetCollisionFreeDirection(accelerationDir, out accelerationDir);
            accelerationDir *= acceleration.magnitude;
            acceleration = accelerationDir;
        }

        steeringBehaviors.Steer(acceleration);
        steeringBehaviors.LookMoveDirection();
    }

    bool HaveAttackTarget()
    {
        return attackTarget != null;
    }

    bool isInAttackRange(Actor actor)
    {
        if (actor == null) return false;
        return (Vector3.Distance(transform.position, actor.transform.position) < attackRadius)
               && (Vector3.Dot(actor.transform.position - transform.position, transform.forward) > 0);
    }

    void UpdateAttackTarget()
    {
        if (attackTarget)
        {
            if (!isInAttackRange(attackTarget))
            {
                attackTarget = null;
            }
        }

        if (attackTarget == null)
        {
            attackTarget = GetNearestAttackTargetInView();
        }
    }

    Actor GetNearestAttackTargetInView()
    {
        ActorTypeFilter filter = (actor) => actor is PlayerActor;

        List<Actor> actors = GetActorsInView(filter);

        if (actors.Count == 0) return null;

        actors.Sort((actorA, actorB) =>
        {
            float distanceA = Vector3.Distance(transform.position, actorA.transform.position);
            float distanceB = Vector3.Distance(transform.position, actorB.transform.position);
            return distanceA.CompareTo(distanceB);
        });

        return actors[0];
    }

    void SyncPaint(Paintable paintable, Vector3 pos, float radius, float hardness, float strength, Color color)
    {
        PlayerObjectController player = LobbyController.Instance.LocalPlayerObjectController;
        int paintableId = PaintablesManager.Instance.GetPaintableID(paintable);
        if (NetworkServer.active)
        {
            player.RpcSyncPaint(paintableId, pos, radius, hardness, strength, color);
        }
        else
        {
            player.CmdSyncPaint(paintableId, pos, radius, hardness, strength, color);
        }
    }
}
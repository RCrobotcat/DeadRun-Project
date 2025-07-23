using CleverCrow.Fluid.BTs.Trees;
using CleverCrow.Fluid.BTs.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class BlueMonster : AIActor
{
    public Actor attackTarget = null;

    protected override void Start()
    {
        base.Start();

        InitAI();
    }

    protected override void Update()
    {
        base.Update();

        UpdateAttackTarget();
    }

    protected override void FixedUpdate()
    {
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
                DoWander();
                return TaskStatus.Success;
            })
            .Build();
    }

    void DoWander()
    {
        if (animator != null)
        {
            animator.SetBool("Attack", false);
        }

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

        if (animator != null) animator.SetBool("Attack", true);

        steeringBehaviors.Steer(Vector3.zero);
        steeringBehaviors.LookAtDirection(attackTarget.transform.position - transform.position);
    }

    void DoPursuit(Actor actor)
    {
        if (actor == null) return;

        if (animator != null) animator.SetBool("Attack", false);

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
        return Vector3.Distance(transform.position, actor.transform.position) < attackRadius;
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
}
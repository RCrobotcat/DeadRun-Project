using CleverCrow.Fluid.BTs.Trees;
using CleverCrow.Fluid.BTs.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Mirror;
using UnityEngine.SceneManagement;

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
        if (NetworkServer.active)
            if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_3_1v1")
                return;

        base.Update();

        UpdateAttackTarget();

        if (animator != null)
            animator.SetFloat("Height", transform.position.y);
    }

    protected override void FixedUpdate()
    {
        if (NetworkServer.active)
            if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_3_1v1")
                return;

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
            .Selector("Try Wander")
            .Sequence("Wander Branch")
            .Condition("Sky or Ground?", () => { return GroundOrSkyPatrol(); })
            .Do("Sky Wander", () => // sky wander behavior Leaf
            {
                DoSkyWander();
                return TaskStatus.Success;
            })
            .End()
            .Do("Ground Wander", () => // ground wander behavior Leaf
            {
                DoGroundWander();
                return TaskStatus.Success;
            })
            .End()
            .Build();
    }

    void DoGroundWander()
    {
        if (animator != null)
        {
            animator.SetBool("Attack", false);
            animator.SetBool("Flying", false);
        }

        wanderBehaviors.targetHeight = 9f;
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

    void DoSkyWander()
    {
        if (animator != null)
        {
            animator.SetBool("Attack", false);
            animator.SetBool("Flying", true);
        }

        wanderBehaviors.targetHeight = 20f;
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

    bool GroundOrSkyPatrol()
    {
        float num = Random.value;
        if (num <= 0.7f)
            return true; // sky patrol
        return false; // ground patrol
    }

    void DoAttack(Actor actor)
    {
        if (actor == null) return;
        PlayerMovement target = actor.GetComponent<PlayerMovement>();

        if (animator != null)
        {
            animator.SetBool("Attack", true);
            animator.SetBool("Flying", false);
        }

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

    public void AttackPlayerAnimationEvent()
    {
        if (SoundController.Instance != null)
        {
            if (NetworkServer.active)
            {
                if (attackTarget.gameObject.scene.name == "Scene_3_1v1")
                {
                    SoundController.Instance.PlaySFX_others(SoundController.Instance.sfxClip_monsterAttack, 0.4f);
                }
            }
            else
            {
                if (SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
                {
                    SoundController.Instance.PlaySFX_others(SoundController.Instance.sfxClip_monsterAttack, 0.4f);
                }
            }
        }

        if (Vector3.Distance(attackTarget.transform.position, transform.position) < attackRadius)
        {
            if (Vector3.Dot(attackTarget.transform.position - transform.position, transform.forward) < 0)
                return;
            
            if (NetworkServer.active)
            {
                if (attackTarget.gameObject.scene.name == "Scene_3_1v1")
                {
                    PlayerMovement player = attackTarget.GetComponent<PlayerMovement>();
                    player.MonsterAttackPlayer(5);
                }
            }
            else
            {
                if (SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
                {
                    PlayerMovement player = attackTarget.GetComponent<PlayerMovement>();
                    player.MonsterAttackPlayer(5);
                }
            }
        }
    }
}
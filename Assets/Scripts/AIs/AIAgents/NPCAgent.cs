using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;

//AI控制器基类，本质是一个FSM，每个节点可以是一个behavior tree
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorTree))]
[RequireComponent(typeof(BehaviorManager))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCAgent : MonoBehaviour
{
    BehaviorTree[] behaviorTrees;
    BehaviorManager behaviorManager;

    protected NavMeshAgent navMeshAgent;
    protected Rigidbody rb;

    public NavMeshAgent NavMeshAgent { get { return navMeshAgent; } }
    public Rigidbody Rigidbody { get { return rb; } }

    bool isActive = true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        behaviorTrees = GetComponents<BehaviorTree>();
        behaviorManager = GetComponent<BehaviorManager>();
        behaviorManager.UpdateInterval = UpdateIntervalType.Manual;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance.GetGameAction() && isActive)
        {
            behaviorManager.Tick();
        }
    }

    public void ActivateAI()
    {
        if (!isActive)
        {
            foreach (var tree in behaviorTrees)
                tree.EnableBehavior();
        }
        isActive = true;
    }
    public void DeactivateAI()
    {
        if (isActive)
        {
            foreach (var tree in behaviorTrees)
                tree.DisableBehavior(); // pause is false -- the tree will end
        }
        isActive = false;
    }
    public void ResetState() { }
}
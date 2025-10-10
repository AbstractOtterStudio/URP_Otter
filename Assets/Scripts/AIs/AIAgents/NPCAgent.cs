using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;

//AI控制器基类，本质是一个FSM，每个节点可以是一个behavior tree
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorManager))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCAgent : MonoBehaviour
{
    BehaviorManager BehaviorMgr;
    protected NavMeshAgent navMeshAgent;
    protected Rigidbody rb;

    public NavMeshAgent NavMeshAgent { get { return navMeshAgent; } }
    public Rigidbody Rigidbody { get { return rb; } }

    bool isActive = true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        BehaviorMgr = GetComponent<BehaviorManager>();
        BehaviorMgr.UpdateInterval = UpdateIntervalType.Manual;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance.GetGameAction() && isActive)
        {
            BehaviorMgr.Tick();
        }
    }

    public void ActivateAI()
    {
        if (!isActive)
        {
            foreach (var tree in BehaviorMgr.BehaviorTrees)
                BehaviorMgr.EnableBehavior(tree.behavior);
        }
        isActive = true;
    }
    public void DeactivateAI()
    {
        if (isActive)
        {
            foreach (var tree in BehaviorMgr.BehaviorTrees)
                BehaviorMgr.DisableBehavior(tree.behavior); // pause is false -- the tree will end
        }
        isActive = false;
    }
    public void ResetState() { }
}
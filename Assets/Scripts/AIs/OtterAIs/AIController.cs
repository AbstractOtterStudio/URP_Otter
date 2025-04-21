using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIController : MonoBehaviour
{
    public bool IsTeammate; // set by GameManager
    public Transform Ball     { get; private set; }
    public Transform FriendlyGoal { get; private set; }
    public Transform EnemyGoal    { get; private set; }

    public Vector3 HomePosition { get; private set; }
    public bool InHomeArea => Vector3.Distance(transform.position, HomePosition) < 2f;
    public bool HasBall => Vector3.Distance(transform.position, Ball.position) < 1.2f;
    public float DistanceToBall => Vector3.Distance(transform.position, Ball.position);

    private AIState current;
    private Rigidbody rb;
    private Animator anim;

    [Header("Movement")]
    public float swimSpeed = 2.8f;
    public float sprintSpeed = 5f;
    public float awarenessRadius = 6f;

    [Header("sprint control")]
    public float sprintDuration = 0.8f;
    private bool  isSprinting;
    private float sprintTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    public void Init(Transform ball, Transform friendlyGoal, Transform enemyGoal, Vector3 home)
    {
        Ball = ball;
        FriendlyGoal = friendlyGoal;
        EnemyGoal = enemyGoal;
        HomePosition = home;
        ChangeState(new IdleState(this));
    }

    void Update()
    {
        if (isSprinting)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0f)
            {
                isSprinting = false;
                anim?.SetBool("IsSprinting", false);
            }
        }

        current?.Update();
    }

    public void ChangeState(AIState next)
    {
        current?.Exit();
        current = next;
        current?.Enter();
    }

    // -------- ACTION HELPERS --------
    public void MoveToward(Vector3 target)
    {
        Vector3 dir = (target - transform.position); dir.y = 0;
        if (dir.sqrMagnitude < .1f) return;
        dir = dir.normalized;
        float speed = current is ChaseBallState || current is InterruptState ? sprintSpeed : swimSpeed;
        Vector3 desired = dir * speed;
        Vector3 change = desired - new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(change, ForceMode.Acceleration);

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public void StartSprint()
    {
        if (isSprinting) return;
        isSprinting = true;
        sprintTimer = sprintDuration;
        anim?.SetBool("IsSprinting", true);
    }

    public bool LineClear(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float len  = dir.magnitude;
        dir.Normalize();
        // SphereCast for slightly widened path checking
        if (Physics.SphereCast(from + Vector3.up*0.5f, 0.35f, dir, out RaycastHit hit, len))
        {
            return hit.collider.CompareTag("GoalBlue") || hit.collider.CompareTag("GoalRed");
        }
        return true;
    }

    public void Shoot(Vector3 target)
    {
        if (!HasBall) return;
        Rigidbody brb = Ball.GetComponent<Rigidbody>();
        Vector3 dir = (target - Ball.position).normalized;
        brb.AddForce(dir * 8f, ForceMode.Impulse);
    }

    public void PassToTeammate()
    {
        AIController[] all = FindObjectsOfType<AIController>();
        List<AIController> mates = new List<AIController>();
        foreach (var a in all)
        {
            if (a != this && a.IsTeammate)
                mates.Add(a);
        }
        if (mates.Count == 0) return;
        AIController mate = mates[Random.Range(0, mates.Count)];
        Shoot(mate.transform.position + Vector3.up * 0.3f);
    }

    public float DistanceTo(Vector3 pos) => Vector3.Distance(transform.position, pos);
}

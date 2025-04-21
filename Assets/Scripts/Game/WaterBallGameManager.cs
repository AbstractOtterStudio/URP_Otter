using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBallGameManager : MonoBehaviour
{
    public Transform ball;
    public Transform goalBlue, goalRed;
    public AIController[] teamBlue; // size 2 (AI teammates of player)
    public AIController[] teamRed;  // size 3 opponents
    public Transform playerSpawn, blueSpawn1, blueSpawn2, redSpawn1, redSpawn2, redSpawn3;

    private Vector3 ballStartPos;

    void Start()
    {
        ballStartPos = ball.position;

        // player is assumed already spawned at playerSpawn
        InitAI(teamBlue, true, blueSpawn1.position, blueSpawn2.position);
        InitAI(teamRed,  false, redSpawn1.position,  redSpawn2.position, redSpawn3.position);
    }

    void InitAI(AIController[] bots, bool isBlue, params Vector3[] homes)
    {
        for(int i=0;i<bots.Length && i<homes.Length;i++)
        {
            bots[i].transform.position = homes[i];
            bots[i].IsTeammate = isBlue;
            bots[i].Init(ball, isBlue?goalBlue:goalRed, isBlue?goalRed:goalBlue, homes[i]);
        }
    }

    public void GoalScored(bool blueScored)
    {
        Debug.Log($"Goal!! {(blueScored?"Blue":"Red")} team scores");
        ResetPositions();
    }

    void ResetPositions()
    {
        ball.position = ballStartPos;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        InitAI(teamBlue, true, blueSpawn1.position, blueSpawn2.position);
        InitAI(teamRed,  false, redSpawn1.position,  redSpawn2.position, redSpawn3.position);
        // TODO reset players to spawns similarly
    }
}

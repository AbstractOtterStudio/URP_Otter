using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBallGameManager : MonoBehaviour
{
    [Header("Refs")] public Ball ball; public Transform ballStart;
    public WaterPlayer[] blueTeam; public WaterPlayer[] redTeam;
    int scoreBlue, scoreRed;
    void Start(){
        foreach(var p in blueTeam){ p.team=new List<WaterPlayer>(blueTeam); p.opponents=new List<WaterPlayer>(redTeam); }
        foreach(var p in redTeam ){ p.team=new List<WaterPlayer>(redTeam ); p.opponents=new List<WaterPlayer>(blueTeam); }
    }
    public void GoalScored(string team){ if(team=="Blue") scoreBlue++; else scoreRed++; Debug.Log($"GOAL {team}! {scoreBlue}-{scoreRed}"); ResetPlay(); }
    void ResetPlay(){ ball.Pos = ballStart.position; ball.Rb.velocity=Vector3.zero; }
}

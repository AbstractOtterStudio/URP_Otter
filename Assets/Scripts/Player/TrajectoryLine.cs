using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    
    float _timestep { get; set; }

    public float Strength { get; private set; }
    public float FlightTime { get; private set; }
    public Vector3 StartPos { get; private set; }
    public Vector3 EndPos => StartPos + FlightTime * InitVel + Physics.gravity * 0.5f * FlightTime * FlightTime;
    public Vector3 InitVel { get; private set; }
    public bool HasTrajectory { get; private set; }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    public void MakeTrajectory(Vector3 startPos, Vector3 fwd, float strength, float mass)
    {
        HasTrajectory = true;
        StartPos = startPos;
        Strength = strength;

        InitVel = fwd * Strength / mass;
        lineRenderer.positionCount = 50;

        FlightTime = 2 * InitVel.y / -Physics.gravity.y;
        var timestep = FlightTime / lineRenderer.positionCount;

        for (int i = 0; i < lineRenderer.positionCount; ++i)
        {
            var t = i * timestep;
            var p = StartPos + t * InitVel;
            p.y = StartPos.y + InitVel.y * t + Physics.gravity.y * 0.5f * t * t;
            lineRenderer.SetPosition(i, p);
        }
    }

    public void FuckOff()
    {
        HasTrajectory = false;
        lineRenderer.positionCount = 0;
    }

    public void SetColor(Color color)
    {
        lineRenderer.startColor = new Color(color.r, color.g, color.b, 0.5f);
        lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.5f);
    }

    public IEnumerable<Vector3> TrajectoryPoints()
    {
        if (lineRenderer.positionCount == 0)
        {
            yield break;
        }

        for (int i = 0; i < lineRenderer.positionCount; ++i)
        {
            var t = i * _timestep;
            var p = StartPos + t * InitVel;
            p.y = StartPos.y + InitVel.y * t + Physics.gravity.y * 0.5f * t * t;
            yield return p;
        }
    }
}
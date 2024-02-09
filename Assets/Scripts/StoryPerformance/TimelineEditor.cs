using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineEditor : MonoBehaviour
{
    public PlayableDirector director;

    private bool isWaitingForInput = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaitingForInput && Input.GetKeyDown(KeyCode.Space))
        {
            ResumeTimeline();
        }
    }

    public void PauseTimeline()
    {
        if (director.state == PlayState.Playing)
        {
            director.Pause();
            isWaitingForInput = true;
        }
    }

    public void ResumeTimeline()
    {
        if (director.state == PlayState.Paused)
        {
            director.time += 0.01;
            director.Resume();
            isWaitingForInput = false;
        }
    }
}

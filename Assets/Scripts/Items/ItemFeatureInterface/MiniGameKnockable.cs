using UnityEngine;

public class MiniGameKnockable : Knockable, QTEMiniGame.IHandler
{

    public void OnMiniGameStart(QTEMiniGame miniGame)
    {
    }

    public void OnMiniGameUserInput(QTEMiniGame.Result result, out bool shouldContinue)
    {
        if (result.zoneHit == QTEMiniGame.Zone.TargetZone)
        {
            currKnockNum++;
            shouldContinue = currKnockNum < maxKnockNum;
            if (currKnockNum == maxKnockNum)
            {
                OnBreak();
            }
        }
        else if (result.zoneHit == QTEMiniGame.Zone.BullseyeZone)
        {
            currKnockNum = maxKnockNum;
            OnBreak();
            shouldContinue = false;
        }
        else
        {
            shouldContinue = true;
        }
    }

    public void OnMiniGameEnd()
    {
    }
}

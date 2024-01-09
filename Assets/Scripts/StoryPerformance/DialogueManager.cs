using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public string[] sequences = {"123", "231", "312"};
    public float transitionSpeed = 1f;
    public float waitSpeed = 3f;

    private SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        DialogueTrigger();
    }

    void DialogueTrigger()
    {
        //play dialogue
        //wait for input
        //play next dialogue
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        StartCoroutine(DisplaySequence(sequences));
    }

    private IEnumerator DisplaySequence(string[] sequences)
    {
        foreach (string sequence in sequences)
        {
            List<GameObject> spriteObjs = new List<GameObject>();
            foreach (char spriteId in sequence)
            {
                string spriteName = spriteId.ToString();
                GameObject spriteObj = SpriteManager.Instance.CreateSpriteObject(transform, int.Parse(spriteId.ToString()));
                spriteObjs.Add(spriteObj);

                // Optionally do something with spriteObj, like fade in

                yield return new WaitForSeconds(transitionSpeed);
            }
            yield return new WaitForSeconds(waitSpeed);
            for (int i = 0; i < spriteObjs.Count; i++)
            {
                GameObject spriteObj = spriteObjs[i];
                Destroy(spriteObj);
                //yield return new WaitForSeconds(transitionSpeed);
            }
        }
        //fade out the bubble as well
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
    }
}

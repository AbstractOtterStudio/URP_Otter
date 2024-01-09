using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Instance {get; private set;}

    //sprite that we are going to use
    public Sprite[] sprites;
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;            
        }
    }

    public GameObject CreateSpriteObject(Transform parent, int spriteId)
    {
        if (spriteId < 0 || spriteId > sprites.Length)
        {
            Debug.LogError("Sprite ID out of range: " + spriteId);
            return null;
        }
        Sprite sprite = sprites[spriteId-1];
        GameObject newSpriteObj = new GameObject((spriteId).ToString());
        newSpriteObj.transform.SetParent(parent); // Set as child of provided parent
        newSpriteObj.transform.localPosition = new Vector3(0f, 0f, -0.2f); // Position it at the parent's origin, but closer to the cam
        newSpriteObj.transform.localRotation = Quaternion.identity; // No rotation
        newSpriteObj.layer = LayerMask.NameToLayer("UI"); // Set the layer to UI so it renders on top

        // Add a SpriteRenderer component and set the sprite
        SpriteRenderer renderer = newSpriteObj.AddComponent<SpriteRenderer>();
        LayoutElement layoutElement = newSpriteObj.AddComponent<LayoutElement>();
        renderer.sprite = sprite;

        return newSpriteObj;
    }
}

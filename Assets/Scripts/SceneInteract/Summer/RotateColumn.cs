using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateColumn : MonoBehaviour
{
    [SerializeField]
    private List<Transform> fruits = new List<Transform>();
    [SerializeField]
    private Transform[] fruitPos = new Transform[4];
    [SerializeField] private bool isActive;
    [SerializeField] private bool hasUrchin;
    [SerializeField] private float rotateSpeed;
    [SerializeField] private GameObject urchin;

    [SerializeField] private bool hasNewFruit;
    [SerializeField] private Material stoneMaterial;
    [SerializeField] private Transform playerTransform;

    private float m_LerpMaterail;
    private bool m_isGold;

    private PlayerHand playerHand;
    void Start()
    {
        urchin.SetActive(false);
        if (playerTransform == null)
        {
            playerTransform = FindObjectOfType<PlayerStateController>().transform;
        }

        if (playerTransform == null)
        {
            Debug.LogError("Dont have PLayer in the scene !");
        }

        if (fruits.Count < 4)
        {
            hasUrchin = true;
        }

        for (int i = 0; i < fruits.Count; i++)
        {
            fruits[i].position = fruitPos[i].position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        FruitsRotate();
        DetectFruitActive();
        StoneToGolden();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerHand>())
        {
            playerHand = other.GetComponent<PlayerHand>();
            if (playerHand.grabItemInHand == null)
            {
                isActive = true;
                return;
            }

            if (playerHand.grabItemInHand != null 
            && playerHand.grabItemInHand.IsCollection)
            {
                Transform fruit = playerHand.grabItemInHand.transform;
                fruits.Add(fruit);                
                fruit.SetParent(this.transform);
                playerHand.grabItemInHand.DeactivatePicker();                         
                playerHand.grabItemInHand = null;
                hasNewFruit = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerMovement>())
        {
            isActive = false;
            playerHand = null;
        }
    }

    private void FruitsRotate()
    {
        if (isActive)
        {
            //rotateSpeed = Mathf.Lerp(, Mathf.Clamp01( * Time.deltaTime));
            foreach (Transform fruit in fruits)
            {	
                fruit.RotateAround(this.transform.position, Vector3.up, rotateSpeed * Time.deltaTime);
            }
        }
    }

    private void DetectFruitActive()
    {
        if (!hasNewFruit) { return; }
        
        hasNewFruit = false;
        for (int i = 0; i < fruits.Count; i++)
        {
            if (Vector3.Distance(fruits[i].position,fruitPos[i].position) > 0.1f)
            {
                hasNewFruit = true;
                // fruits[i].position = fruitPos[i].position;
                fruits[i].position = Vector3.Lerp(fruits[i].position,fruitPos[i].position,Time.deltaTime * 2);
            }
        }

        if (!hasNewFruit)
        {
            if (playerHand != null)
            {
                isActive = true;
            }

            if (hasUrchin && fruits.Count == fruitPos.Length)
            {
                urchin.SetActive(true);
                urchin.GetComponent<Item_Urchin>().UrchinSpawn(urchin.transform,playerTransform);
                m_isGold = true;
            }
        }
    }

    private void StoneToGolden()
    {
        if (m_isGold)
        {
            m_LerpMaterail += Time.deltaTime;
            if (m_LerpMaterail <= 0.8f)
            {
                stoneMaterial.SetFloat("LerpValue", m_LerpMaterail);
            }
            else
            {
                m_isGold = false;
                m_LerpMaterail = 0;
            }
        }
    }
}

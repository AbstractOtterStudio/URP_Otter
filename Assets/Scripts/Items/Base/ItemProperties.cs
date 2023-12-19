using System;
using UnityEngine;

//Item Properties, also as a Mediator to Attr Components
[RequireComponent(typeof(Eatable))]
[RequireComponent(typeof(Knockable))]
[RequireComponent(typeof(Catchable))]
public class ItemProperties : MonoBehaviour
{
    #region AI
    [SerializeField] bool haveAI = false;
    IAgent m_AiAgent;
    #endregion

    #region Food Attr
    [SerializeField] bool canEat;
    public bool CanEat { get { return canEat; } }
    Eatable eatAttr;
    #endregion

    #region Hard Item Attr
    [SerializeField] bool canKnock;
    Knockable knockAttr;
    public bool CanKnock { get { return canKnock; } }
    public bool IsBroken    
    {
        get
        {
            if(CanKnock)
            {
                return knockAttr.IsBroken;
            }
            return true;
        }
    }
    #endregion

    #region Prey Attr
    [SerializeField] bool canCatch;
    protected Catchable catchAttr;
    public bool CanCatch { get { return canCatch; } }
    public bool IsCollection
    {
        get
        {
            if(CanCatch)
            {
                return catchAttr.IsCollection();
            }
            return false;
        }
    }
    #endregion

    public Vector3 spawnPos;
    Quaternion spawnRot;
    public Vector3 euler;

    void Awake()
    {
        if (haveAI) { m_AiAgent = GetComponent<IAgent>(); }
        if (canEat) { eatAttr = gameObject.GetComponent<Eatable>(); }        
        if (canKnock) { knockAttr = gameObject.GetComponent<Knockable>(); }        
        if (canCatch) { catchAttr = gameObject.GetComponent<Catchable>(); }

    }

    #region AI Method
    public void ActivateAI()
    {
        if(haveAI)
        {
            m_AiAgent.ActivateAI();
        }
    }
    #endregion

    public void Knock()
    {
        if (!canKnock) { return; }
        knockAttr.OnBreak();
    }

    public void KnockWith(ItemProperties item) 
    {
        if (IsBroken || !canKnock) { return; }
        Debug.Log($"Try To Break With: {item.gameObject.name}; Using {gameObject.name}");
        item.Knock();
        Knock();
    }

    public (float experience, float health) Eat()
    {        
        if (!canEat || !knockAttr.IsBroken)
        {
            (float experience, float health) nutrition;
            nutrition.experience = 0;
            nutrition.health = 0;
            return nutrition;
        }
        return eatAttr.GetFoodNutrition();
    }

    public void Catch(Transform catcher)
    {
        if (!canCatch) { return; }
        catchAttr.OnCatch(catcher);
        if (haveAI)
        {
            GetComponent<EscaperAgent>().DeactivateAI();
        }
    }
    public void Release()
    {
        if (!canCatch) { return; }
        catchAttr.OnRelease();
        if (haveAI)
        {
            GetComponent<EscaperAgent>().ActivateAI();
        }
    }  
    public void DeactivatePicker() { canCatch = false; }

    public void ResetProperties()
    {
        if (!haveAI)
        {
            transform.position = spawnPos;
            transform.rotation = spawnRot;
        }        
        knockAttr.ResetKnockTime();
    }

    public void InitProperties()
    {
        if (!haveAI)
        {
            spawnPos = transform.position;
            spawnRot = transform.rotation;
            euler = spawnRot.eulerAngles;
        }
    }
}
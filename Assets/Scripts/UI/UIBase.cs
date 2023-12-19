using UnityEngine;
using UnityEngine.UI;

//UI基類
public abstract class UIBase : MonoBehaviour     
{
    //[SerializeField] protected GameEvents valueChangeEvent;

    //public virtual void Init()        
    //{                
    //    EventCenter.AddListener<float>(valueChangeEvent, ValueChange);
    //}

    public abstract void Init();

    //protected virtual void OnDestroy() 
    //{
    //    EventCenter.RemoveListener<float>(valueChangeEvent, ValueChange);
    //}

    //protected abstract void ValueChange(float value);
}
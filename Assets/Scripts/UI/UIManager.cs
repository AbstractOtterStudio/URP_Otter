using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonBase<UIManager>
{
    UIBase[] _uis;
    private QTEMiniGame _qteMiniGame;

    public void Init() // TODO: 把所有Init()都去掉，singleton逻辑封装一下，包括dontdestroyonload
    {
        if (instance == null) { instance = this; }
        else { Debug.LogError("UIManager instance already exists"); }

        _uis = FindObjectsOfType<UIBase>();

        foreach (UIBase ui in _uis)
        {
            Debug.Log($"UI Manager: Initializing {ui.GetType().Name} on {ui.gameObject.name}");
            ui.Init();

            if (ui is QTEMiniGame qteMiniGame)
            {
                if (_qteMiniGame == null)
                {
                    _qteMiniGame = qteMiniGame;
                }
                else
                {
                    Debug.LogError($"Only one QTE Mini Game instance is allowed, ignoring {qteMiniGame.gameObject.name}");
                }
            }
        }
    }

    public void ActivateQTEMiniGame(QTEMiniGame.IHandler handler)
    {
        if (_qteMiniGame != null)
        {
            _qteMiniGame.Activate(handler);
        }
        else
        {
            Debug.LogError("QTE Mini Game instance not found");
        }
    }
}

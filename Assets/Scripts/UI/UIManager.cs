using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    UIBase[] _uis;
    private QTEMiniGame _qteMiniGame;

    protected override void Awake()
    {
        base.Awake();

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

// WaterPlayerManager.cs
using System.Collections.Generic;
using UnityEngine;

public static class WaterPlayerManager
{
    // 所有存活的 WaterPlayer（由 WaterPlayer 自己在 Start/OnDestroy 调用）
    private static readonly List<WaterPlayer> _all = new();
    public static IReadOnlyList<WaterPlayer> All => _all;

    public static void Register(WaterPlayer p)
    {
        if (p && !_all.Contains(p)) _all.Add(p);
    }

    public static void Unregister(WaterPlayer p)
    {
        _all.Remove(p);
    }

    public static void PauseAll()
    {
        foreach (var p in _all) if (p) p.Pause();
    }

    public static void ResumeAll()
    {
        foreach (var p in _all) if (p) p.Resume();
    }
}

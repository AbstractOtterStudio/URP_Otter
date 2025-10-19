#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Griffin
{
    public interface IRadialSelectorProvider<T>
    {
        T[] GetItemValues(object context);
        string GetItemLabel(T value);
        string GetItemDescription(T value);
        Texture2D GetIcon(T value);
    }
}
#endif
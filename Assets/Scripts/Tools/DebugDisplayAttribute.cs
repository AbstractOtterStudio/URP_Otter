using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// see Editor/CommonEditor for usage
public class DebugDisplayAttribute : System.Attribute
{
    public bool editable = false;
    public DebugDisplayAttribute(bool editable = false)
    {
        this.editable = editable;
    }
}
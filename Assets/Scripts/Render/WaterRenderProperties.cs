using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterRenderProperties
{
    public static Vector2 ShorelineDownsampling = new Vector2(1f, 1f);
    public static Vector2 RefractionDownsampling = Vector2.one;
    public static string ShorelineMaxDepthName = "_ShorelineMaxDepth";
    public static int ShorelineMaxDepthID = Shader.PropertyToID(ShorelineMaxDepthName);
    public static string ShorelineExpansionStartName = "_ShorelineExpansionStart";
    public static int ShorelineExpansionStartID = Shader.PropertyToID(ShorelineExpansionStartName);
    public static string ShorelineExpansionEndName = "_ShorelineExpansionEnd";
    public static int ShorelineExpansionEndID = Shader.PropertyToID(ShorelineExpansionEndName);
    public static string ShorelineBufferName = "_ShorelineBuffer";
    public static int ShorelineBufferID = Shader.PropertyToID(ShorelineBufferName);

    public static string WaterDepthBufferName = "_WaterDepthBuffer";
    public static int WaterDepthBufferID = Shader.PropertyToID(WaterDepthBufferName);

    public static string UnderWaterBufferName = "_UnderWaterBuffer";
    public static int UnderWaterBufferID = Shader.PropertyToID(UnderWaterBufferName);

    public static string UnderWaterDepthBufferName = "_UnderWaterDepthBuffer";
    public static int UnderWaterDepthBufferID = Shader.PropertyToID(UnderWaterDepthBufferName);
}

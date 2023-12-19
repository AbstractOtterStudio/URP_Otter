Shader "Unlit/EdgeDetection"

{
    Properties

    {
        
        _MainTex ("Texture", 2D) = "white" {}
        _Mask("_Mask",2D) = "white"{}
        _EdgeOnly ("Edge Only", Range(0,3)) = 1.0
        _EdgeColor ("Edge Color", Color) = (0, 0, 0, 1)

    }

    SubShader

    {
        Tags { "RenderType"="Opaque" }

        LOD 100

 

        Pass

        {
 

            ZTest Always

            Cull Off

            ZWrite Off

 

            CGPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            // make fog work

            #pragma multi_compile_fog

            

            #include "UnityCG.cginc"

 

            struct appdata

            {
                float4 vertex : POSITION;

                float2 uv : TEXCOORD0;

            };

 

            struct v2f

            {
                half2 uv[9] : TEXCOORD0;

                float4 pos : SV_POSITION;

            };

 

            sampler2D _MainTex;
            sampler2D _Mask;

            sampler2D _CameraColorTexture;
            //是Unity为我们提供的方位XXX纹理对应的每个纹素的大小。

            half4 _Mask_TexelSize;

            fixed _EdgeOnly;

            fixed4 _EdgeColor;


 

            

            v2f vert (appdata_img v)

            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                half2 uv = v.texcoord;

                //我们在v2f结构体中定义了一个维数为9的纹理数组，对应了使用Sobel算子采样时需要的9个邻域纹理坐标。通国把计算采样纹理坐标的代码从片元着色器中转移到顶点着色器中，可以减少运算，提高性能。由于从顶点着色器到片元着色器的插值是线性的，因此这样的转移并不会影响纹理坐标的计算结果。

                o.uv[0] = uv + _Mask_TexelSize.xy * half2(-1, -1);

                o.uv[1] = uv + _Mask_TexelSize.xy * half2(0, -1);

                o.uv[2] = uv + _Mask_TexelSize.xy * half2(1, -1);

                o.uv[3] = uv + _Mask_TexelSize.xy * half2(-1, 0);

                o.uv[4] = uv + _Mask_TexelSize.xy * half2(0, 0);

                o.uv[5] = uv + _Mask_TexelSize.xy * half2(1, 0);

                o.uv[6] = uv + _Mask_TexelSize.xy * half2(-1, 1);

                o.uv[7] = uv + _Mask_TexelSize.xy * half2(0, 1);

                o.uv[8] = uv + _Mask_TexelSize.xy * half2(1, 1);

                return o;

            }

            fixed luminance(fixed4 color) {
                return  0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b; 

            }

            

            half Sobel(v2f i) {
                //我们首先定义了水平方向和竖直方向使用的卷积核Gx和Gy。接着我们依次对9个像素进行采样，计算他们的亮度值，再与卷积核Gx和Gy中对应的权重相乘后，叠加到各自的梯度值上。最后，我们从1中减去水平方向和竖直方向的梯度值的绝对值，得到edge。edge值越小，表明该位置越可能是一个边缘检测点。

                const half Gx[9] = {-1,  0,  1,

                                        -2,  0,  2,

                                        -1,  0,  1};

                const half Gy[9] = {-1, -2, -1,

                                        0,  0,  0,

                                        1,  2,  1};     

                

                half texColor;

                half edgeX = 0;

                half edgeY = 0;

                for (int it = 0; it < 9; it++) {
                    texColor = luminance(tex2D(_Mask, i.uv[it]));

                    edgeX += texColor * Gx[it];

                    edgeY += texColor * Gy[it];

                }

                

                half edge = 1 - abs(edgeX) - abs(edgeY);

                

                return edge;

            }

            fixed4 frag (v2f i) : SV_Target
            {
                //首先调用Sobel函数计算当前像素的梯度值edge,并利用该值分别计算了背景为原图和纯色下的颜色值，然后利用_EdgeOnly在两者之间插值得到最终的像素值。Sobel函数将利用Sobel算子对原图进行边缘检测


                half edge = Sobel(i);
                //edge = edge * (1- tex2D(_Mask,i.uv[4]).g);
                //float mask = tex2D(_Mask,i.uv[4]).g;
                //fixed4 withEdgeColor = lerp(_EdgeColor, tex2D(_MainTex, i.uv[4]), clamp(0,1,edge- (_EdgeOnly * -1))) ;
                fixed4 withEdgeColor = lerp(_EdgeColor, tex2D(_CameraColorTexture, i.uv[4]), clamp(0,1,edge- (_EdgeOnly * -1))) ;
                
                //fixed4 mainTexColor= tex2D(_MainTex,i.uv[4])* mask;
                
                //return  tex2D(_MainTex, i.uv[4]);
                
                return withEdgeColor ;

            }

            ENDCG

        }

    }

}

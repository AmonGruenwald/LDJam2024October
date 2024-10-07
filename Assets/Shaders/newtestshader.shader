Shader "Custom/newtestshader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScrollSpd ("ScrollSpd", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 maskUV : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            float _Alpha;
            float4 _ClipRect;
            float4 _MaskScale;
            float4 _MaskOffset;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.worldPosition;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Calculate mask UV with custom scale and offset
                o.maskUV = v.uv * _MaskScale.xy + _MaskOffset.xy;
                
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Apply clip rect for proper UI masking
                float2 clipPosition = i.worldPosition.xy;
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_MaskTex, i.maskUV);
                
                col *= i.color;
                col.a *= round(clamp(mask.a - _Alpha, 0, 1) + 0.5);
                
                col.a *= UnityGet2DClipping(clipPosition, _ClipRect);
                
                return float4(1,0,0,0.5);
            }
            ENDCG
        }
    }
}
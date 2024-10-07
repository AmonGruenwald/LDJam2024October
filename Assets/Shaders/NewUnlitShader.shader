Shader "Unlit/NewUnlitScrollShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Texture", 2D) = "black" {}
        _ScrollSpd ("ScrollSpd", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { 
            "Queue"="Transparent"
            "RenderType"="Transparent" }
        
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag transparent
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            sampler2D _MainTex;
            sampler2D _MaskTex;
            float random(float2 st) {return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);}
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            float2 getRotatedUV(float2 iuv, float rotationSpeed, float2 scrollSpeed, float2 pivot){
                // Calculate the angle based on time
                float angle = _Time.y * rotationSpeed;
                // Create a 2D rotation matrix
                float2x2 rotationMatrix = float2x2(
                    cos(angle), -sin(angle),
                    sin(angle), cos(angle)
                );
                float2 uv = iuv - float2(0.5, 0.5);
                uv = mul(rotationMatrix, uv);
                uv += pivot;
                uv += frac(_Time.y * scrollSpeed); 
                return uv;
            }
            fixed4 frag (v2f i) : SV_Target
            {
        // Define different rotation parameters
        float2 uv1 = getRotatedUV(i.uv, 0.5, float2(0.1,0.1), float2(0.5,0.5));
        float2 uv2 = getRotatedUV(i.uv, -1.0, float2(-0.21,0.21),float2(0.57,-0.75));
        float2 uv3 = getRotatedUV(i.uv, 0.75, float2(0.1,-0.3),float2(-0.85,0.85));
        float2 uv4 = getRotatedUV(i.uv, -0.5, float2(-0.1,-0.1), float2(0.52,-0.454));
        float2 uv5 = getRotatedUV(i.uv, 1.0, float2(-0.1,0.31),float2(0.53,0.25));
        float2 uv6 = getRotatedUV(i.uv, -0.75, float2(0.11,-0.1),float2(-0.05,0.15));

        // Sample the texture with different rotations
        fixed4 col1 = tex2D(_MainTex, uv1);
        fixed4 col2 = tex2D(_MainTex, uv2);
        fixed4 col3 = tex2D(_MainTex, uv3);
        fixed4 col4 = tex2D(_MainTex, uv4);
        fixed4 col5 = tex2D(_MainTex, uv5);
        fixed4 col6 = tex2D(_MainTex, uv6);
        
        // Luminance-based mixing
    fixed4 screenBlend = 1 - (1 - col1) * (1 - col2) * (1 - col3)* (1 - col4)* (1 - col5)* (1 - col6);
    
        fixed4 mask = tex2D(_MaskTex, i.uv);
        // Choose one of the blending methods or create your own combination
        fixed4 finalColor = screenBlend;
        float4 result =  clamp(float4(0.596*finalColor.r,0*finalColor.g,0.474*finalColor.b,finalColor.r),0,1);
        result.a *= mask.r-col6.g-col5.g-col4.g-col3.g-col2.g-col1.g-col6.b-col5.b-col4.b-col3.b-col2.b-col1.b;
        result.a *= mask.r;
        return result;
            }
            ENDCG
        }
    }
}
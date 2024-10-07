Shader "Custom/ParticleCircleTextureWithColor"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}   // Texture to sample from
        _Speed ("Scroll Speed", Float) = 1.0          // Speed of UV scrolling
        _Scale ("Texture Scale", Float) = 1.0         // Scale of the texture inside the circle
        _Radius ("Circle Radius", Float) = 0.5        // Radius of the circle (0.0 - 0.5)
        _JumpFrequency ("Jump Frequency", Float) = 0.5 // How often to jump (in seconds)
        _EdgeSoftness ("Edge Softness", Float) = 0.05 // Softness of the circle's edge
        _Add ("Add", Color) = (0.0, 0.0, 0.0, 0.0)  // Softness of the circle's edge
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Enable support for particle color
            #define PARTICLE

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; // Particle system color over lifetime
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Speed;
            float _Scale;
            float _Radius;
            float _JumpFrequency;
            float _EdgeSoftness;
            float4 _Add;

            // Random function to create pseudo-random values for jumps
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Use the UV coordinates of the particle
                o.color = v.color; // Pass the particle's color to the fragment shader
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use particle's UV coordinates (normalized to range [0,1])
                float2 st = i.uv;

                // Move the origin to the center (0,0)
                st = st * 2.0 - 1.0;

                // Calculate the distance from the center of the particle
                float dist = length(st);

                // Calculate smooth alpha transition at the circle edge using smoothstep
                float alpha = smoothstep(_Radius, _Radius - _EdgeSoftness, dist);

                // Sample the texture only if inside the circle with a soft edge
                if (dist <= _Radius + _EdgeSoftness)
                {
                    // Calculate jump based on time and frequency
                    float jump = floor(_Time.y / _JumpFrequency);

                    // Generate a random offset for each "jump"
                    float2 offset = float2(random(float2(jump, 0.0)), random(float2(0.0, jump)));

                    // Apply the offset and scale to UV coordinates for texture sampling
                    float2 texUV = (st + offset * _Scale) * _Scale;

                    // Sample the texture at the modified UVs
                    fixed4 col = tex2D(_MainTex, texUV);

                    // Modulate the texture color by the particle's color over lifetime
                    col *= i.color;

                    col += _Add;

                    // Return the sampled texture color inside the circle with modulated alpha
                    return fixed4(col.rgb, col.a * alpha);
                }
                else
                {
                    // Outside the circle, return transparent
                    discard;
                    return fixed4(0,0,0,0);
                }
            }
            ENDCG
        }
    }
}
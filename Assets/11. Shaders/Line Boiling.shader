Shader "Custom/LineBoiling2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Boiling Speed", Float) = 10
        _NoiseScale ("Noise Scale", Float) = 10
        _Strength ("Boiling Strength", Float) = 0.005
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Speed;
            float _NoiseScale;
            float _Strength;

            // 의사 무작위 해시 (sin 기반보다 큰 입력값에서도 패턴이 덜 깨짐)
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 격자 사이를 부드럽게 보간하는 value noise (인접 픽셀끼리 왜곡이 연속적)
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 시간에 끊김을 주어 프레임 레이트 제한 (Floor 사용)
                float timeStep = floor(_Time.y * _Speed);
                
                // 타임스텝마다 노이즈 필드를 무작위 위치로 이동시켜 매 스텝 다른 모양으로 일렁이게 함
                float2 timeOffset = float2(hash(float2(timeStep, 0.0)), hash(float2(0.0, timeStep))) * 100.0;

                // 부드러운 노이즈로 왜곡 계산 (x, y는 서로 다른 오프셋으로 샘플)
                float2 noiseUV = IN.texcoord * _NoiseScale + timeOffset;
                float2 distortion = float2(valueNoise(noiseUV), valueNoise(noiseUV + float2(37.2, 17.9))) * 2.0 - 1.0;
                
                // 원래 UV에 왜곡 더하기
                float2 uv = IN.texcoord + distortion * _Strength;

                fixed4 c = tex2D(_MainTex, uv) * IN.color;
                c.rgb *= c.a; // Sprite 셰이더 필수 연산
                return c;
            }
            ENDCG
        }
    }
}

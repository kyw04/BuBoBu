Shader "Custom/LineBoiling2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Boiling Speed", Float) = 10
        _NoiseScale ("Noise Scale", Float) = 100
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

            // 간단한 의사 무작위 노이즈 함수
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
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
                
                // UV 좌표 기반 무작위 왜곡 좌표 계산
                float2 noiseUV = floor(IN.texcoord * _NoiseScale) + timeStep;
                float2 distortion = float2(hash(noiseUV), hash(noiseUV + 1.0)) * 2.0 - 1.0;
                
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

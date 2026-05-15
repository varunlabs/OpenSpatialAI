Shader "HarmonyXR/Demo Gradient Skybox"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (0.46, 0.64, 0.82, 1)
        _HorizonColor ("Horizon Color", Color) = (0.72, 0.84, 0.88, 1)
        _GroundColor ("Ground Color", Color) = (0.39, 0.37, 0.34, 1)
        _Exposure ("Exposure", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            fixed4 _SkyColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            half _Exposure;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float height = normalize(input.direction).y;
                float skyBlend = smoothstep(0.05, 0.85, height);
                float groundBlend = smoothstep(-0.65, -0.05, height);
                fixed4 color = lerp(_GroundColor, _HorizonColor, groundBlend);
                color = lerp(color, _SkyColor, skyBlend);
                color.rgb *= _Exposure;
                return color;
            }
            ENDCG
        }
    }
}

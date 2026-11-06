Shader "Frostline/AlpineTerrainSurface"
{
    Properties { _RockTint("Rock tint", Color) = (0.28,0.29,0.30,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "TerrainForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 color:COLOR; float fog:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
            float4 _RockTint;
            CBUFFER_END
            float Hash(float3 position) { return frac(sin(dot(position,float3(127.1,311.7,74.7)))*43758.5453); }
            float Noise(float3 position)
            {
                float3 cell=floor(position); float3 blend=frac(position); blend=blend*blend*(3-2*blend);
                return lerp(lerp(lerp(Hash(cell),Hash(cell+float3(1,0,0)),blend.x),lerp(Hash(cell+float3(0,1,0)),Hash(cell+float3(1,1,0)),blend.x),blend.y),lerp(lerp(Hash(cell+float3(0,0,1)),Hash(cell+float3(1,0,1)),blend.x),lerp(Hash(cell+float3(0,1,1)),Hash(cell+float3(1,1,1)),blend.x),blend.y),blend.z);
            }
            float Fractal(float3 position)
            {
                return Noise(position)*0.5+Noise(position*2.03+7)*0.25+Noise(position*4.11+19)*0.125+Noise(position*8.21+43)*0.0625;
            }
            float3 ReliefNormal(float3 position,float3 normal,float elevation)
            {
                float3 across=ddx(position); float3 down=ddy(position);
                float3 first=cross(down,normal); float3 second=cross(normal,across);
                float determinant=dot(across,first);
                float3 gradient=sign(determinant)*(ddx(elevation)*first+ddy(elevation)*second);
                return normalize(abs(determinant)*normal-gradient);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionWS=TransformObjectToWorld(input.positionOS.xyz); output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS); output.color=input.color; output.fog=ComputeFogFactor(output.positionCS.z); return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 position=input.positionWS; float3 normal=normalize(input.normalWS);
                float broad=Noise(position*0.35); float medium=Noise(position*2.3); float grit=Noise(position*18);
                float mineral=Fractal(position*3.7);
                float bedding=position.y+position.x*0.18+position.z*0.09+broad*0.8;
                float strata=Noise(float3(position.x*0.3,bedding*3.2,position.z*0.3));
                float fracture=smoothstep(0.22,0.45,Fractal(position*float3(2.5,0.7,2.5)));
                float3 rock=_RockTint.rgb*(0.5+0.85*mineral)*(0.7+0.5*strata)*(0.7+0.3*fracture);
                rock+=float3(0.07,0.045,0.025)*medium;
                rock*=0.85+0.25*grit;
                float cover=smoothstep(0.42,0.64,input.color.r+(medium-0.5)*0.24);
                float windPhase=position.x*6.2+position.z*2.8+Noise(position*0.45)*8;
                float ripples=sin(windPhase)*0.5+0.5;
                float hollow=pow(saturate(Noise(position*2+17)),9);
                float3 snow=lerp(float3(0.52,0.62,0.72),float3(0.72,0.77,0.81),0.6+medium*0.4);
                snow*=0.94+0.06*ripples-hollow*0.45;
                float3 perturb=float3(cos(windPhase)*0.17,0,sin(windPhase)*0.085);
                float rockRelief=Fractal(position*1.7)*0.27+strata*0.11+grit*0.012;
                float snowRelief=sin(windPhase)*0.018+Fractal(position*24)*0.008-hollow*0.07;
                normal=ReliefNormal(position,normal,lerp(rockRelief,snowRelief,cover));
                float4 shadowCoord=TransformWorldToShadowCoord(position);
                Light sunlight=GetMainLight(shadowCoord);
                float diffuse=saturate(dot(normal,sunlight.direction));
                float3 ambient=SampleSH(normal)*0.8;
                float3 albedo=lerp(rock,snow,cover);
                float3 lighting=ambient+sunlight.color*diffuse*sunlight.shadowAttenuation;
                float3 view=GetWorldSpaceNormalizeViewDir(position);
                float sheen=pow(saturate(dot(normal,normalize(view+sunlight.direction))),48)*0.035*cover;
                float3 color=albedo*lighting+sunlight.color*sheen;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float4 ShadowVert(float4 position:POSITION,float3 normal:NORMAL):SV_POSITION
            {
                float3 world=TransformObjectToWorld(position.xyz);
                float4 clip=TransformWorldToHClip(ApplyShadowBias(world,TransformObjectToWorldNormal(normal),_LightDirection));
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return clip;
            }
            half4 ShadowFrag():SV_Target { return 0; }
            ENDHLSL
        }
    }
}

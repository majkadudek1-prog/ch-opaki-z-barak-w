Shader "Valve/VR/Highlight_URP_SinglePass"
{
    Properties
    {
        _TintColor("Tint Color", Color) = (1, 1, 1, 1)
        _SeeThru("SeeThru", Range(0.0, 1.0)) = 0.25
        _Darken("Darken", Range(0.0, 1.0)) = 0.0
        _MainTex("MainTex", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "HighlightSinglePass"
            
            Blend One OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always  // Test against depth buffer but don't reject

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles gles3
            #pragma multi_compile_instancing
            
            // Required for VR single pass instanced rendering
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _SeeThru;
                float _Darken;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample scene depth
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = SampleSceneDepth(screenUV);
                float sceneDepthEye = LinearEyeDepth(sceneDepth, _ZBufferParams);
                
                // Get this fragment's depth
                float fragmentDepthEye = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                
                // Check if we're behind something
                bool isBehind = fragmentDepthEye > sceneDepthEye;

                // Sample texture
                half4 vTexel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 vColor = vTexel * _TintColor * input.color;
                
                // Apply see-through effect if behind
                if (isBehind)
                {
                    vColor *= _SeeThru;
                }
                
                vColor = saturate(2.0 * vColor);
                half flAlpha = vColor.a;

                vColor.rgb *= vColor.a;
                vColor.a = lerp(0.0, _Darken, isBehind ? (flAlpha * _SeeThru) : flAlpha);

                return vColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
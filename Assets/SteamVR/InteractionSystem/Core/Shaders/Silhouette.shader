Shader "Valve/VR/SilhouetteWave"
{
    Properties
    {
        _WaveColor ("Wave Color", Color) = (1.0, 0.9, 0.0, 1.0)
        _WaveOrigin ("Wave Origin", Vector) = (0, 0, 0, 0)
        _WaveSpeed ("Wave Speed", Range(0.1, 100.0)) = 1.42
        _WaveFrequency ("Wave Frequency", Range(0.1, 10.0)) = 7.31
        _WaveAmplitude ("Wave Amplitude", Range(0.1, 5.0)) = 1.56
        _WaveWidth ("Wave Width", Range(0.01, 1.0)) = 0.605
        _WaveIntensity ("Wave Intensity", Range(0.5, 2.0)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off // Double-sided for VR
            Offset -1, -1
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Required for VR single pass instanced rendering
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            // Skinned mesh support
            #pragma multi_compile _ _SKINNING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                
                // Skinning support
                #ifdef _SKINNING_ON
                    float4 boneWeights : BLENDWEIGHTS;
                    uint4 boneIndices : BLENDINDICES;
                #endif
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _WaveColor;
                float4 _WaveOrigin;
                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveAmplitude;
                float _WaveWidth;
                float _WaveIntensity;
                float _EdgeSoftness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Transform position
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                // Transform normal
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                output.uv = input.uv;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Calculate distance from wave origin
                float3 worldToOrigin = input.positionWS - _WaveOrigin.xyz;
                float distanceFromOrigin = length(worldToOrigin);
                
                // Create expanding spherical waves
                float wavePosition = distanceFromOrigin * _WaveFrequency - _Time.y * _WaveSpeed;
                
                // Create smooth wave pattern
                float wave = sin(wavePosition) * 0.5 + 0.5;
                
                // Create wave band with soft edges
                float waveBand = 1.0 - abs(wave - 0.5) * 2.0;
                waveBand = pow(waveBand, 1.0 / _WaveWidth);
                waveBand = smoothstep(0.0, _EdgeSoftness, waveBand);
                
                // Use custom color
                half3 waveColor = _WaveColor.rgb * _WaveIntensity;
                
                // Output color with alpha based on wave
                half4 color = half4(waveColor, waveBand * _WaveColor.a);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
Shader "TheosShaders/CustomLit"
{
    Properties
    {
        _Surface("__surface", Float) = 0.0
        _Cull("__cull", Float) = 2.0

        [Toggle] _UseColorMap("Use Color Map", Float) = 0
        _ColorMap ("Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        
        [Toggle] _UseNormalMap("Use Normal Map", Float) = 0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        
        [Toggle] _UseMetallicMap("Use Metallic Map", Float) = 0
        _Metallic("Metallic", Range(0,1)) = 0
        _MetallicMap ("Metallic Map", 2D) = "white" {}
        
        [Toggle] _UseRoughnessMap("Use Roughness Map", Float) = 0
        _Roughness("Roughness", Range(0,1)) = 0
        _RoughnessMap ("Roughness Map", 2D) = "white" {}

        [Toggle] _UseEmissiveMap("Use Emissive Map", Float) = 0
        _EmissiveColor("EmissiveColor", Color) = (0,0,0,1)
        _EmissiveMap ("Emissive Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" //edit this file to change the lighting calculations
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"  //edit this file to change the lighting calculations

            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
                float4 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 tangentWS : TANGENT;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3; // Shadow coordinate
                float3 vertexLighting : TEXCOORD4; // Vertex lighting
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
            };

            float _UseColorMap;
            float4 _BaseColor;
            sampler2D _ColorMap;
            float4 _ColorMap_ST;
            
            float _UseNormalMap;
            sampler2D _NormalMap;
            
            float _UseMetallicMap;
            float _Metallic;
            sampler2D _MetallicMap;
            
            float _UseRoughnessMap;
            float _Roughness;
            sampler2D _RoughnessMap;

            float _UseEmissiveMap;
            float4 _EmissiveColor;
            sampler2D _EmissiveMap;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _ColorMap);
                o.vertex = TransformWorldToHClip(o.positionWS);

                // Transform the mesh’s base normal to world space.
                o.normalWS = TransformObjectToWorldNormal(v.normal.xyz);
                o.tangentWS = normalize(TransformObjectToWorld(v.tangent.xyz));

                // Shadow coordinate calculation
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                #else
                    o.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Vertex lighting
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    o.vertexLighting = VertexLighting(o.positionWS, o.normalWS);
                #else
                    o.vertexLighting = float3(0, 0, 0);
                #endif

                OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Sample textures conditionally
                half4 albedoColor = _UseColorMap ? tex2D(_ColorMap, i.uv) : _BaseColor;
                half3 normalTS = _UseNormalMap ? UnpackNormal(tex2D(_NormalMap, i.uv)) : half3(0, 0, 1);
                half metallicValue = _UseMetallicMap ? tex2D(_MetallicMap, i.uv).r : _Metallic;
                half roughnessValue = _UseRoughnessMap ? tex2D(_RoughnessMap, i.uv).r : _Roughness;
                half4 emissiveValue = _UseEmissiveMap ? tex2D(_EmissiveMap, i.uv) : _EmissiveColor;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // Prepare input data
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = normalize(i.normalWS);
                inputData.viewDirectionWS = viewDirWS;
                inputData.bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, inputData.normalWS);
                inputData.shadowCoord = i.shadowCoord;
                inputData.vertexLighting = i.vertexLighting;
                inputData.fogCoord = ComputeFogFactor(i.positionWS);

                // Shadow coordinate handling
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = i.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Vertex lighting
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    inputData.vertexLighting = i.vertexLighting;
                #else
                    inputData.vertexLighting = float3(0, 0, 0);
                #endif

                // Prepare surface data
                SurfaceData surfaceData;
                surfaceData.albedo = albedoColor.rgb;
                surfaceData.specular = 0;
                surfaceData.metallic = metallicValue;
                surfaceData.smoothness = 1 - roughnessValue; // Convert roughness to smoothness
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emissiveValue;
                surfaceData.occlusion = 1;
                surfaceData.alpha = albedoColor.a;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;

                // Apply shadow to the final color
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    
}

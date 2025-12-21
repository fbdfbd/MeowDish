Shader "Custom/ScreenSpaceOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Outline Thickness (Pixels)", Range(0, 10)) = 1
        _DepthThreshold("Depth Threshold", Range(0, 1)) = 0.5
        _NormalThreshold("Normal Threshold", Range(0, 1)) = 0.4
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off ZTest Always

        Pass
        {
            Name "ScreenSpaceOutlinePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

        // URP 필수
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // FullScreenPass를 위한 Blit 라이브러리
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        float4 _OutlineColor;
        float _OutlineThickness;
        float _DepthThreshold;
        float _NormalThreshold;



        // Depth용
        float TotalVariation(float center, float left, float right, float down, float up)
        {
            return abs(center - left) + abs(center - right) + abs(center - down) + abs(center - up);
        }

        // Normal용
        float TotalVariation(float3 center, float3 left, float3 right, float3 down, float3 up)
        {
            return distance(center, left) + distance(center, right) + distance(center, down) + distance(center, up);
        }





        float GetEdgeFactor(float2 uv)
        {
            // 화면 해상도에 따른 텍셀 크기
            float2 offset = _ScreenSize.zw * _OutlineThickness;

            // 1) Depth
            float depthC = SampleSceneDepth(uv);
            float depthL = SampleSceneDepth(uv + float2(-offset.x, 0));
            float depthR = SampleSceneDepth(uv + float2(offset.x, 0));
            float depthD = SampleSceneDepth(uv + float2(0, -offset.y));
            float depthU = SampleSceneDepth(uv + float2(0, offset.y));

            // 2) Normal
            float3 normalC = SampleSceneNormals(uv);
            float3 normalL = SampleSceneNormals(uv + float2(-offset.x, 0));
            float3 normalR = SampleSceneNormals(uv + float2(offset.x, 0));
            float3 normalD = SampleSceneNormals(uv + float2(0, -offset.y));
            float3 normalU = SampleSceneNormals(uv + float2(0, offset.y));

            // 3) 차이 계산
            float depthDiff = TotalVariation(depthC, depthL, depthR, depthD, depthU);
            float normalDiff = TotalVariation(normalC, normalL, normalR, normalD, normalU);

            // 비교
            float isEdgeDepth = depthDiff > _DepthThreshold ? 1.0 : 0.0;
            float isEdgeNormal = normalDiff > _NormalThreshold ? 1.0 : 0.0;

            return max(isEdgeDepth, isEdgeNormal);
        }

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // 현재 화면의 색상 가져오기
        float4 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

        // 엣지 계산
        float edge = GetEdgeFactor(input.texcoord);

        // 엣지 부분은 OutlineColor, 나머지는 원래 색상
        return lerp(originalColor, _OutlineColor, edge);
    }
    ENDHLSL
}
    }
}
Shader "Oculus/OVRMRCameraFrameLit" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _DepthTex("Depth (cm)", 2D) = "black" {}
        _InconfidenceTex("Inconfidence (0-100)", 2D) = "black" {}
        _Visible("Visible", Range(0.0,1.0)) = 1.0
    }
    SubShader{
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        #pragma target 3.0

        // Inlined OVRMRChromaKey.cginc content to avoid include file issues
        fixed4 _ChromaKeyColor;
        float _ChromaKeySimilarity;
        float _ChromaKeySmoothRange;
        float _ChromaKeySpillRange;

        // https://en.wikipedia.org/wiki/YUV
        float3 RGB2YUV(float3 rgb)
        {
            float r = rgb.x;
            float g = rgb.y;
            float b = rgb.z;
            float y = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            float u = -0.09991 * r - 0.33609 * g + 0.436 * b;
            float v = 0.615 * r - 0.55861 * g - 0.05639 * b;
            return float3(y, u, v);
        }

        float3 YUV2RGB(float3 yuv)
        {
            float y = yuv.x;
            float u = yuv.y;
            float v = yuv.z;
            float r = y + 1.28033 * v;
            float g = y - 0.21482 * u - 0.38059 * v;
            float b = y + 2.12798 * u;
            return float3(r, g, b);
        }

        fixed4 GetCameraColor(float2 colorUV);

        float ColorDistance_YUV_YUV(float3 yuv1, float3 yuv2)
        {
            float dist = distance(yuv1.yz, yuv2.yz);
            dist += saturate(yuv1.x - 0.9);
            return dist;
        }

        float ColorDistance_RGB_YUV(float3 rgb1, float3 yuv2)
        {
            float3 yuv1 = RGB2YUV(rgb1);
            return ColorDistance_YUV_YUV(yuv1, yuv2);
        }

        float ColorDistance_RGB_RGB(float3 rgb1, float3 rgb2)
        {
            float3 yuv1 = RGB2YUV(rgb1);
            float3 yuv2 = RGB2YUV(rgb2);
            return ColorDistance_YUV_YUV(yuv1, yuv2);
        }

        float RGB2Gray(float3 rgb)
        {
            float r = rgb.x;
            float g = rgb.y;
            float b = rgb.z;
            float y = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            return y;
        }

        float GetAlphaFromDistance(float dist)
        {
            float result = smoothstep(_ChromaKeySimilarity, _ChromaKeySimilarity + _ChromaKeySmoothRange, dist);
            result = result * result;
            return result;
        }

        float GetSpillFromDistance(float dist)
        {
            float result = smoothstep(_ChromaKeySimilarity, _ChromaKeySimilarity + _ChromaKeySpillRange, dist);
            result = result * result * result;
            return result;
        }

        float4 GetColorAfterChromaKey(float2 UV, float2 deltaUV, float step)
        {
            float3 chromaColor = _ChromaKeyColor.rgb;
            float3 chromaYUV = RGB2YUV(chromaColor);
            float dist = 0.0;
            const int samples = 3;
            float offset = ((float)samples - 1.0) / 2.0;
            for (int i = 0; i < samples; ++i)
            {
                for (int j = 0; j < samples; ++j)
                {
                    fixed4 color = GetCameraColor(UV + float2((float)i - offset, (float)j - offset) * deltaUV * step);
                    float d = ColorDistance_RGB_YUV(color, chromaYUV);
                    dist += d;
                }
            }
            dist /= (samples * samples);
            fixed4 centerColor = GetCameraColor(UV);
            float alpha = GetAlphaFromDistance(dist);
            float spill = GetSpillFromDistance(dist);
            float gray = RGB2Gray(centerColor.rgb);
            float4 outColor;
            outColor.rgb = lerp(float3(gray, gray, gray), centerColor.rgb, spill);
            outColor.a = alpha;
            return outColor;
        }
        // End of inlined OVRMRChromaKey.cginc

        #define TEST_ENVIRONMENT 0

        sampler2D _MainTex;
        sampler2D _DepthTex;
        sampler2D _MaskTex;

        float4 _TextureDimension; // (w, h, 1/w, 1/h)
        float4 _TextureWorldSize; // (width_in_meter, height_in_meter, 0, 0)

        float _SmoothFactor;
        float _DepthVariationClamp;

        float _CullingDistance;

        struct Input {
        #if TEST_ENVIRONMENT
            float2 uv_MainTex;
        #endif
            float4 screenPos;
        };

        fixed4 _Color;
        fixed  _Visible;
        float4 _FlipParams; // (flip_h, flip_v, 0, 0)

        fixed GetMask(float2 uv)
        {
            return tex2D(_MaskTex, uv).r;
        }

        fixed4 GetCameraColor(float2 colorUV)
        {
            fixed4 c = tex2D(_MainTex, colorUV) * _Color;
            return c;
        }

        float GetDepth(float2 uv)
        {
            float depth = tex2D(_DepthTex, uv).x * 1.0 / 100;
            return depth;
        }

        float3 GetNormal(float2 uv)
        {
            float dz_x = GetDepth(uv + float2(_TextureDimension.z, 0)) - GetDepth(uv - float2(_TextureDimension.z, 0));
            float dz_y = GetDepth(uv + float2(0, _TextureDimension.w)) - GetDepth(uv - float2(0, _TextureDimension.w));
            dz_x = clamp(dz_x, -_DepthVariationClamp, _DepthVariationClamp);
            dz_y = clamp(dz_y, -_DepthVariationClamp, _DepthVariationClamp);
            float3 normal = cross(float3(_TextureWorldSize.x * _TextureDimension.z * 2.0 * _SmoothFactor, 0, dz_x), float3(0, _TextureWorldSize.y * _TextureDimension.w * 2.0 * _SmoothFactor, dz_y));
            normal = normalize(normal);
            return normal;
        }

        void surf(Input IN, inout SurfaceOutput o) {
    #if TEST_ENVIRONMENT
            float2 colorUV = float2(IN.uv_MainTex.x, IN.uv_MainTex.y);
    #else
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float2 colorUV = float2(screenUV.x, 1.0 - screenUV.y);
    #endif
            if (_FlipParams.x > 0.0)
            {
                colorUV.x = 1.0 - colorUV.x;
            }
            if (_FlipParams.y > 0.0)
            {
                colorUV.y = 1.0 - colorUV.y;
            }
            float mask = GetMask(colorUV);
            if (mask == 0.0)
            {
                discard;
            }
            float4 col = GetColorAfterChromaKey(colorUV, _TextureDimension.zw, 1.0);
            if (col.a < 0.0)
            {
                discard;
            }
            float depth = GetDepth(colorUV);
            if (depth > _CullingDistance)
            {
                discard;
            }
            float3 normal = GetNormal(colorUV);
            o.Albedo = col.rgb;
            o.Normal = normal;
            o.Alpha = col.a *_Visible;
        }
        ENDCG
    }
    // Fixed: Changed from "Alpha-Diffuse" to "Standard" which exists in URP
    FallBack "Standard"
}


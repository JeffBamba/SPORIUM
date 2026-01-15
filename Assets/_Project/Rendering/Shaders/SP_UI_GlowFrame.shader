Shader "Sporae/UI/GlowFrame"
{
    Properties
    {
        _BorderColor ("Border Color", Color) = (0.498, 1.000, 0.478, 0.60)
        _GlowColor ("Glow Color", Color) = (0.498, 1.000, 0.478, 0.90)
        _BorderThickness ("Border Thickness (px)", Float) = 4.0
        _GlowSize ("Glow Size (px)", Float) = 14.0
        _GlowIntensity ("Glow Intensity", Float) = 1.0
        _BorderSoftness ("Border Softness (px)", Float) = 1.0
        _GlowFalloff ("Glow Falloff", Float) = 1.25
        _GradTop ("Gradient Top", Color) = (0.118, 0.157, 0.165, 0.90)
        _GradBottom ("Gradient Bottom", Color) = (0.118, 0.157, 0.165, 0.90)
        _GradStrength ("Gradient Strength", Float) = 1.0
        _EdgeMode ("Edge Mode (0=All,1=Top,2=Bottom)", Float) = 0.0
        _GlowMode ("Glow Mode (0=Both,1=Out,2=In)", Float) = 0.0
        _InnerPad ("Inner Pad (px)", Float) = 0.0
        _Size ("Target Size", Vector) = (512, 256, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BorderColor;
            float4 _GlowColor;
            float4 _GradTop;
            float4 _GradBottom;
            float4 _Size;
            float _BorderThickness;
            float _GlowSize;
            float _GlowIntensity;
            float _BorderSoftness;
            float _GlowFalloff;
            float _GradStrength;
            float _EdgeMode;
            float _GlowMode;
            float _InnerPad;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float edgeDistPixels(float2 uv, float2 sizePx)
            {
                float2 px = uv * sizePx;
                float2 d = min(px, sizePx - px);
                return min(d.x, d.y);
            }

            float signedDistToInnerRect(float2 px, float2 sizePx, float innerPad)
            {
                float2 innerMin = float2(innerPad, innerPad);
                float2 innerMax = sizePx - innerMin;
                float2 c = (innerMin + innerMax) * 0.5;
                float2 h = (innerMax - innerMin) * 0.5;
                float2 d = abs(px - c) - h;
                float outside = length(max(d, 0.0));
                float inside = min(max(d.x, d.y), 0.0);
                return outside + inside;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 sizePx = max(_Size.xy, float2(2.0, 2.0));
                float dist;
                if (_EdgeMode < 0.5)
                {
                    dist = edgeDistPixels(i.uv, sizePx);
                }
                else if (_EdgeMode < 1.5)
                {
                    // top edge only: center the edge in the middle of this glow band
                    float edgeCenter = sizePx.y * 0.5;
                    dist = abs(i.uv.y * sizePx.y - edgeCenter);
                }
                else if (_EdgeMode < 2.5)
                {
                    // bottom edge only: center the edge in the middle of this glow band
                    float edgeCenter = sizePx.y * 0.5;
                    dist = abs(i.uv.y * sizePx.y - edgeCenter);
                }
                else
                {
                    dist = edgeDistPixels(i.uv, sizePx);
                }

                float glowMask = 1.0;
                if (_GlowMode > 0.5)
                {
                    float2 px = i.uv * sizePx;
                    float sdist = signedDistToInnerRect(px, sizePx, _InnerPad);
                    if (_GlowMode < 1.5)
                    {
                        // outward only: glow only outside the inner rect
                        glowMask = step(0.0, sdist);
                        dist = max(sdist, 0.0);
                    }
                    else
                    {
                        // inward only: glow only inside the inner rect
                        glowMask = step(0.0, -sdist);
                        dist = max(-sdist, 0.0);
                    }
                }

                float border = 0.0;
                if (_BorderThickness > 0.001)
                {
                    border = smoothstep(_BorderThickness + _BorderSoftness, _BorderThickness, dist);
                }
                float glowRaw = saturate(1.0 - (dist / max(_GlowSize, 0.001)));
                float glow = pow(glowRaw, _GlowFalloff) * _GlowIntensity * glowMask;

                float4 grad = lerp(_GradTop, _GradBottom, i.uv.y) * _GradStrength;
                float glowAlpha = _GlowColor.a * glow;
                float borderAlpha = _BorderColor.a * border;
                float alpha = saturate(glowAlpha + borderAlpha);
                float3 rgb = (_GlowColor.rgb * glow) + (_BorderColor.rgb * border);
                float4 col = float4(rgb, alpha) + grad;
                return col;
            }
            ENDHLSL
        }
    }
}

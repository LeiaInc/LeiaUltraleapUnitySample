Shader "LeiaUnity/EditorPreview"
{
    Properties
    {
        _texture_0("Texture 0", 2D) = "white" { }
        _texture_1("Texture 1", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ SideBySide
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            inline float2 vec2(float a) {
                return float2(a, a);
            }

            inline float3 vec3(float a) {
                return float3(a, a, a);
            }

            inline float3 periodic_mod(float3 a, float3 b) {
                return a - b * floor(a / b);
            }

            sampler2D _texture_0;
            sampler2D _texture_1;
            float4 _texture_0_ST;

            uniform int _width;
            uniform int _height;
            uniform float _numViews;
            uniform float _actSingleTapCoef;
            uniform float _pixelPitch;
            uniform float _n;
            uniform float _d_over_n;
            uniform float _s;
            uniform float _anglePx;
            uniform float _no;
            uniform float _gamma;
            uniform float _smooth;
            uniform float _oePitchX;
            uniform float _tanSlantAngle;

            uniform float3 _subpixelCentersX;
            uniform float3 _subpixelCentersY;

            uniform float _faceX;
            uniform float _faceY;
            uniform float _faceZ;
            
#ifdef SideBySide
            inline float4 getSideBySidePixel(float2 uv) {

                int xTileCount = 2;
                int yTileCount = 1;

                int xInd = floor(uv.x * xTileCount);
                int yInd = floor((1.0 - uv.y) * yTileCount);
                int camInd = abs(int(xInd + yInd * xTileCount));

                if (camInd >= _numViews) return float4(0.0, 0.0, 0.0, 1.0);

                uv = (uv * float2(xTileCount, yTileCount)) % 1;

                return((camInd == 0) * tex2D(_texture_0, uv) +
                    (camInd == 1) * tex2D(_texture_1, uv));;
            }
#endif

            float3 smoothbox(float x1, float x2, float sm, float3 x) {
                return smoothstep(x1 - sm, x1 + sm, x) * (1.0 - smoothstep(x2 - sm, x2 + sm, x));
            }

            float3 compute_rgb(float position, float4 _texture, float3 phase, float totalCount) {
                return smoothbox(0.5 - 1.0 / 2.0 / totalCount,
                    0.5 + 1.0 / 2.0 / totalCount,
                    _smooth,
                    periodic_mod(phase - ((totalCount - 1.0) / 2.0 + position) / totalCount, vec3(1.0))) * pow(_texture.rgb, vec3(_gamma));
            }

            float3 phi(float3 xo, float3 yo) {
                float oePitchXModified = _oePitchX * (1.0 + _s / (3.0 * _width));

                float tanSlantAngleModified = _tanSlantAngle - _anglePx / (3.0 * _height);

                float phaseAtCenter = 0.5 * (_width - _tanSlantAngle * _height + _tanSlantAngle) / _oePitchX;

                phaseAtCenter += ((_numViews - 1.0) / 2.0 - _no) / _numViews;

                return vec3(phaseAtCenter) + (xo + tanSlantAngleModified * yo) / oePitchXModified / _pixelPitch;
            }

            float4 interlaceTextures(float3 phase, float4 tex1, float4 tex2) {
                float3 rgb = compute_rgb(1.0, tex1, phase, 2.0) + compute_rgb(2.0, tex2, phase, 2.0);

                float _gamma = 1.75;

                return float4(pow(rgb.r, 1.0 / _gamma), pow(rgb.g, 1.0 / _gamma), pow(rgb.b, 1.0 / _gamma), 1.0);
            }


            float4 cpsm(float2 uv, float4 tex1, float4 tex2) {

                float usedFaceX = _faceX;
                float usedFaceY = _faceY;
                float usedFaceZ = _faceZ;

                float2 xoyo = _pixelPitch * float2(uv.x - 0.5, 0.5 - uv.y) * float2(_width, _height);

                float2 XY = float2(usedFaceX, usedFaceY) - xoyo;

                float2 x1y1;
                float denom = abs(sqrt(pow(usedFaceZ, 2.0) + (1.0 - 1.0 / pow(_n, 2.0)) * (pow(XY.x, 2.0) + pow(XY.y, 2.0))));
                x1y1 = xoyo - _d_over_n * XY / denom;

                float3 x = x1y1.x + _subpixelCentersX * _pixelPitch;
                float3 y = x1y1.y + _subpixelCentersY * _pixelPitch;

                float3 phaseCorrected = phi(x, y);

                phaseCorrected = periodic_mod(phaseCorrected, vec3(1.0));
                float4 rgba = interlaceTextures(phaseCorrected, tex1, tex2);

                return rgba;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _texture_0);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
#ifdef SideBySide
                return getSideBySidePixel(i.uv);
#endif
            
                float2 uv = i.uv.xy;

                // Sample whole textures
                float4 right = tex2D(_texture_1, uv);
                float4 left = tex2D(_texture_0, uv);

                // ACT
                float _actSingleTapNorm = 1.0 - _actSingleTapCoef;
                float3 act_texL = clamp((left.rgb - _actSingleTapCoef * right.rgb) / _actSingleTapNorm, vec3(0.0), vec3(1.0));
                float3 act_texR = clamp((right.rgb - _actSingleTapCoef * left.rgb) / _actSingleTapNorm, vec3(0.0), vec3(1.0));

                // Interlace
                float2 flippedUv = float2(uv.x, 1.0 - uv.y);
                float4 retColor = cpsm(flippedUv, float4(act_texL, left.a), float4(act_texR, right.a));
                retColor.a = 1.0;

                return retColor;
            }
            ENDCG
        }
    }
}

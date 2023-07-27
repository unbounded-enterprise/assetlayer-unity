Shader"Custom/EnteredPortal" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _ScrollSpeedX ("Scroll X Speed", Range(-2,2)) = 0.5
        _ScrollSpeedY ("Scroll Y Speed", Range(-2,2)) = 0.5
        _Color ("Portal Color", Color) = (1,0,1,1)
        _DistortionFactor ("Distortion Factor", Range(0,1)) = 0.1
        _DistortionSpeed ("Distortion Speed", Range(-2,2)) = 0.5
        _RippleAmount ("Ripple Amount", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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

sampler2D _MainTex;
float4 _MainTex_ST;
float _ScrollSpeedX;
float _ScrollSpeedY;
float4 _Color;
float _DistortionFactor;
float _DistortionSpeed;
float _RippleAmount;

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    float2 uv = i.uv;
    float2 noise = tex2D(_MainTex, uv + _Time.y * _DistortionSpeed).rg - 0.5;
    uv += noise * _DistortionFactor;
    uv.x += _Time.y * _ScrollSpeedX;
    uv.y += _Time.y * _ScrollSpeedY;
    float ripple = sin(distance(uv, float2(0.5, 0.5)) * 20.0 - _Time.y * 10.0) * _RippleAmount;
    fixed4 tex = tex2D(_MainTex, uv + ripple);
    return tex * _Color;
}
            ENDCG
        }
    }
} // This closing brace was missing.

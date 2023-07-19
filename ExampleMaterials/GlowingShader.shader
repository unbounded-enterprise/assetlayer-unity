Shader "Custom/SimpleLit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Brightness ("Brightness", Range(0, 2)) = 1
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0, 6)) = 3
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 1
        _MinLight ("Min Light", Range(0, 1)) = 0.3
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        float4 _Color;
        sampler2D _EmissionMap;
        float4 _EmissionColor;
        sampler2D _BumpMap;
        float _Brightness;
        float4 _RimColor;
        float _RimPower;
        float _PulseSpeed;
        float _MinLight;

        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color * _Brightness;
            fixed3 bump = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            fixed4 emission = tex2D(_EmissionMap, IN.uv_MainTex) * _EmissionColor;

            float rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            fixed4 rimColor = _RimColor * pow(rim, _RimPower);

            float pulse = (sin(_Time.y * _PulseSpeed) + 1) * 0.5;
            col *= lerp(_MinLight, 1.0, pulse);

            o.Albedo = col.rgb;
            o.Normal = bump;
            o.Emission = emission.rgb + rimColor.rgb;
        }
        ENDCG
    }
    Fallback "Diffuse"
}

#ifndef MH2_PARRALLEX_CRYSTAL_FUNCITON
#define MH2_PARRALLEX_CRYSTAL_FUNCITON

#include "Packages/com.xuanxuan.render.utility/Shader/HLSL/Mh2_Utility.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DissolveNoiseMap);
SAMPLER(sampler_DissolveNoiseMap);

#ifdef MH2_COLOR_DYE
    TEXTURE2D(_DyeMaskTex);
    SAMPLER(sampler_DyeMaskTex);
#endif

#ifdef MH2_TOON_DISSOLVE
    TEXTURE2D(_dissolveTex);
    SAMPLER(sampler_dissolveTex);
#endif

#ifdef CRYSTAL_BASE_PASS

    TEXTURE2D(_NormalMap);
    SAMPLER(sampler_NormalMap);

    TEXTURE2D(_DistortMap);
    SAMPLER(sampler_DistortMap);

    TEXTURE2D(_HeightMap);
    SAMPLER(sampler_HeightMap);


    TEXTURE2D(_Layer1Tex);
    SAMPLER(sampler_Layer1Tex);

    TEXTURE2D(_Layer2Tex);
    SAMPLER(sampler_Layer2Tex);

    TEXTURE2D(_MatCapTex);
    SAMPLER(sampler_MatCapTex);
#endif


CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColorTint;
    half _BaseOffset;

    float4 _NormalMap_ST;
    float4 _dissolveTex_ST;

    half4 _DissolveColor;
    half4 _DissolveVec1;
    half4 _DissolveVec2;
    half4 _DissovlveCenter;
    float4 _DissolveNoiseMap_ST;

    half4 _VertexOffsetVec;

    #ifdef CRYSTAL_BASE_PASS
        float4 _HeightMap_ST;
        half4 _HeightMapVec;

        float4 _DistortMap_ST;
        half4  _DistortVec;

        float4 _Layer1Tex_ST;
        half4 _Layer1Vec;
        half4 _Layer1ColorTint;

        float4 _Layer2Tex_ST;
        half4 _Layer2Vec;
        half4 _Layer2ColorTint;

        half4 _MatCapColorTint;

        half4 _FresnelVec;
        half4 _FresnelColor;
    #endif

    #ifdef MH2_COLOR_DYE
        //染色相关
        uint _DyeDebugID;
        uint _DyeToggles;
        uint _DyeModeFlags;
        half4 _DyeColorArray[16];
    #endif

    #ifdef MH2_TOON_DISSOLVE
        uint _MH2ToonLitShaderFlags;
        float4x4 _worldSpaceToRootObjSpaceMatrix;
        half4 _dissolveColor;
        half3 _DissolveAixesDirection;
    #endif

CBUFFER_END

#ifdef MH2_TOON_DISSOLVE
    bool CheckLocalFlags(uint bits)
    {
        return (_MH2ToonLitShaderFlags&bits) != 0;
    }
    #define FLAG_BIT_TOON_DISSOLVE (1 << 7)
    #define FLAG_BIT_TOON_DISSOLVE_AIXES_EFFECT (1 << 21)
    struct DissolveData
    {
        half3 positionOS;
        half3 aixesDirection;
        half3 color;
        half4 texSampleColor;
        half4 dissolveVec1;
        half4 dissolveVec2;
    };
    void DissolveEffect(inout half4 color, DissolveData data)
    {
        half texSample = data.texSampleColor.r;
            texSample = FastLinearToSRGB(texSample);//美术的mask图很多时候都是srgb的。无法改变。
        half combinedValue = 1;
        if(CheckLocalFlags(FLAG_BIT_TOON_DISSOLVE_AIXES_EFFECT))
        {
            half aixesPos = dot(normalize(data.positionOS), normalize(data.aixesDirection)) * length(data.positionOS);
            aixesPos = aixesPos + data.dissolveVec1.x;
            aixesPos *= data.dissolveVec1.y;//轴向位置

            aixesPos = saturate(aixesPos);

            texSample *= data.dissolveVec1.z;

            //默认mask图为white。这里就不会影响到aixesPos。
            combinedValue = lerp(texSample*aixesPos,aixesPos,aixesPos);
            combinedValue = saturate(combinedValue);
        }
        else
        {
            combinedValue = texSample + data.dissolveVec1.x;
        }

        half dissolveColorInterp = Mh2Remap(combinedValue,data.dissolveVec2.x,data.dissolveVec2.x+data.dissolveVec2.y,1,0);
        color.rgb = lerp(color.rgb,data.color,dissolveColorInterp);
        color.a *= combinedValue;//2024/7/19 注意，之前是直接覆盖。但是有半透明物体就不行了。
    }
#endif

    half CalDissolveAlphaWithoutTex(float3 positionWS)
    {
        float dist = distance(positionWS,_DissovlveCenter);
        return Mh2RemapNoClamp(dist,_DissolveVec1.x,_DissolveVec1.x +_DissolveVec1.y,0,1);
    }

    float3 calDissolveWSInVextex(float3 positionWS,half3 normalWS)
    {
        half dissolveMask = CalDissolveAlphaWithoutTex(positionWS);
        dissolveMask = saturate(1-dissolveMask);
        positionWS += normalWS *_DissolveVec1.z*dissolveMask;
        return positionWS;
    }

    float3 calVertexOffsetWSInVextex(float3 positionWS,half3 normalWS)
    {
        half vertexOffsetIntensity= SimplexNoise(positionWS*_VertexOffsetVec.x+_Time.y*_VertexOffsetVec.y);
        vertexOffsetIntensity -= 0.5f;
        vertexOffsetIntensity *= _VertexOffsetVec.z; 

        positionWS += normalWS*vertexOffsetIntensity;
        return positionWS;
    }

    void calDissolveInFrag(float3 positionWS,float2 dissolveMapUV,inout half3 color)
    {
        half dissolveValue = SAMPLE_TEXTURE2D(_DissolveNoiseMap,sampler_DissolveNoiseMap,dissolveMapUV);
        half distMask = CalDissolveAlphaWithoutTex(positionWS);
                    
        dissolveValue = dissolveValue+distMask;
        // return  half4(dissolveValue.rrr,1);
                        
        half dissolveColorMask = Mh2Remap(dissolveValue,_DissolveVec2.z,_DissolveVec2.z+_DissolveVec2.w,1,0);
        // return half4(dissolveColorMask.rrr,1);
        color = lerp(color,_DissolveColor,dissolveColorMask);
        clip( dissolveValue -_DissolveVec1.w);
    }

    float2 calDissolveUV(float2 originUV,float time)
    {
        return TRANSFORM_TEX(originUV,_DissolveNoiseMap) + time * _DissolveVec2.xy;
    }

    #ifdef CRYSTAL_BASE_PASS

     // This example uses the Attributes structure as an input structure in
        // the vertex shader.
        struct Attributes
        {
            float4 positionOS   : POSITION;
            float2 uv           : TEXCOORD0;
            float3 normalOS     : NORMAL;
            float4 tangentOS    : TANGENT;
            #ifdef MH2_COLOR_DYE
                float4 Color        : COLOR;
            #endif
        };

        struct Varyings
        {
            // The positions in this struct must have the SV_POSITION semantic.
            float4 positionHCS  : SV_POSITION;
            float4 uv           :TEXCOORD0;//zw matCapUV
            float4 heightDistortUV :TEXCOORD1;
            float4 layer1Layer2UV :TEXCOORD2;
            float4 DissolveUV :TEXCOORD3;
            float3 positionWS   :TEXCOORD4;
            half3 normalWS      :TEXCOORD5;
            #ifdef _NORMALMAP
                half3 tangentWS                : TEXCOORD7;    // xyz: tangent, w: viewDir.y
                half3 bitangentWS              : TEXCOORD8;   
            #endif

            #ifdef _NORMALMAP
                half3 tangentOS : TEXCOORD6;
            #else
                half3 viewDirTS    :TEXCOORD6;
            #endif

            #ifdef MH2_COLOR_DYE
                half4 pixelDyeBlackHSV : TEXCOORD9;
                half4 pixelDyeWhiteHSV : TEXCOORD10;
            #endif

            #ifdef MH2_TOON_DISSOLVE
                half3 effectPositionOS:TEXCOORD11;
                float4 positionOS   : TEXCOORD12;
            #endif
            
        };

        #ifdef MH2_COLOR_DYE
            #define Flag_Bit_Dye_Enable (1 << 0)
            #define Flag_Bit_Dye_Debug (1 << 1)
            #define Flag_Bit_Dye_DebugMask (1 << 2)
            bool CheckDyeFlags(uint bits)
            {
                return (_DyeToggles&bits) != 0;
            }
        #endif

        half4 LayerColor(TEXTURE2D_PARAM(_Texture,sampler_Texture),float2 layerUV,half2 viewVecFullHeight,half4 layerVec,half4 layerTint)
        {
            half layerHeightStep = layerVec.z;
            layerUV = layerUV + viewVecFullHeight*layerHeightStep;
            half4 layerColor = SAMPLE_TEXTURE2D(_Texture,sampler_Texture,layerUV)*layerTint;
            return  layerColor;
            
        }

     
        

        Varyings vert(Attributes IN)
        {
            // Declaring the output object (OUT) with the Varyings struct.
            Varyings OUT = (Varyings) 0;
            OUT.positionWS = TransformObjectToWorld(IN.positionOS);

            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            #ifdef _NORMALMAP
                real sign = real(IN.tangentOS.w) * GetOddNegativeScale();
                OUT.tangentWS = real3(TransformObjectToWorldDir(IN.tangentOS.xyz));
                OUT.bitangentWS = real3(cross(OUT.normalWS, float3(OUT.tangentWS))) * sign;
            #endif
            
            
            #ifdef MRR_DISSOLVE
                OUT.positionWS = calDissolveWSInVextex(OUT.positionWS,OUT.normalWS);
            #endif

            #ifdef CRYSTAL_VERTEX_OFFSET
                OUT.positionWS = calVertexOffsetWSInVextex(OUT.positionWS,OUT.normalWS);
            #endif
            OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
            
            
            
            OUT.uv.xy = TRANSFORM_TEX(IN.uv,_BaseMap);
            

            #if defined(CRYSTAL_MATCAP)  && !defined(_NORMALMAP)
                float2 matCapUV;
                matCapUV.x = mul(normalize(UNITY_MATRIX_IT_MV[0].xyz),normalize(IN.normalOS));
                matCapUV.y = mul(normalize(UNITY_MATRIX_IT_MV[1].xyz),normalize(IN.normalOS));
                matCapUV = matCapUV*0.5+0.5;
                OUT.uv.zw = matCapUV;
            #endif

            #ifdef _NORMALMAP
                OUT.uv.zw = TRANSFORM_TEX(IN.uv,_NormalMap);
            #endif
            
            
            

            float time = _Time.x;
            OUT.heightDistortUV.xy = TRANSFORM_TEX(IN.uv,_HeightMap) +time*_HeightMapVec.xy;
            
            #ifdef CRYSTAL_DISTORT
                OUT.heightDistortUV.zw = TRANSFORM_TEX(IN.uv,_DistortMap) + time*_DistortVec.xy;
            #endif

            #ifdef CRYSTAL_LAYER1
                OUT.layer1Layer2UV.xy = TRANSFORM_TEX(IN.uv,_Layer1Tex) + time*_Layer1Vec.xy;
            #endif
            
            #ifdef CRYSTAL_LAYER2
                OUT.layer1Layer2UV.zw = TRANSFORM_TEX(IN.uv,_Layer2Tex) + time*_Layer2Vec.xy;
            #endif

            #ifdef MRR_DISSOLVE
                OUT.DissolveUV.xy =  calDissolveUV(IN.uv,time);
            #endif
            
            
            // half3 viewDirWS = GetWorldSpaceViewDir(positionWS);
            // VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS,IN.tangentOS);
            //
            #ifdef _NORMALMAP
                OUT.tangentOS = IN.tangentOS;
            #else
                half3 binormalOS = cross(IN.tangentOS,IN.normalOS);
                half3x3 tbnMatrix = half3x3(IN.tangentOS.xyz,binormalOS.xyz,IN.normalOS.xyz);
                float3 camPosOS = TransformWorldToObject(float4(_WorldSpaceCameraPos, 1.0)).xyz;
                half3 viewDirOS = normalize(camPosOS - IN.positionOS);

                OUT.viewDirTS = mul(tbnMatrix,viewDirOS);
            #endif


            #ifdef MH2_COLOR_DYE
                //染色相关
                uint DyeID = IN.Color.g * 255;
                uint DyeId2 = DyeID & 15;
                uint DyeId1 = (DyeID >> 4) & 15;
                
                UNITY_BRANCH
                if(CheckDyeFlags(Flag_Bit_Dye_Enable))
                {
                    OUT.pixelDyeBlackHSV = _DyeColorArray[DyeId1];
                    OUT.pixelDyeWhiteHSV = _DyeColorArray[DyeId2];
                    if((_DyeModeFlags & (1 << DyeId1))== 0)   //A通道的正负用来记录色相or着色模式 负为色相，正为着色
                        OUT.pixelDyeBlackHSV.a *= -1;
                    if((_DyeModeFlags & (1 << DyeId2))== 0)
                        OUT.pixelDyeWhiteHSV.a *= -1;
                }
                UNITY_BRANCH
                if (CheckDyeFlags(Flag_Bit_Dye_Debug))
                {
                    //染色debug
                    if(DyeId1 == _DyeDebugID)
                        OUT.pixelDyeBlackHSV = 1;
                    else
                        OUT.pixelDyeBlackHSV = 0;
                    if(DyeId2 == _DyeDebugID)
                        OUT.pixelDyeWhiteHSV = 1;
                    else
                        OUT.pixelDyeWhiteHSV = 0;
                }
            #endif

            #ifdef MH2_TOON_DISSOLVE
                UNITY_BRANCH
                if(CheckLocalFlags(FLAG_BIT_TOON_DISSOLVE))
                {
                    
                    //if(CheckLocalFlags(FLAG_BIT_TOON_CUSTOM_WORLD_TO_OBJ_MATRIX))
                    //{
                    OUT.effectPositionOS =  mul(_worldSpaceToRootObjSpaceMatrix,half4(TransformObjectToWorld(IN.positionOS), 1)); 
                    //}
                    //else
                    //{
                    //    output.effectPositionOS = input.positionOS;
                    //}
                    OUT.positionOS = IN.positionOS;
                    OUT.uv.zw = TRANSFORM_TEX(IN.uv,_dissolveTex);
                }
            #endif
            
            // Returning the output.
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            
            // float2 uv = IN.uv;
            // //切线空间下viewDir
            half2 matcapUV;
            half3 viewDirTS = 0;
            #ifdef _NORMALMAP
                half3 normalTSSample = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,IN.uv.zw));
                half3x3 tangentToWorldMatrix = half3x3(IN.tangentWS,IN.bitangentWS,IN.normalWS);
                half3 normalDir = TransformTangentToWorld(normalTSSample,tangentToWorldMatrix,true);
            
                matcapUV.x = mul((UNITY_MATRIX_V[0]).xyz,normalDir);
                matcapUV.y = mul((UNITY_MATRIX_V[1]).xyz,normalDir);
                matcapUV = matcapUV*0.5+0.5;

                half3 viewDirWS =  _WorldSpaceCameraPos - IN.positionWS;
                tangentToWorldMatrix[2] = normalDir;
                viewDirTS = normalize(mul(tangentToWorldMatrix,viewDirWS)) ;
                
            #else
                matcapUV = IN.uv.zw;
                viewDirTS = normalize(IN.viewDirTS);
            #endif
            
            
            
            half distort = 0;
            #ifdef CRYSTAL_DISTORT
                distort = SAMPLE_TEXTURE2D(_DistortMap,sampler_DistortMap,IN.heightDistortUV.zw).r * _DistortVec.z;
                distort  = distort * 2 -1;
            #endif
            
            float2 heightUV = IN.heightDistortUV.xy;
            heightUV += distort*_HeightMapVec.w;
            half height = SAMPLE_TEXTURE2D(_HeightMap,sampler_HeightMap,heightUV);
            half heightScale = _HeightMapVec.z;
            // height *= heightScale;
            half2 viewVec = viewDirTS.xy ;
            half2 viewVecFullHeight = viewVec*height*heightScale;
            half2 uvAfterHeigh = IN.uv +viewVecFullHeight*_BaseOffset;

            half4 texMarble = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvAfterHeigh)*_BaseColorTint;
            
            half3 color = texMarble.rgb ;
            half alpha = texMarble.a;
            #ifdef CRYSTAL_LAYER1
            // half layerFallOff = 1;
                half4 layer1Color = LayerColor(TEXTURE2D_ARGS(_Layer1Tex,sampler_Layer1Tex),IN.layer1Layer2UV.xy,viewVecFullHeight,_Layer1Vec,_Layer1ColorTint);
                color += layer1Color;
            #endif
            #ifdef  CRYSTAL_LAYER2
                half4 layer2Color = LayerColor(TEXTURE2D_ARGS(_Layer2Tex,sampler_Layer2Tex),IN.layer1Layer2UV.zw,viewVecFullHeight,_Layer2Vec,_Layer2ColorTint);
                color += layer2Color;
            #endif

            #ifdef MH2_COLOR_DYE
                UNITY_BRANCH
                if(CheckDyeFlags(Flag_Bit_Dye_Enable))
                {
                    //albedo染色
                    //先黑白然后插值
                    half2 DyeMaskTex = SAMPLE_TEXTURE2D(_DyeMaskTex, sampler_DyeMaskTex, uvAfterHeigh).rg; 
                    
                    half3 blackColor = color;
                    half3 whiteColor = color;
                    
                    UNITY_BRANCH
                    if(IN.pixelDyeBlackHSV.a < 0)
                    {
                        //色相偏移模式
                        blackColor = rgb2hsv(blackColor);
                        blackColor.r = blackColor.r + IN.pixelDyeBlackHSV.r * 0.5; //色相偏移
                        blackColor = hsv2rgb(blackColor);
                        //饱和度
                        blackColor = ColorSaturate(blackColor, IN.pixelDyeBlackHSV.g + 1);
                        //亮度黑白插值算法
                        blackColor = IN.pixelDyeBlackHSV.b < 0 ? lerp(blackColor, 0.0, -IN.pixelDyeBlackHSV.b) : lerp(blackColor, 1.0, IN.pixelDyeBlackHSV.b);
                    }
                    else
                    {
                        //求灰度
                        half lightness = Max3(blackColor.r, blackColor.g, blackColor.b) + Min3(blackColor.r, blackColor.g, blackColor.b);
                        lightness = IN.pixelDyeBlackHSV.z < 0 ? lerp(lightness, 0.0, -IN.pixelDyeBlackHSV.z) : lerp(lightness, 2.0, IN.pixelDyeBlackHSV.z);
                        half saturation = IN.pixelDyeBlackHSV.y * (1 - saturate(lightness - 1));
                        blackColor = hsv2rgb(half3(IN.pixelDyeBlackHSV.x, saturation, saturate(lightness)));
                    }

                    UNITY_BRANCH
                    if(IN.pixelDyeWhiteHSV.a < 0)
                    {
                        //色相偏移模式
                        whiteColor = rgb2hsv(whiteColor);
                        whiteColor.r = whiteColor.r + IN.pixelDyeWhiteHSV.r * 0.5; //色相偏移
                        whiteColor = hsv2rgb(whiteColor);
                        //饱和度
                        whiteColor = ColorSaturate(whiteColor, IN.pixelDyeWhiteHSV.g + 1);
                        //亮度黑白插值算法
                        whiteColor = IN.pixelDyeWhiteHSV.b < 0 ? lerp(whiteColor, 0.0, -IN.pixelDyeWhiteHSV.b) : lerp(whiteColor, 1.0, IN.pixelDyeWhiteHSV.b);
                    }
                    else
                    {
                        half lightness = Max3(whiteColor.r, whiteColor.g, whiteColor.b) + Min3(whiteColor.r, whiteColor.g, whiteColor.b);
                        lightness = IN.pixelDyeWhiteHSV.z < 0 ? lerp(lightness, 0.0, -IN.pixelDyeWhiteHSV.z) : lerp(lightness, 2.0, IN.pixelDyeWhiteHSV.z);
                        half saturation = IN.pixelDyeWhiteHSV.y * (1 - saturate(lightness - 1));
                        whiteColor = hsv2rgb(half3(IN.pixelDyeWhiteHSV.x, saturation, saturate(lightness)));
                    }
                    
                    color = saturate(lerp(color ,lerp(blackColor, whiteColor, DyeMaskTex.r), DyeMaskTex.g));
                    alpha = saturate(alpha * lerp(abs(IN.pixelDyeBlackHSV.a), abs(IN.pixelDyeWhiteHSV.a), DyeMaskTex.g));

                    //染色Debug
                    UNITY_BRANCH
                    if (CheckDyeFlags(Flag_Bit_Dye_Debug))
                    {
                        half3 DyeDebugReturn;
                        if(CheckDyeFlags(Flag_Bit_Dye_DebugMask))
                        {
                            DyeDebugReturn = lerp(IN.pixelDyeBlackHSV.rgb, IN.pixelDyeWhiteHSV.rgb, DyeMaskTex.r) * DyeMaskTex.g;
                        }
                        else
                        {
                            DyeDebugReturn = max(IN.pixelDyeBlackHSV.rgb, IN.pixelDyeWhiteHSV.rgb);
                        }
                        return half4(DyeDebugReturn ,1.0);
                    }
                }
            #endif

            #ifdef CRYSTAL_MATCAP
                half3 matCapColor = SAMPLE_TEXTURE2D(_MatCapTex,sampler_MatCapTex,matcapUV)*_MatCapColorTint.rgb*_MatCapColorTint.a;
                color += matCapColor;
            #endif

            half3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
            #ifndef _NORMALMAP
                half3 normalDir = normalize(IN.normalWS);
            #endif

            half dotNV = dot(normalDir,viewDir);

            #ifdef CRYSTAL_FRESNEL
                half fresnel = 1 - Mh2Remap(dotNV,_FresnelVec.x-_FresnelVec.y,_FresnelVec.x + _FresnelVec.y,0,1);
                fresnel = clamp(fresnel,0,1);
                half3 fresnelColor = fresnel*_FresnelColor.rgb*_FresnelColor.a;
                color += fresnelColor;
            #endif

            #ifdef MRR_DISSOLVE
                calDissolveInFrag(IN.positionWS,IN.DissolveUV.xy,color);
            #endif

            #ifdef MH2_TOON_DISSOLVE
                UNITY_BRANCH
                if(CheckLocalFlags(FLAG_BIT_TOON_DISSOLVE))
                {
                    DissolveData dissolve_data ;
                    dissolve_data.positionOS = IN.positionOS;
                    dissolve_data.aixesDirection =_DissolveAixesDirection;

                    dissolve_data.color = _dissolveColor;

                    dissolve_data.texSampleColor = SAMPLE_TEXTURE2D(_dissolveTex,sampler_dissolveTex,IN.uv.zw);
                    dissolve_data.dissolveVec1 = _DissolveVec1;
                    dissolve_data.dissolveVec2 = _DissolveVec2;
                    
                    half4 color2 = half4(color ,alpha);
                    DissolveEffect(color2, dissolve_data);
                    alpha *= color2.a;
                    color = color2.rgb;
                    clip(alpha - dissolve_data.dissolveVec1.w);
                }
            #endif
            
            
            return half4(color,alpha);
            
        }

    #endif


#endif
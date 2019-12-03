using System.Collections;
using System.Collections.Generic;
using System.IO;
using MoleMole;
using UnityEngine;

public enum FOWSDFPregenerateMode
{
    CPU,
    GPU
}

public class FOWShadowSDF : FOWShadow
{
    public FOWSDFPregenerateMode m_PregenerateMode = FOWSDFPregenerateMode.GPU;
    public string m_TextureSavePath = "";

    public int m_Test = 4;
    
    // Shading parameter
    public int   m_BlurInteration      = 2;
    public float m_BlurLevel           = 3.5f;
    public float m_BlurResolutionScale = 0.8f;
    
    // Scene dependent parameter
    public float m_CutOff       = 0.0001f;
    public float m_Luminance    = 3.5f;
    public float m_StepScale    = 0.9f;
    public float m_StepMinValue = 0.0001f;

    private Color[]   m_SDFBuffer        = null;
    private RenderTexture m_SDFTexture       = null;
    private Vector2   m_TextureSizeScale = Vector2.one;
    private Vector2   m_TextureTexelSize = Vector2.one;
    
    private RenderTexture m_Fowtexture   = null;
    private Material m_ShadowSDFMaterial = null;
    private Material m_SDFMaterial       = null;
    private Material m_BlurMaterial      = null;

    private FOWData    m_FOWData;
    private FOWMapData m_MapData = null;
    
    public int texWidth
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.m_TexWidth;
            return 0;
        }
    }

    public int texHeight
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.m_TexHeight;
            return 0;
        }
    }

    public Texture2D mapDataTexture
    {
        get
        {
            if (m_MapData != null)
            {
                return m_MapData.mapDataTexture;
            }

            return null;
        }
    }

    public RenderTexture sdfTexture
    {
        get
        {
            return m_SDFTexture;
        }
    }

    public RenderTexture fowTexture
    {
        get
        {
            return m_Fowtexture;
        }
    }

    public FOWShadowSDF(FOWData data)
    {
        Init(data);
    }

    public void Init(FOWData data)
    {
        m_FOWData = data;

        int length = m_FOWData.m_TexWidth * m_FOWData.m_TexHeight;

        if (m_Fowtexture == null)
        {
            m_Fowtexture            = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);
            m_Fowtexture.wrapMode   = TextureWrapMode.Clamp;
            m_Fowtexture.filterMode = FilterMode.Bilinear;
            m_Fowtexture.Create();
        }

        if (m_SDFMaterial == null) 
            m_SDFMaterial = new Material(Shader.Find("Hidden/FOWJumpFloodSDF"));
        
        if (m_ShadowSDFMaterial == null)
            m_ShadowSDFMaterial = new Material(Shader.Find("Hidden/FOWShadowSDF"));

        if (m_BlurMaterial == null) 
            m_BlurMaterial = new Material(Shader.Find("Hidden/FOWBlurSDF"));

        if (m_MapData == null)
        {
            m_MapData = new FOWMapData();
            m_MapData.Init(data);
        }

        if (m_SDFBuffer == null) m_SDFBuffer = new Color[length];

        if (m_SDFTexture == null)
        {
            m_SDFTexture            = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat);
            m_SDFTexture.wrapMode   = TextureWrapMode.Clamp;
            m_SDFTexture.filterMode = FilterMode.Bilinear;
        }
        
        if(texWidth > texHeight)
             m_TextureSizeScale = new Vector2(1.0f, (float)texHeight / texWidth);
        else
            m_TextureSizeScale = new Vector2((float)texWidth / texHeight, 1.0f);

        m_TextureTexelSize = new Vector2(1.0f / texWidth, 1.0f / texHeight);
    }

    public void Destroy()
    {
        if (m_Fowtexture != null)
        {
            m_Fowtexture.Release();
            m_Fowtexture = null;
        }

        if (m_MapData != null)
        {
            m_MapData.DeInit();
            m_MapData = null;
        }
        
        if(m_SDFTexture != null)
            Object.Destroy(m_SDFTexture);
    }

    public void Update() { }

    public void UpdatePlayerData(List<FOWPlayerData> playerDataList)
    {
        if (playerDataList.Count > 0)
        {
            var playerPosWS  = playerDataList[0].position;
            var playerRadius = playerDataList[0].radius;
            var playerPosLS  = m_FOWData.WorldToLocal(playerPosWS);
            playerPosLS.x /= m_FOWData.m_XSize;
            playerPosLS.z /= m_FOWData.m_ZSize;

            var playerLSNorm = new Vector2(playerPosLS.x, playerPosLS.z);

            m_ShadowSDFMaterial.SetVector("_PlayerPos", playerLSNorm);
            m_ShadowSDFMaterial.SetFloat("_PlayerRadius", playerRadius);
            
            m_BlurMaterial.SetVector("_PlayerPos", playerLSNorm);
            m_BlurMaterial.SetFloat("_PlayerRadius", playerRadius);
        }
    }

    public void Pregenerate()
    {
        switch(m_PregenerateMode)
        {
        case FOWSDFPregenerateMode.CPU:
            PregenerateCPU();
            break;
            
        case FOWSDFPregenerateMode.GPU:
            PregenerateGPU();
            break;
        }
    }
    
    //--! Ref: https://yuhuang-neil.iteye.com/blog/1186683
    public bool IsPowerOf2(int number)
    {
        if((((number-1) & number) == 0) && number!=0)
            return true;  
        return false;  
    }
    
    public void PregenerateGPU()
    {
        if (texWidth != texHeight || !IsPowerOf2(texHeight))
        {
            Debug.Log("Texture size need to be power of 2, using CPU mode instead");
            return;
        }
        
        // Generate a map Texture
        m_MapData.GenerateMapData();
    
        //--! Ref: https://www.comp.nus.edu.sg/~tants/jfa/i3d06.pdf Jump Flood Algorithm
        //--! Ref: https://www.shadertoy.com/view/Mdy3DK
        // SDF Texture Initialization
        RenderTexture rt0 = RenderTexture.GetTemporary(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat);
        RenderTexture rt1 = RenderTexture.GetTemporary(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat);
        m_SDFMaterial.SetTexture("_MapDataTexture", m_MapData.mapDataTexture);
        Graphics.Blit(null, rt0, m_SDFMaterial, 0);
        
        RenderTexture[] pingpong = new RenderTexture[2];
        pingpong[0] = rt0;
        pingpong[1] = rt1;

        float power = Mathf.Log(texHeight, 2);
        Vector2 texelSize = new Vector2(1.0f / texWidth, 1.0f / texHeight);

        int levelCount = (int)power + m_Test;
        for (int i = 0; i <= levelCount; i++)
        {
            int level  = i > power ? (int) power : i;
            int index0 = i % 2;
            int index1 = (i + 1) % 2;
        
            m_SDFMaterial.SetTexture("_SDFTexture", pingpong[index0]);
            m_SDFMaterial.SetVector("_TexelSize", texelSize);
            m_SDFMaterial.SetFloat("_Level", level);
            m_SDFMaterial.SetFloat("_Power", power);
            Graphics.Blit(null, pingpong[index1], m_SDFMaterial, 1);
        }

        // Final gathering
        RenderTexture result = null;
        if (levelCount % 2 == 1) 
            result = pingpong[0];
        else
            result = pingpong[1];
        
        RenderTexture temp = RenderTexture.GetTemporary(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat);
        m_SDFMaterial.SetTexture("_SDFFinalTexture", result);
        Graphics.Blit(null, temp, m_SDFMaterial, 2);
        Graphics.CopyTexture(temp, m_SDFTexture);
            
        RenderTexture.ReleaseTemporary(rt0);
        RenderTexture.ReleaseTemporary(rt1);
    }
    
    public void PregenerateCPU()
    {
        // if (m_TextureSavePath != "")
        // {
        //     // Regenerate a sdf texture if read from disk failed
        //     if (ReadFOWTexture())
        //         return;
            
        //     Debug.Log("Read sdf texture from disk failed");
        // }

        // // Generate a map Texture
        // m_MapData.GenerateMapData();

        // // Generate a sdf Texture
        // var obstacleBuffer = m_MapData.obstacleBuffer;
        // var invWidth       = 1.0f / texWidth;
        // var invHeight      = 1.0f / texHeight;
        // // Using map texture to calculate a sdf texture
        // for (int j = 0; j < texHeight; j++)
        // {
        //     for (int i = 0; i < texWidth; i++)
        //     {
        //         // obstacle already
        //         if (obstacleBuffer.Exists(x => x.x == i && x.y == j))
        //         {
        //             m_SDFBuffer[j * texWidth + i] = Color.black;
        //             continue;
        //         }

        //         // loop all obstacle pixel
        //         var minDistance         = float.MaxValue;
        //         var position            = new Vector2(i * invWidth, j * invHeight);
        //         var minDistancePosition = Vector2.zero;
        //         for (int count = 0; count < obstacleBuffer.Count; count++)
        //         {
        //             float distance = (obstacleBuffer[count] - position).magnitude;
        //             if (distance < minDistance)
        //             {
        //                 minDistancePosition = obstacleBuffer[count];
        //                 minDistance         = distance;
        //             }
        //         }

        //         m_SDFBuffer[j * texWidth + i].r = minDistance;
        //         m_SDFBuffer[j * texWidth + i].g = 0.0f;
        //         m_SDFBuffer[j * texWidth + i].b = 0.0f;
        //         m_SDFBuffer[j * texWidth + i].a = 1.0f;
        //     }
        // }
        
        // m_SDFTexture.SetPixels(m_SDFBuffer);
        // m_SDFTexture.Apply();
    }

    public bool ReadFOWTexture()
    {
        // byte[] bytes = null;

        // if (File.Exists(m_TextureSavePath))
        // {
        //     bytes = System.IO.File.ReadAllBytes(m_TextureSavePath);
            
        //     if(m_SDFTexture == null)
        //         m_SDFTexture = new RenderTexture(1, 1);
            
        //     // this will auto-resize the texture dimensions.
        //     m_SDFTexture.LoadImage(bytes);

        //     if (m_SDFTexture.width != texWidth || m_SDFTexture.height != texHeight)
        //     {
        //         Debug.Log("File: " + m_TextureSavePath + "have different size with given data");
        //         m_SDFTexture.Resize(texWidth, texHeight);
        //         return false;
        //     }
        //     return true;
        // }
        // else
        // {
        //     Debug.Log("File path didn't existed");
        //     return false;
        // }
        return false;
    }

    public bool SaveFOWTexture()
    {
        // if (m_TextureSavePath != "")
        // {
        //     System.IO.File.WriteAllBytes(m_TextureSavePath, m_MapData.mapDataTexture.EncodeToPNG());
        //     System.IO.File.WriteAllBytes(m_TextureSavePath, m_SDFTexture.EncodeToPNG());
            
        //     Texture2D fowTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
        //     RenderTexture.active = m_Fowtexture;
        //     fowTexture.ReadPixels(new Rect(0, 0, m_Fowtexture.width, m_Fowtexture.height), 0, 0);
        //     fowTexture.Apply();
        //     System.IO.File.WriteAllBytes(m_TextureSavePath, fowTexture.EncodeToPNG());
        //     return true;
        // }

        return false;
    }

    public RenderTexture RenderFOWTexture()
    {
        // SDF shading parameter update
        m_ShadowSDFMaterial.SetFloat("_CutOff", m_CutOff);
        m_ShadowSDFMaterial.SetFloat("_Luminance", m_Luminance);
        m_ShadowSDFMaterial.SetFloat("_StepScale", m_StepScale);
        m_ShadowSDFMaterial.SetFloat("_StepMinValue", m_StepMinValue);
        
        m_ShadowSDFMaterial.SetTexture("_SDFTexture", m_SDFTexture);
        m_ShadowSDFMaterial.SetVector("_TextureSizeScale", m_TextureSizeScale);
        Graphics.Blit(null, m_Fowtexture, m_ShadowSDFMaterial);

        // Blur
        RenderTexture rt = RenderTexture.GetTemporary(m_Fowtexture.width, m_Fowtexture.height, 0);
        Graphics.Blit(m_Fowtexture, rt);

        m_BlurMaterial.SetVector("_TextureTexelSize", m_TextureTexelSize);
        m_BlurMaterial.SetFloat("_BlurLevel", m_BlurLevel);
        for (int i = 0; i < m_BlurInteration; i++)
        {
            int width  = (int) (m_BlurResolutionScale * rt.width);
            int height = (int) (m_BlurResolutionScale * rt.height);
            var rt2 = RenderTexture.GetTemporary(width, height, 0);
            m_BlurMaterial.SetTexture("_FOWTexture", rt);
            Graphics.Blit(null, rt2, m_BlurMaterial);

            RenderTexture.ReleaseTemporary(rt);
            rt = rt2;
        }

        Graphics.Blit(rt, m_Fowtexture);
        RenderTexture.ReleaseTemporary(rt);

        return m_Fowtexture;
    }

    public float GetLerpValue()
    {
        return 0.0f;
    }
}

using System.Collections;
using System.Collections.Generic;
using MoleMole;
using UnityEngine;

public class FOWShadowDota : FOWShadow
{
    public FOWMap m_Map = null;

    public float m_BlurOffset     = 0.05f;
    public int   m_BlurInteration = 0;

    private Material m_BlurMaterial = null;
    private RenderTexture m_Fowtexture = null;
    
    public Texture2D mapDataTexture
    {
        get
        {
            if (m_Map != null && m_Map.m_MapData != null)
            {
                return m_Map.m_MapData.mapDataTexture;
            }
            return null;
        }
    }
    
    public Texture2D maskDataTexture
    {
        get
        {
            if (m_Map != null && m_Map.m_MaskData != null)
            {
                return m_Map.m_MaskData.maskDataTexture;
            }
            return null;
        }
    }
    
    public RenderTexture fowTexture
    {
        get
        {
            return m_Fowtexture;
        }
    }

    public FOWShadowDota(FOWData data)
    {
        Init(data);
    }
    
    public void Init(FOWData data)
    {
        if (m_Fowtexture == null)
        {
            m_Fowtexture = new RenderTexture(data.m_TexWidth, data.m_TexHeight, 0, RenderTextureFormat.ARGB32);
            m_Fowtexture.wrapMode   = TextureWrapMode.Clamp;
            m_Fowtexture.filterMode = FilterMode.Bilinear;
            m_Fowtexture.Create();
        }
                    
        if(m_BlurMaterial == null)
            m_BlurMaterial = new Material(Shader.Find("Hidden/FOWBlur"));
        
        // Generate map data
        if (m_Map == null)
            m_Map = new FOWMap(data);
    }

    public void Destroy()
    {
        if (m_Map != null)
        {
            m_Map.Release();
            m_Map = null;
        }

        if (m_Fowtexture != null)
        {
            m_Fowtexture.Release();
            m_Fowtexture = null;
        }
    }

    public void Update()
    {
        m_Map.Update();
    }

    public void UpdatePlayerData(List<FOWPlayerData> playerDataList)
    {
        m_Map.UpdateMask(playerDataList);
    }

    public void Pregenerate()
    {
        m_Map.GenerateMapData();
    }

    public bool SaveFOWTexture() { return false; }
    public bool ReadFOWTexture() { return false; }

    public RenderTexture RenderFOWTexture()
    {
        var maskTexture = maskDataTexture;

        RenderTexture rt = RenderTexture.GetTemporary(m_Fowtexture.width, m_Fowtexture.height, 0);
        Graphics.Blit(maskTexture, rt);
        
        m_BlurMaterial.SetFloat("_Offset", m_BlurOffset);
        for (int i = 0; i <= m_BlurInteration; i++)
        {
            var rt2 = RenderTexture.GetTemporary(maskTexture.width / 2, maskTexture.height / 2, 0);
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
        return m_Map.m_MaskData.mixValue;
    }
}

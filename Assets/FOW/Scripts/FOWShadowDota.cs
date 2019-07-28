using System.Collections;
using System.Collections.Generic;
using MoleMole;
using UnityEngine;

public class FOWShadowDota : FOWShadow
{
    public FOWMap m_Map = null;

    public float m_BlurOffset     = 0.05f;
    public int   m_BlurInteration = 0;
    public float m_BlurResolutionScale = 0.8f;

    private Material m_BlurMaterial = null;
    private RenderTexture m_Fowtexture = null;
    private FOWData m_FOWData = null;
    private Vector2 m_TextureTexelSize = Vector2.one;

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
    
    public void Init(FOWData data = null)
    {
        if(data != null)
            m_FOWData = data;
        
        if (m_Fowtexture == null ||
            m_Fowtexture.width != texWidth ||
            m_Fowtexture.height != texHeight)
        {
            if (m_Fowtexture != null)
            {
                Object.DestroyImmediate(m_Fowtexture);
                m_Fowtexture = null;
            }
            
            m_Fowtexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);
            m_Fowtexture.wrapMode   = TextureWrapMode.Clamp;
            m_Fowtexture.filterMode = FilterMode.Bilinear;
            m_Fowtexture.Create();
        }
                    
        if(m_BlurMaterial == null)
            m_BlurMaterial = new Material(Shader.Find("Hidden/FOWBlur"));
        
        // Generate map data
        if (m_Map == null)
            m_Map = new FOWMap(data);
        
        m_TextureTexelSize = new Vector2(1.0f / texWidth, 1.0f / texHeight);
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
        Init();
        
        m_Map.Update();
    }

    public void FixedUpdate()
    {
        m_Map.FixedUpdate();
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
        m_BlurMaterial.SetVector("_TextureTexelSize", m_TextureTexelSize);
        for (int i = 0; i <= m_BlurInteration; i++)
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
        return m_Map.m_MaskData.mixValue;
    }
    
    public virtual FOWShadowType GetFowShadowType()
    {
        return FOWShadowType.FOWSHADOW_TYPE_DOTA;
    }
}

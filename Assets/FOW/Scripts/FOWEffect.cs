using System;
using System.Collections;
using System.Collections.Generic;
using MoleMole;
using UnityEngine;

// Fog of war 
[DisallowMultipleComponent]
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FOWEffect : MonoBehaviour
{
    #region Public

    [Header("-----------------------Map Data-----------------------")]
    // MapData and MaskData share the same FOWData
    public FOWData m_Data = new FOWData();

    public FOWShadowType         m_FOWShadowType = FOWShadowType.FOWSHADOW_TYPE_SDF;
    public FOWShadow             m_FOWShadow;
    public FOWSDFPregenerateMode m_PregenerateMode = FOWSDFPregenerateMode.GPU;

    [Header("-----------------------Shading-----------------------")]
    public int m_BlurInteration = 0;
    public float m_BlurOffset          = 0.05f;
    public float m_BlurLevel           = 3.5f;
    public float m_BlurResolutionScale = 0.8f;
    
    [Range(0.0f, 1.0f)] public float m_FogValue = 0.4f;

    [HideInInspector] public string m_TextureSavePath = "";

    public float m_CutOff       = 0.0001f;
    public float m_Luminance    = 3.5f;
    public float m_StepScale    = 0.9f;
    public float m_StepMinValue = 0.0001f;
    
    public int m_Test = 4;

    public Color m_FogColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    public int m_ColliderLayerMask = 0;

    public static FOWEffect instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<FOWEffect>();

            return m_Instance;
        }
    }

    // preview computed map data 
    [Header("-----------------------Debug-----------------------")]
    public bool m_VisualizeGrid = true;

    public bool m_VisualizeRay    = false;
    public bool m_VisualizeOrigin = false;
    public bool m_VisualizeAxis   = false;

    public float m_AxisLength   = 10.0f;
    public float m_OriginRadius = 3.0f;
    [HideInInspector] public bool m_IsPlayerDatasUpdated = false;

    #endregion

    #region Private

    private Material m_EffectMaterial;
    private Camera m_Camera = null;
    private List<FOWPlayerData> m_PlayerDataList = null;
    private RenderTexture m_CopyCameraBuffer = null;
    private bool m_IsInited = false;
    private static FOWEffect m_Instance;

    #endregion

    public void Awake()
    {
        Init();
        GenerateMapData();
    }

    public void OnDestroy()
    {
        if (m_FOWShadow != null)
        {
            m_FOWShadow.Destroy();
            m_FOWShadow = null;
        }

        m_PlayerDataList = null;
    }

    public void Update()
    {
        m_Data.Update();
        if(m_FOWShadow != null)
            m_FOWShadow.Update();
    }

    public void Init()
    {
        // Camera need to write depth into depthTexture
        if (m_Camera == null)
        {
            m_Camera = gameObject.GetComponent<Camera>();
            m_Camera.depthTextureMode = DepthTextureMode.Depth;
        }

        if (m_FOWShadow == null)
        {
            switch (m_FOWShadowType)
            {
            case FOWShadowType.FOWSHADOW_TYPE_DOTA:
                m_FOWShadow = new FOWShadowDota(m_Data);
                break;

            case FOWShadowType.FOWSHADOW_TYPE_SDF:
                m_FOWShadow                                    = new FOWShadowSDF(m_Data);
                ((FOWShadowSDF) m_FOWShadow).m_TextureSavePath = m_TextureSavePath;
                ((FOWShadowSDF) m_FOWShadow).m_PregenerateMode = m_PregenerateMode;
                break;
            }
        }

        if (m_EffectMaterial == null)
        {
            m_EffectMaterial = new Material(Shader.Find("Hidden/FOWEffect"));
            switch (m_FOWShadowType)
            {
            case FOWShadowType.FOWSHADOW_TYPE_DOTA:
                m_EffectMaterial.DisableKeyword("FOWSHADOWTYPE_SDF");
                break;

            case FOWShadowType.FOWSHADOW_TYPE_SDF:
                m_EffectMaterial.EnableKeyword("FOWSHADOWTYPE_SDF");
                break;
            }
        }

        m_IsInited = true;
    }

    public void GenerateMapData()
    {
        if (!IsInited()) return;
        m_FOWShadow.Pregenerate();
    }

    public void SaveMapData()
    {
        if (!IsInited()) return;
        m_FOWShadow.SaveFOWTexture();
    }

    public bool IsInited()
    {
        return m_IsInited;
    }
    
    // Register and update specific playerData
    public void UpdatePlayerData(FOWPlayerData playerData)
    {
        if (!IsInited()) return;

        if (m_PlayerDataList == null)
            m_PlayerDataList = new List<FOWPlayerData>();

        if (!m_PlayerDataList.Contains(playerData))
            m_PlayerDataList.Add(playerData);

        if (!m_IsPlayerDatasUpdated)
        {
            // Async update map's maskData
            m_FOWShadow.UpdatePlayerData(m_PlayerDataList);

            m_IsPlayerDatasUpdated = true;
        }
    }

    public void RemovePlayerData(FOWPlayerData playerData)
    {
        if (!IsInited()) return;

        if (m_PlayerDataList != null && m_PlayerDataList.Contains(playerData))
            m_PlayerDataList.Remove(playerData);
    }

    public void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!IsInited())
        {
            Graphics.Blit(src, dst);
            return;
        }

        // Update param of FOWShadow's shading parameter
        switch (m_FOWShadowType)
        {
        case FOWShadowType.FOWSHADOW_TYPE_DOTA:
            var shadowDOTA = (FOWShadowDota) m_FOWShadow;
            shadowDOTA.m_BlurOffset     = m_BlurOffset;
            shadowDOTA.m_BlurInteration = m_BlurInteration;
            break;

        case FOWShadowType.FOWSHADOW_TYPE_SDF:
            var shadowSDF = (FOWShadowSDF) m_FOWShadow;
            shadowSDF.m_BlurLevel           = m_BlurLevel;
            shadowSDF.m_BlurInteration      = m_BlurInteration;
            shadowSDF.m_BlurResolutionScale = m_BlurResolutionScale;
            shadowSDF.m_CutOff              = m_CutOff;
            shadowSDF.m_Luminance           = m_Luminance;
            shadowSDF.m_StepScale           = m_StepScale;
            shadowSDF.m_StepMinValue        = m_StepMinValue;
            shadowSDF.m_Test                = m_Test;
            break;
        }

        var fowTexture = m_FOWShadow.RenderFOWTexture();

        // Fog of war post-process effect 
        var cameraBuffer = src;

        // regenerate a render texture if attributes changed
        if (m_CopyCameraBuffer != null)
        {
            if (m_CopyCameraBuffer.width != cameraBuffer.width || m_CopyCameraBuffer.height != cameraBuffer.height)
            {
                m_CopyCameraBuffer.Release();
                m_CopyCameraBuffer = null;
            }
        }

        if (m_CopyCameraBuffer == null)
        {
            m_CopyCameraBuffer = new RenderTexture(cameraBuffer);
            m_CopyCameraBuffer.Create();
        }

        Graphics.CopyTexture(cameraBuffer, m_CopyCameraBuffer);
        m_EffectMaterial.SetFloat("_LerpValue", m_FOWShadow.GetLerpValue());
        m_EffectMaterial.SetColor("_FogColor", m_FogColor);
        m_EffectMaterial.SetVector("_InvSize", m_Data.invSize);
        m_EffectMaterial.SetTexture("_FOWTexture", fowTexture);
        m_EffectMaterial.SetTexture("_CameraColorBuffer", m_CopyCameraBuffer);
        m_EffectMaterial.SetMatrix("_InvVP", (m_Camera.projectionMatrix * m_Camera.worldToCameraMatrix).inverse);
        m_EffectMaterial.SetMatrix("_FOWWorldToLocal", m_Data.localToWorld.inverse);
        m_EffectMaterial.SetVector("_PositionWS", m_Data.m_PositionWS);
        m_EffectMaterial.SetFloat("_FogValue", m_FogValue);

        Graphics.Blit(null, dst, m_EffectMaterial);
    }

    #region Visualization

    public void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        if (m_Data == null) return;

        var xSize       = m_Data.m_XSize;
        var zSize       = m_Data.m_ZSize;
        var texWidth    = m_Data.m_TexWidth;
        var texHeight   = m_Data.m_TexHeight;
        var positionWS  = m_Data.m_PositionWS;
        var heightRange = m_Data.m_HeightRange;

        if (heightRange <= 0) return;
        if (xSize <= 0 || zSize <= 0 || texWidth <= 0 || texHeight <= 0) return;

        // coordinate axis
        if (m_VisualizeAxis)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(positionWS, m_Data.LocalToWorldDir(Vector3.right) * m_AxisLength);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(positionWS, m_Data.LocalToWorldDir(Vector3.up) * m_AxisLength);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(positionWS, m_Data.LocalToWorldDir(Vector3.forward) * m_AxisLength);
        }

        float deltax = xSize / texWidth;
        float deltay = zSize / texHeight;

        if (m_VisualizeGrid)
        {
            for (int i = 0; i <= texWidth; i++)
            {
                Vector3 b = m_Data.LocalToWorld(new Vector3(i * deltax, 0, 0));
                Vector3 t = m_Data.LocalToWorld(new Vector3(i * deltax, 0, zSize));
                Gizmos.color = Color.green;
                Gizmos.DrawLine(b, t);
            }

            for (int j = 0; j <= texHeight; j++)
            {
                Vector3 b = m_Data.LocalToWorld(new Vector3(0, 0, j * deltay));
                Vector3 t = m_Data.LocalToWorld(new Vector3(xSize, 0, j * deltay));

                Gizmos.color = Color.green;
                Gizmos.DrawLine(b, t);
            }
        }

        if (m_VisualizeRay)
        {
            // Ray visualization
            RaycastHit hit;
            Ray        ray = new Ray(Vector3.zero, m_Data.LocalToWorldDir(Vector3.down));

            LayerMask stageCollider = LayerMask.NameToLayer("StageCollider");
            int       layerMask     = 1 << stageCollider.value;

            // LayerMask stageCollider = LayerMask.NameToLayer("StageCollider");
            // stageCollider = ~stageCollider;
            for (int j = 0; j <= texHeight; j++)
            {
                for (int i = 0; i <= texWidth; i++)
                {
                    Vector3 position = new Vector3(i * deltax, 0, j * deltay);
                    ray.origin = m_Data.LocalToWorld(position);
                    // Does the ray intersect any objects excluding the player layer
                    if (Physics.Raycast(ray, out hit, heightRange, layerMask))
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * heightRange);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * heightRange);
                    }
                }
            }
        }

        if (m_VisualizeOrigin)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(positionWS, m_OriginRadius);
            Gizmos.DrawSphere(positionWS + m_Data.m_XSize * m_Data.LocalToWorldDir(Vector3.right) + m_Data.m_ZSize * m_Data.LocalToWorldDir(Vector3.forward), m_OriginRadius);
        }
    }

    #endregion
}

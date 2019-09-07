using System;
using System.Collections;
using System.Collections.Generic;
using MoleMole;
using UnityEngine;
using Object = UnityEngine.Object;

public class FOWMaskData
{
    public enum MaskType
    {
        AccurateFOV = 1,
        BasicFOV    = 2,
        Circular    = 3,
    }

    private enum UpdateMark
    {
        None,
        Changed,
        EndUpdate,
    }

    public MaskType m_MaskType = MaskType.BasicFOV;

    public Texture2D maskDataTexture
    {
        get
        {
            return m_MaskTexture;
        }
    }

    // Basic Data getter
    public Vector3 positionWS
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.m_PositionWS;
            return Vector3.zero;
        }
    }

    public float deltaX
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.deltaX;
            return 0.0f;
        }
    }

    public float deltaZ
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.deltaZ;
            return 0.0f;
        }
    }

    public float invDeltaX
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.invDeltaX;
            return 0.0f;
        }
    }

    public float invDeltaZ
    {
        get
        {
            if (m_FOWData != null) return m_FOWData.invDeltaZ;
            return 0.0f;
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

    public float mixValue
    {
        get
        {
            return m_MixValue;
        }
    }

    private float m_TimeAccumlation;
    private float m_RefreshTime;
    private float m_MixValue;

    private Texture2D m_MaskTexture = null;
    private byte[]    m_MaskCache   = null;
    private Color[]   m_ColorBuffer = null;

    private UpdateMark m_UpdateMark;

    private FOWData m_FOWData = null;

    // FOV Calculation
    private Queue<Vector2Int> m_RootPixelQueue;
    private List<int>         m_ArrivedPixels;
    private object            m_Lock;
    
    [HideInInspector] public float             m_RefreshTimeSpeed = 4.0f;
    [HideInInspector] public float             m_MixTimeSpeed     = 3.0f;
    [HideInInspector] public float             m_FadeTimeLength   = 1.0f;

    public void Init(FOWData data = null)
    {
        if(data != null)
            m_FOWData = data;

        int length = texWidth * texHeight;

        // regenerate when attributes changed
        if (m_MaskCache == null || m_MaskCache.Length != length)
            m_MaskCache = new byte[length];

        if (m_ColorBuffer == null || m_ColorBuffer.Length != length)
            m_ColorBuffer = new Color[length];

        // regenerate the texture if necessary
        if (m_MaskTexture == null || 
            m_MaskTexture.width != texWidth || 
            m_MaskTexture.height != texHeight)
        {
            if (m_MaskTexture != null)
            {
                Object.DestroyImmediate(m_MaskTexture);
                m_MaskTexture = null;
            }

            m_MaskTexture            = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
            m_MaskTexture.wrapMode   = TextureWrapMode.Clamp;
            m_MaskTexture.filterMode = FilterMode.Bilinear;
        }

        if (m_RootPixelQueue == null)
            m_RootPixelQueue = new Queue<Vector2Int>();

        if (m_ArrivedPixels == null)
            m_ArrivedPixels = new List<int>();
        
        if(m_Lock == null)
            m_Lock = new object();
    }

    public void DeInit()
    {
        Init(m_FOWData);
        
        if (m_MaskTexture != null)
        {
            Object.DestroyImmediate(m_MaskTexture);
            m_MaskTexture = null;
        }

        m_MaskCache   = null;
        m_ColorBuffer = null;

        // fov calculator
        if (m_RootPixelQueue != null)
        {
            m_RootPixelQueue.Clear();
            m_RootPixelQueue = null;
        }

        if (m_ArrivedPixels != null)
        {
            m_ArrivedPixels.Clear();
            m_ArrivedPixels = null;
        }
    }

    public void Update()
    {
        Init();

        if (m_MixValue >= 1.0f)
        {
            if (m_RefreshTime >= 1.0f)
            {
                m_RefreshTime = 0.0f;
                if (UpdateMaskTexture())
                {
                    m_MixValue = 0;
                    FOWEffect.instance.SetLerpValue(0);
                    FOWEffect.instance.m_IsPlayerDatasUpdated = false;
                }
            }
            else
            {
                m_RefreshTime += Time.deltaTime* m_RefreshTimeSpeed;
            }
        }
        else
        {
            m_MixValue += Time.deltaTime* m_MixTimeSpeed;
            FOWEffect.instance.SetLerpValue(m_MixValue);
        }
    }

    private bool UpdateMaskTexture()
    {
        lock (m_Lock)
        {
            if (m_UpdateMark == UpdateMark.None) return false;
            if (m_UpdateMark == UpdateMark.EndUpdate) return true;

            // Update MaskTexture from ColorBuffer
            m_MaskTexture.SetPixels(m_ColorBuffer);
            m_MaskTexture.Apply();

            // Mark as clean
            m_UpdateMark = UpdateMark.None;
            return true;
        }
    }

    public void UpdateColorBuffer()
    {
        if (m_UpdateMark != UpdateMark.Changed)
        {
            m_UpdateMark = UpdateMark.EndUpdate;
        }
        else
        {
            for (int i = 0; i < texWidth; i++)
            {
                for (int j = 0; j < texHeight; j++)
                {
                    bool isVisible = m_MaskCache[j * texWidth + i] == 1;

                    // Update last and current frame's color buffer channel
                    Color origin = m_ColorBuffer[j * texWidth + i];
                    origin.r = Mathf.Clamp01(origin.r + origin.g);
                    origin.b = origin.g;
                    origin.g = isVisible ? 1 : 0;
                    origin.a = 1.0f;

                    m_ColorBuffer[j * texWidth + i] = origin;
                    m_MaskCache[j * texWidth + i]   = 0;
                }
            }
        }
    }

    private void SetVisible(int x, int y)
    {
        m_MaskCache[y * texWidth + x] = 1;

        m_UpdateMark = UpdateMark.Changed;
    }

    private bool IsVisible(int x, int y)
    {
        if (x < 0 || x >= texWidth || y < 0 || y >= texHeight)
            return false;
        return m_ColorBuffer[y * texWidth + x].g > 0.5f;
    }

    #region Mask Calculation

    public void Calculate(FOWPlayerData playerData, FOWMapData mapData)
    {
        switch (m_MaskType)
        {
        case MaskType.Circular:
            CalculateCircular(playerData, mapData);
            break;

        case MaskType.AccurateFOV:
        case MaskType.BasicFOV:
            CalculateFOVMask(playerData, mapData);
            break;

        default:
            CalculateCircular(playerData, mapData);
            break;
        }
    }

    private void CalculateCircular(FOWPlayerData playerData, FOWMapData mapData)
    {
        Vector3 playerPositionLS = m_FOWData.WorldToLocal(playerData.position);

        int rx = (int) (playerData.radius * invDeltaX);
        int rz = (int) (playerData.radius * invDeltaZ);
        int rs = rx * rx;
        int x  = Mathf.FloorToInt((playerPositionLS.x) * invDeltaX);
        int z  = Mathf.FloorToInt((playerPositionLS.z) * invDeltaZ);

        int beginx = Mathf.Max(0, x - rx);
        int beginy = Mathf.Max(0, z - rz);
        int endx   = Mathf.Min(texWidth, x + rx);
        int endy   = Mathf.Min(texHeight, z + rz);

        for (int j = beginy; j < endy; j++)
        {
            for (int i = beginx; i < endx; i++)
            {
                int dx   = i - x;
                int dy   = j - z;
                int rads = dx * dx + dy * dy;
                if (rads <= rs && !mapData[i, j])
                    SetVisible(i, j);
            }
        }
    }

    private void CalculateFOVMask(FOWPlayerData playerData, FOWMapData mapData)
    {
        Vector3 playerLocalPosition = m_FOWData.WorldToLocal(playerData.position);
        float   radiusSq            = playerData.radiusSqr;

        int x = Mathf.FloorToInt((playerLocalPosition.x) * invDeltaX);
        int z = Mathf.FloorToInt((playerLocalPosition.z) * invDeltaZ);

        if (x < 0 || x >= texWidth) return;
        if (z < 0 || z >= texHeight) return;

        if (mapData[x, z]) return;

        m_RootPixelQueue.Clear();
        m_ArrivedPixels.Clear();

        m_RootPixelQueue.Enqueue(new Vector2Int(x, z));
        m_ArrivedPixels.Add(z * texWidth + x);
        SetVisible(x, z);

        while (m_RootPixelQueue.Count > 0)
        {
            var root = m_RootPixelQueue.Dequeue();
            if (mapData[root.x, root.y])
            {
                if (PreRayCast(root, x, z))
                {
                    int index = root.y * texWidth + root.x;
                    if (!m_ArrivedPixels.Contains(index))
                        m_ArrivedPixels.Add(index);
                    SetVisible(root.x, root.y);
                }
                else
                    RayCast(root, x, z, playerData);

                continue;
            }

            // spread the visible area around root position in four direction
            SetVisibleAtPosition(root.x - 1, root.y, x, z, radiusSq);
            SetVisibleAtPosition(root.x, root.y - 1, x, z, radiusSq);
            SetVisibleAtPosition(root.x + 1, root.y, x, z, radiusSq);
            SetVisibleAtPosition(root.x, root.y + 1, x, z, radiusSq);
        }
    }

    private bool PreRayCast(Vector2Int pos, int centX, int centZ)
    {
        float k = ((float) (pos.y - centZ)) / (pos.x - centX);
        if (k < -0.414f && k >= -2.414f)
        {
            return !IsVisible(pos.x + 1, pos.y + 1) && !IsVisible(pos.x - 1, pos.y - 1);
        }
        else if (k < -2.414f || k >= 2.414f)
        {
            return !IsVisible(pos.x + 1, pos.y) && !IsVisible(pos.x - 1, pos.y);
        }
        else if (k < 2.414f && k >= 0.414f)
        {
            return !IsVisible(pos.x + 1, pos.y - 1) && !IsVisible(pos.x - 1, pos.y + 1);
        }
        else
        {
            return !IsVisible(pos.x, pos.y + 1) && !IsVisible(pos.x, pos.y - 1);
        }
    }

    private void RayCast(Vector2Int pos, int centX, int centZ, FOWPlayerData playerData)
    {
        float r = playerData.radius * invDeltaX;

        Vector2 dir = new Vector2(pos.x - centX, pos.y - centZ);

        float l = dir.magnitude;
        if (r - l <= 0) return;

        dir = dir.normalized * (r - l);
        int x = pos.x + (int) dir.x;
        int y = pos.y + (int) dir.y;

        SetInvisibleLine(pos.x, pos.y, x, y, centX, centZ, playerData.radiusSqr);
    }

    private bool IsInRange(int x, int z, int centX, int centZ, float radiusSq)
    {
        float r = (x - centX) * (x - centX) * deltaX * deltaX + (z - centZ) * (z - centZ) * deltaZ * deltaZ;

        if (r > radiusSq)
            return false;

        return true;
    }

    private void SetVisibleAtPosition(int x, int z, int centX, int centZ, float radiusSq)
    {
        if (x < 0 || z < 0 || x >= texWidth || z >= texHeight)
            return;

        if (!IsInRange(x, z, centX, centZ, radiusSq))
            return;

        int index = z * texWidth + x;
        if (m_ArrivedPixels.Contains(index))
            return;

        m_ArrivedPixels.Add(index);
        m_RootPixelQueue.Enqueue(new Vector2Int(x, z));
        SetVisible(x, z);
    }

    private void SetInvisibleAtPosition(int x, int z)
    {
        int index = z * texWidth + x;
        if (m_ArrivedPixels.Contains(index) == false)
        {
            m_ArrivedPixels.Add(index);
        }
    }

    private void SetInvisibleLine(int beginx, int beginy, int endx, int endy, int centX, int centZ, float rsq)
    {
        int dx = Mathf.Abs(endx - beginx);
        int dy = Mathf.Abs(endy - beginy);
        //int x, y;
        int step = ((endy < beginy && endx >= beginx) || (endy >= beginy && endx < beginx)) ? -1 : 1;
        int p,   twod, twodm;
        int pv1, pv2,  to;
        int x,   y;
        if (dy < dx)
        {
            p     = 2 * dy - dx;
            twod  = 2 * dy;
            twodm = 2 * (dy - dx);
            if (beginx > endx)
            {
                pv1  = endx;
                pv2  = endy;
                endx = beginx;
            }
            else
            {
                pv1 = beginx;
                pv2 = beginy;
            }

            to = endx;
        }
        else
        {
            p     = 2 * dx - dy;
            twod  = 2 * dx;
            twodm = 2 * (dx - dy);
            if (beginy > endy)
            {
                pv2  = endx;
                pv1  = endy;
                endy = beginy;
            }
            else
            {
                pv2 = beginx;
                pv1 = beginy;
            }

            to = endy;
        }

        if (dy < dx)
        {
            x = pv1;
            y = pv2;
        }
        else
        {
            x = pv2;
            y = pv1;
        }

        SetInvisibleAtPosition(x, y);
        while (pv1 < to)
        {
            pv1++;
            if (p < 0)
                p += twod;
            else
            {
                pv2 += step;
                p   += twodm;
            }

            if (dy < dx)
            {
                x = pv1;
                y = pv2;
            }
            else
            {
                x = pv2;
                y = pv1;
            }

            if (!IsInRange(x, y, centX, centZ, rsq))
            {
                return;
            }

            SetInvisibleAtPosition(x, y);
        }
    }

    #endregion
}

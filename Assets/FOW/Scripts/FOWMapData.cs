using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoleMole
{
    public class FOWMapData
    {
        public bool isPrecomputed
        {
            get
            {
                return false;
            }
        }
                
        // texel size
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

        // Fog area size
        public float xSize
        {
            get
            {
                if (m_FOWData != null) return m_FOWData.m_XSize;
                return 0.0f;
            }
        }

        public float zSize
        {
            get
            {
                if (m_FOWData != null) return m_FOWData.m_ZSize;
                return 0.0f;
            }
        }

        // Fog texture size
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

        public Vector3 positionWS
        {
            get
            {
                if (m_FOWData != null) return m_FOWData.m_PositionWS;
                return Vector3.zero;
            }
        }

        public Vector3 rotationWS
        {
            get
            {
                if (m_FOWData != null) return m_FOWData.m_RotationWS;
                return Vector3.zero;
            }
        }

        public float heightRange
        {
            get
            {
                if (m_FOWData != null) return m_FOWData.m_HeightRange;
                return 0.0f;
            }
        }

        public bool this[int i, int j]
        {
            get
            {
                return m_MapData[i, j];
            }
        }

        public Texture2D mapDataTexture
        {
            get
            {
                return m_MapDataTexture;
            }
        }

        public Color[] colorBuffer
        {
            get
            {
                return m_MapDataColorBuffer;
            }
        }

        public List<Vector2> obstacleBuffer
        {
            get
            {
                return m_MapDataObstacleBuffer;
            }
        }

        private bool[,] m_MapData;

        // Debug use for preview
        private Color[]   m_MapDataColorBuffer;
        private List<Vector2> m_MapDataObstacleBuffer;
        private Texture2D m_MapDataTexture;

        private FOWData m_FOWData = null;
        
        public void Init(FOWData data)
        {
            m_FOWData = data;

            int length = texWidth * texHeight;
            
            // regenerate when attributes changed
            if (m_MapDataColorBuffer == null || m_MapDataColorBuffer.Length != length)
            {
                m_MapDataColorBuffer = null;
                m_MapDataColorBuffer = new Color[texWidth * texHeight];
            }

            if (m_MapDataObstacleBuffer == null) 
                m_MapDataObstacleBuffer = new List<Vector2>();

            if (m_MapDataTexture == null)
            {
                m_MapDataTexture            = null;
                m_MapDataTexture            = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, true);
                m_MapDataTexture.wrapMode   = TextureWrapMode.Clamp;
                m_MapDataTexture.filterMode = FilterMode.Trilinear;
            }

            if (m_MapData == null || m_MapData.Length != length)
            {
                m_MapData = null;
                m_MapData = new bool[texWidth, texHeight];
            }
        }
        
        public void DeInit()
        {
            Object.DestroyImmediate(m_MapDataTexture);
            m_MapDataTexture        = null;
            m_MapData               = null;
            m_MapDataColorBuffer    = null;
            m_MapDataObstacleBuffer = null;
            m_FOWData               = null;
        }
        
        public void Update()
        {
        }
        
        private bool IsObstacle(int x, int y)
        {
            float px = x * deltaX;
            float py = y * deltaZ;

            Vector3 startPosition = m_FOWData.LocalToWorld(new Vector3(px, 0, py));
            Vector3 rayDirection  = m_FOWData.LocalToWorldDir(Vector3.down);
            
            Ray ray = new Ray(startPosition, rayDirection);

            LayerMask stageCollider = LayerMask.NameToLayer("StageCollider");
            int layerMask = 1 << stageCollider.value;
            
            // if (Physics.Raycast(ray, heightRange, layerMask)) 
            if (Physics.Raycast(ray, heightRange)) 
                return true;
            else
                return false;
        }
        
        public void GenerateMapData()
        {
            for (int j = 0; j < texHeight; j++)
            {
                for (int i = 0; i < texWidth; i++)
                {
                    m_MapData[i, j] = IsObstacle(i, j);
                }
            }

            var invWidth  = 1.0f / texWidth;
            var invHeight = 1.0f / texHeight;
            
            // Debug use for preview
            for (int j = 0; j < texHeight; j++)
            {
                for (int i = 0; i < texWidth; i++)
                {
                    bool isObstacle = IsObstacle(i, j);
                    m_MapDataColorBuffer[j * texWidth + i] = isObstacle ? Color.red : Color.green;
                    
                    if(isObstacle)
                        m_MapDataObstacleBuffer.Add(new Vector2(i * invWidth, j * invHeight));
                }
            }

            if (m_MapDataColorBuffer.Length > 0)
            {
                m_MapDataTexture.SetPixels(m_MapDataColorBuffer);
                m_MapDataTexture.Apply();
            }
        }
    }
}

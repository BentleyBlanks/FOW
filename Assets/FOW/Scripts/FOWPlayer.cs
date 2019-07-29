using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoleMole
{
    public class FOWPlayerData
    {
        public Vector3 position  = Vector3.negativeInfinity;
        public float   radius    = 0.0f;
        public float   radiusSqr = 0.0f;

        public FOWPlayerData(Vector3 position, float radius)
        {
            this.position  = position;
            this.radius    = radius;
            this.radiusSqr = radius * radius;
        }
    }

    [ExecuteInEditMode]
    public class FOWPlayer : MonoBehaviour
    {
        [Range(0.0f, 10.0f)] public float m_Radius = 0.15f;
        [Range(0.0f, 5.0f)] public float m_PlayerSpeed = 1.0f;
        
        private Transform     m_Transform  = null;
        private FOWPlayerData m_PlayerData = null;

        void Awake()
        {
            Init();
        }

        void OnDestroy()
        {
            // FOWEffect may destory first
            if (m_PlayerData != null && FOWEffect.instance != null)
            {
                FOWEffect.instance.RemovePlayerData(m_PlayerData);
                m_PlayerData = null;
            }
        }

        public void Update()
        {
            Init();

            UpdatePosition();
            
            Vector3 position = m_Transform.position;
            // Update playerData if any attributes have changed
            if (FOWEffect.instance.m_FOWShadowType == FOWShadowType.FOWSHADOW_TYPE_SDF ||
                m_PlayerData.position != position ||
                m_PlayerData.radius != m_Radius)
            {
                m_PlayerData.radius    = m_Radius;
                m_PlayerData.radiusSqr = m_Radius * m_Radius;
                m_PlayerData.position  = position;

                FOWEffect.instance.m_IsPlayerDatasUpdated = false;
                FOWEffect.instance.UpdatePlayerData(m_PlayerData);
            }
        }

        public void UpdatePosition()
        {
            Vector3 position = m_Transform.position;
            
            if (Input.GetKeyDown (KeyCode.W))
                position.z += m_PlayerSpeed;
            if (Input.GetKeyDown (KeyCode.S))
                position.z -= m_PlayerSpeed;
            if (Input.GetKeyDown (KeyCode.A))
                position.x -= m_PlayerSpeed;
            if (Input.GetKeyDown (KeyCode.D))
                position.x += m_PlayerSpeed;
            
            m_Transform.position = position;
        }

        public void Init()
        {
            if (m_PlayerData == null) 
                m_PlayerData = new FOWPlayerData(Vector3.negativeInfinity, m_Radius);

            if (m_Transform == null)
                m_Transform = transform;
        }
    }
}

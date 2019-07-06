using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoleMole
{
    [Serializable]
    public class FOWData
    {
        [Range(-100.0f, 100.0f)] public float m_HeightRange = 1.0f;
        [Range(0.0f, 1000.0f)]   public float m_XSize       = 120.0f;
        [Range(0.0f, 1000.0f)]   public float m_ZSize       = 120.0f;

        [Range(0, 1000)] public int m_TexWidth  = 100;
        [Range(0, 1000)] public int m_TexHeight = 100;
        
        public Vector3 m_PositionWS = Vector3.zero;
        public Vector3 m_RotationWS = Vector3.zero;

        public float deltaX
        {
            get { return m_DeltaX; }
        }
        
        public float deltaZ
        {
            get { return m_DeltaZ; }
        }
        
        public float invDeltaX
        {
            get { return m_InvDeltaX; }
        }
        
        public float invDeltaZ
        {
            get { return m_InvDeltaZ; }
        }
        
        public Vector2 invSize
        {
            get { return m_InvSize; }
        }
        
        public Matrix4x4 localToWorld
        {
            get { return m_Transform; }
        }
        
        public Matrix4x4 localToWorldDir
        {
            get { return m_TransformDir; }
        }

        private float m_DeltaX       = 0.0f;
        private float m_DeltaZ       = 0.0f;
        private float m_InvDeltaX    = 0.0f;
        private float m_InvDeltaZ    = 0.0f;

        private Vector2 m_InvSize          = Vector2.one;
        private Vector3 m_PositionCenterWS = Vector3.zero;
        
        private Matrix4x4 m_Transform    = Matrix4x4.zero;
        private Matrix4x4 m_TransformDir = Matrix4x4.zero;

        public void Update()
        {
            m_DeltaX      = m_XSize / m_TexWidth;
            m_DeltaZ      = m_ZSize / m_TexHeight;
            m_InvDeltaX   = 1.0f / m_DeltaX;
            m_InvDeltaZ   = 1.0f / m_DeltaZ;
            m_InvSize.x = 1.0f / m_XSize;
            m_InvSize.y = 1.0f / m_ZSize;
            
            // initialize transform matrix
            Quaternion rot = Quaternion.Euler(m_RotationWS);

            m_Transform.SetTRS(m_PositionWS, rot, Vector3.one);
            m_TransformDir.SetTRS(Vector3.zero, rot, Vector3.one);
            
            m_PositionCenterWS = m_PositionWS + new Vector3(m_XSize * 0.5f, 0, m_ZSize * 0.5f);
        }
        
        public Vector3 LocalToWorld(Vector3 positionLS)
        {
            return m_Transform.MultiplyPoint(positionLS);
        }
        
        public Vector3 LocalToWorldDir(Vector3 directiolLS)
        {
            return m_TransformDir.MultiplyVector(directiolLS);
        }
        
        public Vector3 WorldToLocal(Vector3 positionWS)
        {
            return m_Transform.inverse.MultiplyPoint(positionWS);
        }
        
        public Vector3 WorldToLocalDir(Vector3 directionWS)
        {
            return m_TransformDir.inverse.MultiplyVector(directionWS);
        }
    }
}

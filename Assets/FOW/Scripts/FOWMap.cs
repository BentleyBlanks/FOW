using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MoleMole
{
    public class FOWMap
    {
        #region Public

        public FOWMapData  m_MapData  = null;
        public FOWMaskData m_MaskData = null;

        #endregion

        #region Private

        private object m_Lock;
        private WaitCallback m_MaskCalculatorCallback;

        #endregion

        public FOWMap(FOWData data)
        {
            Init(data);
        }

        public void Init(FOWData data)
        {
            if (m_MapData == null)
            {
                m_MapData = new FOWMapData();
                m_MapData.Init(data);
            }


            if (m_MaskData == null)
            {
                m_MaskData = new FOWMaskData();
                m_MaskData.Init(data);
            }

            if(m_Lock == null)
                m_Lock = new object();
            
            if(m_MaskCalculatorCallback == null)
                m_MaskCalculatorCallback = new WaitCallback(UpdateMaskAsync);
        }
        
        public void Release()
        {
            lock (m_Lock)
            {
                if (m_MapData != null)
                {
                    m_MapData.DeInit();
                    m_MapData = null;
                }

                if (m_MaskData != null)
                {
                    m_MaskData.DeInit();
                    m_MaskData = null;
                }
            }
            m_Lock = null;
        }

        public void Update()
        {
            m_MapData.Update();
            m_MaskData.Update();
        }
        
        public void UpdateMask(List<FOWPlayerData> playerDataList)
        {
            ThreadPool.QueueUserWorkItem(m_MaskCalculatorCallback, playerDataList);
        }

        private void UpdateMaskAsync(object playerDataList)
        {
            var list = (List<FOWPlayerData>)playerDataList;

            lock (m_Lock)
            {
                for (int i = 0; i < list.Count; i++)
                    m_MaskData.Calculate(list[i], m_MapData);

                m_MaskData.UpdateColorBuffer();
            }
        }
        
        public void GenerateMapData()
        {
            m_MapData.GenerateMapData();
        }
    }
}

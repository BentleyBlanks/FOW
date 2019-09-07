using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FOWEffect))]
public class FOWEffectEditor : Editor
{
    private FOWEffect m_Target;
    
    void OnEnable()
    {
        m_Target = (FOWEffect) target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // for preview FOW shadow textures info
        var fowShadow = m_Target.m_FOWShadow;
        if (fowShadow != null)
        {
            switch (m_Target.m_FOWShadowType)
            {
            case FOWShadowType.FOWSHADOW_TYPE_SDF:
                var shadowSDF = (FOWShadowSDF) fowShadow;
                if (shadowSDF.fowTexture != null)
                    GUILayout.Box(shadowSDF.fowTexture);

                if (shadowSDF.sdfTexture != null)
                    GUILayout.Box(shadowSDF.sdfTexture);

                if (shadowSDF.mapDataTexture != null)
                    GUILayout.Box(shadowSDF.mapDataTexture);
                            
                // regenerate texture infomation
                if (GUILayout.Button("Generate Textures"))
                    m_Target.GenerateFOWTexture();
                
                if (GUILayout.Button("Force Generate Textures"))
                {
                    var backup = shadowSDF.m_TextureSavePath;
                    shadowSDF.m_TextureSavePath = "";
                    m_Target.GenerateFOWTexture();
                    shadowSDF.m_TextureSavePath = backup;
                }

                shadowSDF.m_TextureSavePath = GUILayout.TextArea(shadowSDF.m_TextureSavePath);
                
                if (GUILayout.Button("Save Textures"))
                {
                    shadowSDF.SaveFOWTexture();
                }
                break;

            case FOWShadowType.FOWSHADOW_TYPE_DOTA:
                var shadowDota = (FOWShadowDota) fowShadow;
                if (shadowDota.fowTexture != null)
                    GUILayout.Box(shadowDota.fowTexture);

                if (shadowDota.maskDataTexture != null)
                    GUILayout.Box(shadowDota.maskDataTexture);

                if (shadowDota.mapDataTexture != null)
                    GUILayout.Box(shadowDota.mapDataTexture);

                // regenerate texture infomation
                if (GUILayout.Button("Generate"))
                    shadowDota.Pregenerate();
                break;
            }
        }
    }
}

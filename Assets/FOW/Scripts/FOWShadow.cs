using System.Collections;
using System.Collections.Generic;
using MoleMole;
using UnityEngine;

public enum FOWShadowType
{
    FOWSHADOW_TYPE_DOTA,
    FOWSHADOW_TYPE_SDF
}

public interface FOWShadow
{
    void Init(FOWData data);

    void Destroy();

    void Update();

    void FixedUpdate();
    
    void UpdatePlayerData(List<FOWPlayerData> playerDataList);

    void Pregenerate();

    bool SaveFOWTexture();
    
    bool ReadFOWTexture();
    
    RenderTexture RenderFOWTexture();

    float GetLerpValue();

    FOWShadowType GetFowShadowType();
}

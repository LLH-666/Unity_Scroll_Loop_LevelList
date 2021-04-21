using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PackInfo
{
    #region Member Variables

    public int Id;
    public string DisplayName;
    public List<LevelData> LevelDatas = new List<LevelData>();

    #endregion

    #region Properties

    public int FromLevelNumber
    {
        get { return LevelDatas[0].Lv; }
    }

    public int ToLevelNumber
    {
        get { return FromLevelNumber + (LevelDatas.Count - 1); }
    }

    public int NumLevelsInPack
    {
        get { return ToLevelNumber - FromLevelNumber + 1; }
    }

    #endregion
}
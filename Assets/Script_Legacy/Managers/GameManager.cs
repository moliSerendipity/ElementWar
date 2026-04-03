using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingleMonoBase<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        LuaManager.Instance.PreloadAllLuaScripts(() =>
        {
            LuaManager.Instance.StartGame();
        });
    }

    void Update()
    {
        
    }
}

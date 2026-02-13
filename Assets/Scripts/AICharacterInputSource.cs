using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICharacterInputSource : MonoBehaviour, IInputSource
{
    private void Awake()
    {

    }

    /// <summary>
    /// 获取当前帧输入数据
    /// </summary>
    /// <returns>当前帧的输入数据</returns>
    public InputFrame GetInputFrame()
    {
        throw new System.NotImplementedException();
    }

    private void ResetInputFrame()
    {

    }
}
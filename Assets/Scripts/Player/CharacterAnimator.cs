using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private Animator animtor;                                                   // 角色动画控制器组件

    #region 动画参数哈希值
    private static readonly int speedHash = Animator.StringToHash("Speed");     // 速度参数哈希值
    private static readonly int inputXHash = Animator.StringToHash("InputX");   // 输入X参数哈希值
    #endregion

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponView : MonoBehaviour
{
    [Tooltip("枪口发出射线和火光的具体位置节点")]
    public Transform muzzlePoint;
    [Tooltip("左手IK需要强行吸附过去的位置节点")]
    public Transform leftHandIKTarget;
}

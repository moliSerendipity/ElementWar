using UnityEngine;

// 怪物实体根节点 (挂在怪物的最顶层)
public class EnemyEntity : MonoBehaviour
{
    [HideInInspector]
    public int uid;

    [Header("怪物的配置信息")]
    public int configID = 81001;
    public int level = 85;

    [Header("视觉与 Shader 配置")]
    [Tooltip("怪物的主要 MeshRenderer，用于元素反应染色")]
    public Renderer mainRenderer;

    // 全局 UID 累加器，确保每次实例化分配的 ID 是正数且唯一
    private static int globalEnemyUIDIndex = 10000;
    // 材质属性块，用于动态改 Shader 而不打断 GPU 合批
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        // 赋予安全且全局唯一的递增 ID
        uid = ++globalEnemyUIDIndex;
        propertyBlock = new MaterialPropertyBlock();
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<Renderer>();
    }

    /// <summary>
    /// 更新怪物的元素 Shader 状态 (供 Lua 直接调用)
    /// </summary>
    /// <param name="_elementType">元素类型</param>
    /// <param name="_intensity">发光强度 (0.0 ~ 1.0)</param>
    public void SetElementShaderState(int _elementType, float _intensity)
    {
        if (mainRenderer == null)
            return;
        // 获取并缓存当前的渲染属性
        mainRenderer.GetPropertyBlock(propertyBlock);
        // 将 Lua 的ElementType 映射为视觉表现颜色
        Color elementColor = Color.white;
        switch (_elementType)
        {
            case 2: elementColor = new Color(1.0f, 0.3f, 0.0f);
                break;
            case 3: elementColor = new Color(0.6f, 0.0f, 1.0f);
                break;
            case 4: elementColor = new Color(0.0f, 0.8f, 1.0f);
                break;
            case 1:
            default: _intensity = 0f;
                break;
        }
        propertyBlock.SetColor("_ElementColor", elementColor);
        propertyBlock.SetFloat("_ElementIntensity", _intensity);
        mainRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// 触发元素反应 Shader 效果
    /// </summary>
    /// <param name="_reactionName"></param>
    public void TriggerReactionShader(string _reactionName)
    {
        if (mainRenderer == null)
            return;
        // 获取并缓存当前的渲染属性
        mainRenderer.GetPropertyBlock(propertyBlock);


        propertyBlock.SetFloat("_ReactionTime", Time.time);
        mainRenderer.SetPropertyBlock(propertyBlock);
    }
}

using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色输入路由器。
    /// 只负责从当前输入源构建 RawInput，不再把输入写成多份缓存后再层层转发。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterInputRouter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSourceBehaviour;

        private ICharacterInputSource currentInputSource;

        public ICharacterInputSource CurrentInputSource => currentInputSource;

        private void Awake()
        {
            ResolveInputSource();
        }

        private void OnEnable()
        {
            ResolveInputSource();
        }

        public void SetInputSource(ICharacterInputSource _inputSource)
        {
            currentInputSource = _inputSource;
        }

        public bool SetInputSource(MonoBehaviour _inputSourceBehaviour)
        {
            inputSourceBehaviour = _inputSourceBehaviour;
            return ResolveInputSource();
        }

        /// <summary>
        /// 构建当前帧 RawInput。
        /// 这是角色主链唯一合法的原始输入读取入口。
        /// </summary>
        public bool TryBuildRawInputContext(out InputContext _inputContext)
        {
            _inputContext = InputContext.Empty;

            if (currentInputSource == null && !ResolveInputSource())
            {
                return false;
            }

            return currentInputSource.TryBuildInputContext(out _inputContext);
        }

        private bool ResolveInputSource()
        {
            if (inputSourceBehaviour is ICharacterInputSource source)
            {
                currentInputSource = source;
                return true;
            }

            currentInputSource = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveInputSource();
        }
#endif
    }
}

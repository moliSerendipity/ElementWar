#if UNITY_EDITOR
using Game.Definition.Enemy;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Enemy
{
    /// <summary>
    /// EnemyAttackConfig 自定义 Inspector。
    ///
    /// 功能：
    /// 1. 不同 ShapeType 只显示对应的形状参数
    /// 2. 显示从 AnimationClip 自动读取的 BaseDuration
    /// 3. 显示自动推导的有效打击距离，并在 maxUseRange 超出时警告
    /// </summary>
    [CustomEditor(typeof(EnemyAttackConfig))]
    public sealed class EnemyAttackConfigEditor : UnityEditor.Editor
    {
        #region Serialized Properties

        // --- ConfigBase ---
        private SerializedProperty propConfigId;
        private SerializedProperty propDisplayName;
        private SerializedProperty propDescription;
        private SerializedProperty propIsEnabled;

        // --- Animation ---
        private SerializedProperty propAnimationClip;
        private SerializedProperty propAnimationTriggerName;
        private SerializedProperty propDamageNormalizedTime;

        // --- Damage ---
        private SerializedProperty propDamageMultiplier;
        private SerializedProperty propDamageKind;
        private SerializedProperty propIsAreaOfEffect;

        // --- Shape ---
        private SerializedProperty propShapeType;
        private SerializedProperty propOffsetDistance;
        private SerializedProperty propRadius;
        private SerializedProperty propSectorHalfAngle;
        private SerializedProperty propBoxHalfDepth;
        private SerializedProperty propBoxHalfWidth;
        private SerializedProperty propBoxHalfHeight;

        // --- Selection ---
        private SerializedProperty propMaxUseRange;
        private SerializedProperty propMinUseRange;
        private SerializedProperty propSelectionWeight;

        #endregion

        private void OnEnable()
        {
            // ConfigBase fields
            propConfigId = serializedObject.FindProperty("configId");
            propDisplayName = serializedObject.FindProperty("displayName");
            propDescription = serializedObject.FindProperty("description");
            propIsEnabled = serializedObject.FindProperty("isEnabled");

            // Animation
            propAnimationClip = serializedObject.FindProperty("animationClip");
            propAnimationTriggerName = serializedObject.FindProperty("animationTriggerName");
            propDamageNormalizedTime = serializedObject.FindProperty("damageNormalizedTime");

            // Damage
            propDamageMultiplier = serializedObject.FindProperty("damageMultiplier");
            propDamageKind = serializedObject.FindProperty("damageKind");
            propIsAreaOfEffect = serializedObject.FindProperty("isAreaOfEffect");

            // Shape
            propShapeType = serializedObject.FindProperty("shapeType");
            propOffsetDistance = serializedObject.FindProperty("offsetDistance");
            propRadius = serializedObject.FindProperty("radius");
            propSectorHalfAngle = serializedObject.FindProperty("sectorHalfAngle");
            propBoxHalfDepth = serializedObject.FindProperty("boxHalfDepth");
            propBoxHalfWidth = serializedObject.FindProperty("boxHalfWidth");
            propBoxHalfHeight = serializedObject.FindProperty("boxHalfHeight");

            // Selection
            propMaxUseRange = serializedObject.FindProperty("maxUseRange");
            propMinUseRange = serializedObject.FindProperty("minUseRange");
            propSelectionWeight = serializedObject.FindProperty("selectionWeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EnemyAttackConfig config = (EnemyAttackConfig)target;

            DrawConfigBaseSection();
            DrawAnimationSection(config);
            DrawDamageSection();
            DrawShapeSection(config);
            DrawSelectionSection(config);

            serializedObject.ApplyModifiedProperties();
        }

        #region Sections

        /// <summary>
        /// ConfigBase 通用字段。
        /// </summary>
        private void DrawConfigBaseSection()
        {
            EditorGUILayout.LabelField("Config Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propConfigId);
            EditorGUILayout.PropertyField(propDisplayName);
            EditorGUILayout.PropertyField(propDescription);
            EditorGUILayout.PropertyField(propIsEnabled);
            EditorGUILayout.Space(8);
        }

        /// <summary>
        /// Animation 区域：clip 引用、trigger 名、伤害时间点，以及自动计算的时长信息。
        /// </summary>
        private void DrawAnimationSection(EnemyAttackConfig _config)
        {
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(propAnimationClip);
            EditorGUILayout.PropertyField(propAnimationTriggerName);
            EditorGUILayout.PropertyField(propDamageNormalizedTime);

            // 显示自动读取的时长信息。
            float baseDuration = _config.BaseDuration;
            float windupTime = baseDuration * _config.DamageNormalizedTime;
            float recoveryTime = baseDuration * (1f - _config.DamageNormalizedTime);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Clip Duration (auto)", baseDuration);
            EditorGUILayout.FloatField("Windup Duration (auto)", windupTime);
            EditorGUILayout.FloatField("Recovery Duration (auto)", recoveryTime);
            EditorGUI.EndDisabledGroup();

            if (_config.AnimationClip == null)
            {
                EditorGUILayout.HelpBox("未设置 AnimationClip，时长将回退为 1.0s。", MessageType.Warning);
            }

            EditorGUILayout.Space(8);
        }

        /// <summary>
        /// Damage 区域。
        /// </summary>
        private void DrawDamageSection()
        {
            EditorGUILayout.LabelField("Damage", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propDamageMultiplier);
            EditorGUILayout.PropertyField(propDamageKind);
            EditorGUILayout.PropertyField(propIsAreaOfEffect);
            EditorGUILayout.Space(8);
        }

        /// <summary>
        /// Shape 区域：根据 shapeType 条件显示对应的参数。
        /// </summary>
        private void DrawShapeSection(EnemyAttackConfig _config)
        {
            EditorGUILayout.LabelField("Detection Shape", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(propShapeType);
            EditorGUILayout.PropertyField(propOffsetDistance);

            // 根据形状类型只显示相关参数。
            AttackShapeType currentShape = (AttackShapeType)propShapeType.enumValueIndex;

            switch (currentShape)
            {
                case AttackShapeType.Sphere:
                    EditorGUILayout.PropertyField(propRadius, new GUIContent("Sphere Radius"));
                    break;

                case AttackShapeType.Sector:
                    EditorGUILayout.PropertyField(propRadius, new GUIContent("Sector Radius"));
                    EditorGUILayout.PropertyField(propSectorHalfAngle);
                    break;

                case AttackShapeType.Box:
                    EditorGUILayout.PropertyField(propBoxHalfDepth, new GUIContent("Box Half Depth (Z)"));
                    EditorGUILayout.PropertyField(propBoxHalfWidth, new GUIContent("Box Half Width (X)"));
                    EditorGUILayout.PropertyField(propBoxHalfHeight, new GUIContent("Box Half Height (Y)"));
                    break;
            }

            // 显示自动推导的有效打击距离。
            float effectiveRange = _config.EffectiveStrikeRange;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Effective Strike Range (auto)", effectiveRange);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
        }

        /// <summary>
        /// Selection 区域：使用距离和权重，以及距离校验警告。
        /// </summary>
        private void DrawSelectionSection(EnemyAttackConfig _config)
        {
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propMinUseRange);
            EditorGUILayout.PropertyField(propMaxUseRange);
            EditorGUILayout.PropertyField(propSelectionWeight);

            // 距离校验警告。
            float effectiveRange = _config.EffectiveStrikeRange;
            if (_config.MaxUseRange > effectiveRange + 0.01f)
            {
                EditorGUILayout.HelpBox(
                    $"maxUseRange ({_config.MaxUseRange:F2}) 超过有效打击距离 ({effectiveRange:F2})，" +
                    $"敌人可能在打不到目标的距离发起攻击。",
                    MessageType.Warning);
            }

            if (_config.MaxUseRange <= _config.MinUseRange)
            {
                EditorGUILayout.HelpBox("maxUseRange 必须大于 minUseRange。", MessageType.Error);
            }

            EditorGUILayout.Space(8);
        }

        #endregion
    }
}
#endif

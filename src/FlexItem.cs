using System;
using UnityEngine;

namespace Flexbox
{
    public enum AlignSelf
    {
        Auto = 0,
        Stretch,
        FlexStart,
        FlexEnd,
        Center,
        // Baseline // todo
    }

    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public sealed class FlexItem : MonoBehaviour
    {
        // todo
        // [Tooltip("同级FlexItem的顺序，越小越靠前")]
        // [SerializeField] private int m_Order;

        [Tooltip("扩展比例，大于0表示可扩展")]
        [SerializeField] private float m_FlexGrow;

        [Tooltip("收缩比例，大于0表示可收缩")]
        [SerializeField] private float m_FlexShrink = 1;

        [Tooltip("基础尺寸")]
        [SerializeField] private Vector2 m_FlexBasis = new Vector2(-1f, -1f);

        [Tooltip("最小尺寸")]
        [SerializeField] private Vector2 m_MinSize = new Vector2(-1f, -1f);

        [Tooltip("最大尺寸")]
        [SerializeField] private Vector2 m_MaxSize = new Vector2(-1f, -1f);

        [Tooltip("允许该FlexItem覆盖FlexContainer的AlignItems属性")]
        [SerializeField] private AlignSelf m_AlignSelf;

        public float FlexGrow
        {
            get => Math.Max(m_FlexGrow, 0f);
        }

        public float FlexShrink
        {
            get => Math.Max(m_FlexShrink, 0f);
        }

        public Vector2 FlexBasis
        {
            get => m_FlexBasis;
        }

        public Vector2 MinSize
        {
            get => m_MinSize;
        }

        public Vector2 MaxSize
        {
            get => m_MaxSize;
        }

        public AlignSelf AlignSelf
        {
            get => m_AlignSelf;
        }

        private void TryMakeContainerRebuld()
        {
            if (transform.parent.TryGetComponent<FlexContainer>(out var container))
            {
                container.SetDirty();
            }
        }

        private void OnEnable()
        {
            TryMakeContainerRebuld();
        }

        private void OnDisable()
        {
            TryMakeContainerRebuld();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TryMakeContainerRebuld();
        }
#endif
    }
}

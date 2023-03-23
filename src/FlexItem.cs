using System;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public sealed class FlexItem : UIBehaviour
    {
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

        [Tooltip("同级FlexItem的顺序，越小越靠前")]
        [SerializeField] private int m_Order;

        private DrivenRectTransformTracker m_Tracker;

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

        public int Order
        {
            get => m_Order;
        }

        private void TryMakeContainerRebuld()
        {
            if (transform.parent != null && transform.parent.TryGetComponent<FlexContainer>(out var container))
            {
                m_Tracker.Clear();
                // 锁定position、Anchors和Size
                m_Tracker.Add(this, transform as RectTransform,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                container.SetDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TryMakeContainerRebuld();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            TryMakeContainerRebuld();

            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            TryMakeContainerRebuld();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            TryMakeContainerRebuld();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            TryMakeContainerRebuld();
        }
#endif
    }
}

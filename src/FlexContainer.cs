using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Flexbox
{
    public enum Axis
    {
        x = 0,
        y = 1,
    }

    public enum FlexDirection
    {
        Row = 0,
        Column,
        RowReverse,
        ColumnReverse,
    }

    public enum FlexWrap
    {
        Nowrap = 0,
        wrap,
        // WrapReverse v
    }

    public enum JustifyContent
    {
        FlexStart = 0,
        FlexEnd,
        Center,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly,
    }

    public enum AlignItems
    {
        Center = 0,
        FlexStart,
        FlexEnd,
        Stretch,
        // Baseline // todo
    }

    public enum AlignContent
    {
        Normal = 0,
        FlexStart,
        FlexEnd,
        Center,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly,
        Stretch,
    }

    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public sealed class FlexContainer : UIBehaviour, ICanvasElement, ILayoutElement
    {
        [Tooltip("主轴方向（从左往右/从上往下）")]
        [SerializeField] private FlexDirection m_FlexDirection;

        [Tooltip("主轴布局方式")]
        [SerializeField] private JustifyContent m_JustifyContent;

        [Tooltip("交叉轴布局方式")]
        [SerializeField] private AlignItems m_AlignItems;

        // todo
        // [Tooltip("当需要收缩时，是否能换行")]
        // [SerializeField] private FlexWrap m_FlexWrap;

        // todo
        // [Tooltip("交叉轴换行布局方式")]
        // [SerializeField] private AlignContent m_AlignContent;

        [Tooltip("间距")]
        [SerializeField] private Vector2 m_Gap;

        [Tooltip("自动宽度")]
        [SerializeField] private bool m_AutoWidth;

        [Tooltip("自动高度")]
        [SerializeField] private bool m_AutoHeight;

        private DrivenRectTransformTracker m_Tracker;

        // 是否需要更新，跟随canvas更新策略
        private bool m_isDirty;

        // 布局有关的组件
        private readonly List<(RectTransform rect, FlexItem flex)> m_Items = new List<(RectTransform, FlexItem)>();

        [NonSerialized] private RectTransform m_Rect;
        private RectTransform ContainerRect
        {
            get
            {
                if (m_Rect == null)
                {
                    m_Rect = GetComponent<RectTransform>();
                }
                return m_Rect;
            }
        }

#if UNITY_EDITOR
        public FlexDirection FlexDirection => m_FlexDirection;
#endif

        public float minWidth => 0;

        public float preferredWidth
        {
            get
            {
                if (m_AutoWidth)
                {
                    DoLayout();
                }
                return ContainerRect.rect.size[0];
            }
        }

        public float flexibleWidth => 0;

        public float minHeight => 0;

        public float preferredHeight
        {
            get
            {
                if (m_AutoHeight)
                {
                    DoLayout();
                }
                return ContainerRect.rect.size[1];
            }
        }

        public float flexibleHeight => 0;

        public int layoutPriority => 0;

        private void CreateChildrenItems()
        {
            m_Items.Clear();

            for (var i = 0; i < ContainerRect.childCount; i++)
            {
                // 隐藏的不参与布局
                if (!(ContainerRect.GetChild(i) is RectTransform childRect) || !childRect.gameObject.activeSelf)
                {
                    continue;
                }

                var flex = childRect.GetComponent<FlexItem>();

                // 只找出有FlexItem的item
                if (flex == null)
                {
                    continue;
                }

                // 左上角
                childRect.anchorMin = Vector2.up;
                childRect.anchorMax = Vector2.up;

                m_Items.Add((childRect, flex));
            }

            // items按order升序排列
            m_Items.Sort((a, b) => a.flex.Order - b.flex.Order);

            // 反转
            if (m_FlexDirection == FlexDirection.RowReverse || m_FlexDirection == FlexDirection.ColumnReverse)
            {
                m_Items.Reverse();
            }
        }

        private float GetActualSizeByAxis(int index, int axis, float size)
        {
            var flex = m_Items[index].flex;
            var minSize = flex.MinSize[axis];
            var maxSize = flex.MaxSize[axis];

            if (minSize >= 0)
            {
                size = Math.Max(size, minSize);
            }
            if (maxSize >= 0)
            {
                size = Math.Min(size, maxSize);
            }

            return size;
        }

        /// <summary>
        /// # 计算所有一级`FlexItem`的实际尺寸（宽度或者高度，视主轴而定）
        /// ## 算法步骤
        /// 1. 遍历算出所有item的扩缩前尺寸集合`itemSizeList`
        /// 2. 同时找出所有可扩展的item下标集合`growableItemIndexList`
        /// 3. 同时找出所有可收缩的item下标集合`shrinkableItemIndexList`
        /// 4. 根据容器尺寸和`itemSizeList.Sum`相减计算剩余空间`restSpace`
        /// 5. if restSpace > 0 && growableItemIndexList.Count > 0 goto 6; else if restSpace < 0 && shrinkableItemIndexList.Count > 0 goto 8; else goto 10
        /// 6. 根据扩展算法和`MaxSize`计算`growableItemIndexList`扩展后尺寸，更新`itemSizeList`
        /// 7. goto 4
        /// 8. 根据收缩算法和`MinSize`计算`shrinkableItemIndexList`收缩后尺寸，更新`itemSizeList`
        /// 9. goto 4
        /// 10. end
        /// ## 算法优化
        /// 1. `restSpace`一旦确定正负，则符号不再改变，可以限制第5步始终走到同一个if分支，或者else
        /// 2. 小数判定相等存在精度问题
        /// </summary>
        private void DoLayout()
        {
            CreateChildrenItems();

            if (m_Items.Count == 0)
            {
                return;
            }

            #region axis

            // 主轴是x还是y
            var mainAxis = (int)(m_FlexDirection == FlexDirection.Row || m_FlexDirection == FlexDirection.RowReverse ? Axis.x : Axis.y);
            var crossAxis = 1 - mainAxis;

            // 主轴是否x轴
            var xIsMainAxis = mainAxis == (int)Axis.x;

            // x作为主轴的时候向右为正方向
            // y作为主轴的时候向下为负方向
            var mainPositionScale = xIsMainAxis ? 1 : -1;

            Vector2 createVector2AccordingMainAxis(float x, float y) => xIsMainAxis ? new Vector2(x, y) : new Vector2(y, x);

            #endregion

            #region init

            // 空白间距是固定的
            var gapMainSize = m_Gap[mainAxis] * (m_Items.Count - 1);

            // 主轴上各个item的尺寸
            var itemMainSizeList = new List<float>();
            // 交叉轴上各个item的尺寸
            var itemCrossSizeList = new List<float>();
            // 当前可扩展的item索引
            var growableItemIndexList = new List<int>();
            // 当前可收缩的item索引
            var shrinkableItemIndexList = new List<int>();

            // 剩余空间，可正可负
            float getRestSpace() => ContainerRect.rect.size[mainAxis] - gapMainSize - itemMainSizeList.Sum();
            // 总的扩展
            // 支持不扩展完剩余空间
            float getTotalGrow() => Math.Max(growableItemIndexList.Sum(i => m_Items[i].flex.FlexGrow), 1f);
            // 总的收缩
            // 支持收缩不足完整所需要的空间
            float getTotalShrink() => Math.Min(shrinkableItemIndexList.Sum(i => m_Items[i].flex.FlexShrink), 1f);
            // 收缩算法分母值
            float getTotalShrinkSize() => shrinkableItemIndexList.Aggregate(0f, (ret, index) => ret + (itemMainSizeList[index] * m_Items[index].flex.FlexShrink));

            for (var index = 0; index < m_Items.Count; index++)
            {
                var item = m_Items[index];
                var flex = item.flex;
                var rect = item.rect;

                // 先计算交叉轴，TMP这种交叉轴大小会影响主轴大小
                var basicCrossSize = flex.FlexBasis[crossAxis];
                // 交叉轴就用初始大小
                var crossSize = basicCrossSize >= 0 ? basicCrossSize : LayoutUtility.GetPreferredSize(rect, crossAxis);
                // Stretch优先级更高
                if (flex.AlignSelf == AlignSelf.Stretch || (flex.AlignSelf == AlignSelf.Auto && m_AlignItems == AlignItems.Stretch))
                {
                    crossSize = ContainerRect.rect.size[crossAxis];
                }
                crossSize = GetActualSizeByAxis(index, crossAxis, crossSize);
                // 这里就要先设置交叉轴尺寸
                rect.sizeDelta = createVector2AccordingMainAxis(rect.rect.size[mainAxis], crossSize);
                itemCrossSizeList.Add(crossSize);

                // 再计算主轴
                var basicMainSize = flex.FlexBasis[mainAxis];
                // 结合LayoutElement，基础大小可变
                var mainSize = basicMainSize >= 0 ? basicMainSize : LayoutUtility.GetPreferredSize(rect, mainAxis);
                mainSize = GetActualSizeByAxis(index, mainAxis, mainSize);
                itemMainSizeList.Add(mainSize);

                if (flex.FlexGrow > 0)
                {
                    growableItemIndexList.Add(index);
                }
                if (flex.FlexShrink > 0)
                {
                    shrinkableItemIndexList.Add(index);
                }
            }

            var restSpace = getRestSpace();

            #endregion

            #region 计算item主轴尺寸

            // 先设置容器宽高
            if (m_AutoWidth || m_AutoHeight)
            {
                var autoMain = xIsMainAxis ? m_AutoWidth : m_AutoHeight;
                var autoCross = xIsMainAxis ? m_AutoHeight : m_AutoWidth;

                // 主轴平铺，交叉轴取最大尺寸
                ContainerRect.sizeDelta = createVector2AccordingMainAxis(ContainerRect.rect.size[mainAxis] - (autoMain ? restSpace : 0), autoCross ? itemCrossSizeList.Max() : ContainerRect.rect.size[crossAxis]);

                if (autoMain)
                {
                    restSpace = 0;
                }
            }

            // 扩展
            if (restSpace > 0)
            {
                while (growableItemIndexList.Count > 0 && restSpace > 0)
                {
                    var totalFlexGrow = getTotalGrow();
                    // 本轮循环已经固定尺寸的item集合
                    var fixedItemIndexList = new List<int>();
                    // 是否产生了多余的剩余空间
                    var hasMoreSpace = false;

                    foreach (var index in growableItemIndexList)
                    {
                        var flex = m_Items[index].flex;
                        var maxMainSize = flex.MaxSize[mainAxis];

                        var mainSize = itemMainSizeList[index];
                        mainSize += flex.FlexGrow / totalFlexGrow * restSpace;

                        if (maxMainSize >= 0)
                        {
                            if (mainSize >= maxMainSize)
                            {
                                // size不可能再改变
                                fixedItemIndexList.Add(index);
                            }
                            if (mainSize > maxMainSize)
                            {
                                mainSize = maxMainSize;
                                hasMoreSpace = true;
                            }
                        }

                        itemMainSizeList[index] = Math.Max(mainSize, 0f);
                    }

                    // 说明会出现新的剩余空间
                    // 需要继续计算
                    if (hasMoreSpace)
                    {
                        // 排除掉已经固定的
                        growableItemIndexList = growableItemIndexList.Except(fixedItemIndexList).ToList();
                        // 重新计算
                        restSpace = getRestSpace();
                    }
                    // 到位了
                    else
                    {
                        growableItemIndexList.Clear();
                    }
                }
            }
            // 收缩或者换行
            else if (restSpace < 0)
            {
                while (shrinkableItemIndexList.Count > 0 && restSpace < 0)
                {
                    var totalShrink = getTotalShrink();
                    var totalShrinkSize = getTotalShrinkSize();
                    // 本轮循环已经固定尺寸的item集合
                    var fixedItemIndexList = new List<int>();
                    // 是否需要压缩更多空间
                    var needMoreSpace = false;

                    foreach (var index in shrinkableItemIndexList)
                    {
                        var flex = m_Items[index].flex;
                        var minMainSize = flex.MinSize[mainAxis];

                        var mainSize = itemMainSizeList[index];
                        if (totalShrinkSize > 0)
                        {
                            mainSize += totalShrink * restSpace * flex.FlexShrink * mainSize / totalShrinkSize;
                        }

                        if (minMainSize >= 0)
                        {
                            if (mainSize <= minMainSize)
                            {
                                // size不可能再改变
                                fixedItemIndexList.Add(index);
                            }
                            if (mainSize < minMainSize)
                            {
                                mainSize = minMainSize;
                                needMoreSpace = true;
                            }
                        }

                        itemMainSizeList[index] = Math.Max(mainSize, 0f);
                    }

                    // 说明需要更多压缩空间
                    // 需要继续计算
                    if (needMoreSpace)
                    {
                        // 排除掉已经固定的
                        shrinkableItemIndexList = shrinkableItemIndexList.Except(fixedItemIndexList).ToList();
                        // 重新计算
                        restSpace = getRestSpace();
                    }
                    // 到位了
                    else
                    {
                        shrinkableItemIndexList.Clear();
                    }
                }
            }

            for (var index = 0; index < itemMainSizeList.Count; index++)
            {
                var item = m_Items[index];
                var rect = item.rect;
                var basicCrossSize = item.flex.FlexBasis[crossAxis];
                var crossSize = basicCrossSize >= 0 ? basicCrossSize : rect.rect.size[crossAxis];

                rect.sizeDelta = createVector2AccordingMainAxis(itemMainSizeList[index], itemCrossSizeList[index]);
            }

            #endregion

            #region JustifyContent 主轴布局方式

            // 对齐方式的整体偏移量
            var totalMainDelta = 0f;
            var spacing = m_Gap[mainAxis];
            // 左对齐
            if (m_JustifyContent == JustifyContent.FlexStart)
            {
            }
            // 右对齐
            else if (m_JustifyContent == JustifyContent.FlexEnd)
            {
                totalMainDelta = getRestSpace();
            }
            // 居中
            else if (m_JustifyContent == JustifyContent.Center)
            {
                totalMainDelta = getRestSpace() / 2;
            }
            // 两边顶到头，剩余空间平分
            else if (m_JustifyContent == JustifyContent.SpaceBetween)
            {
                spacing += getRestSpace() / Math.Max(m_Items.Count - 1, 1);
            }
            // 两边间距为中间一半
            else if (m_JustifyContent == JustifyContent.SpaceAround)
            {
                var halfSpacing = getRestSpace() / ((Math.Max(m_Items.Count - 1, 0) * 2) + 2);
                totalMainDelta = halfSpacing;
                spacing += halfSpacing * 2;
            }
            // 两边跟中间一样间距
            else if (m_JustifyContent == JustifyContent.SpaceEvenly)
            {
                var innerSpacing = getRestSpace() / (m_Items.Count + 1);
                totalMainDelta = innerSpacing;
                spacing += innerSpacing;
            }

            var prevMainDelta = totalMainDelta;
            foreach (var item in m_Items)
            {
                var rect = item.rect;
                rect.anchoredPosition = createVector2AccordingMainAxis((prevMainDelta + (rect.rect.size[mainAxis] / 2)) * mainPositionScale, rect.anchoredPosition[crossAxis]);
                prevMainDelta += rect.rect.size[mainAxis] + spacing;
            }

            #endregion

            #region AlignItems 交叉轴布局方式

            for (var index = 0; index < m_Items.Count; index++)
            {
                var item = m_Items[index];
                var rect = item.rect;
                var flex = item.flex;

                // 顶部对齐
                if (flex.AlignSelf == AlignSelf.FlexStart || (flex.AlignSelf == AlignSelf.Auto && m_AlignItems == AlignItems.FlexStart))
                {
                    rect.anchoredPosition = createVector2AccordingMainAxis(rect.anchoredPosition[mainAxis], -rect.rect.size[crossAxis] / 2 * mainPositionScale);
                }

                // 居中对齐，包括拉伸
                if (flex.AlignSelf == AlignSelf.Center || (flex.AlignSelf == AlignSelf.Auto && m_AlignItems == AlignItems.Center)
                    || flex.AlignSelf == AlignSelf.Stretch || (flex.AlignSelf == AlignSelf.Auto && m_AlignItems == AlignItems.Stretch))
                {
                    rect.anchoredPosition = createVector2AccordingMainAxis(rect.anchoredPosition[mainAxis], -ContainerRect.rect.size[crossAxis] / 2 * mainPositionScale);
                }

                // 底部对齐
                if (flex.AlignSelf == AlignSelf.FlexEnd || (flex.AlignSelf == AlignSelf.Auto && m_AlignItems == AlignItems.FlexEnd))
                {
                    rect.anchoredPosition = createVector2AccordingMainAxis(rect.anchoredPosition[mainAxis], ((rect.rect.size[crossAxis] / 2) - ContainerRect.rect.size[crossAxis]) * mainPositionScale);
                }
            }

            #endregion
        }

#if UNITY_EDITOR
        public void ForceDoLayout()
        {
            DoLayout();
        }
#endif

        public void SetDirty()
        {
            if (!IsActive())
            {
                return;
            }

            CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(this);
            m_isDirty = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MarkDrivenSize();
            SetDirty();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();

            base.OnDisable();
            CanvasUpdateRegistry.UnRegisterCanvasElementForLayoutRebuild(this);
            m_isDirty = false;
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            MarkDrivenSize();
            SetDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            MarkDrivenSize();
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            MarkDrivenSize();
            SetDirty();
        }
#endif

        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                // 等ContentSizeFilter等组件计算完毕
                case CanvasUpdate.PostLayout:
                    if (m_isDirty)
                    {
                        DoLayout();
                    }
                    break;
            }
        }

        public void LayoutComplete()
        {
            m_isDirty = false;
        }

        public void GraphicUpdateComplete()
        {
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        private void MarkDrivenSize()
        {
            m_Tracker.Clear();

            // 锁定Size
            var prop = DrivenTransformProperties.None;
            if (m_AutoWidth)
            {
                prop |= DrivenTransformProperties.SizeDeltaX;
            }
            if (m_AutoHeight)
            {
                prop |= DrivenTransformProperties.SizeDeltaY;
            }

            if (prop != DrivenTransformProperties.None)
            {
                m_Tracker.Add(this, transform as RectTransform, prop);
            }
        }
    }
}

# Flexbox弹性布局

## 什么是Flexbox

![](https://drafts.csswg.org/css-flexbox/images/flex-direction-terms.svg)

[Flexbox详细介绍（必读！！！）](https://css-tricks.com/snippets/css/a-guide-to-flexbox/)

> [完整草案（慎点）](https://drafts.csswg.org/css-flexbox/)

## 关键算法

### 扩大算法

按照`grow`权重分配多余空间即可，即`grow / sum(grow)`。

举例：

水平布局，container宽度为1000px，item宽度分别为300px，200px，100px，`grow`值分别为1、2、3。

则剩余1000 - 300 - 200 - 100 = 400px，扩大后item的宽度分别为

300 + 1 / (1 + 2 + 3) * 400 = 366.67px

200 + 2 / (1 + 2 + 3) * 400 = 333.33px

100 + 3 / (1 + 2 + 3) * 400 = 300px

> 特别地，当sum(grow) < 1的时候，sum(grow) = max(sum(grow), 1)，相当于剩余空间打了折扣，所以所有item加在一起不能占满容器空间。

若`grow`值分别为0.1、0.2、0.3，则item宽度最后分别为

300 + 0.1 / 1 * 400 = 340px

200 + 0.2 / 1 * 400 = 280px

100 + 0.3 / 1 * 400 = 220px

所以总结起来，每个item宽度的计算公式为

width + grow / sum(grow) * (containerWidth - sum(width)) * min(sum(grow), 1)

### 缩小算法

和扩大算法略有不同，如果简单按照`shrink`权重分配需要缩小的空间，则存在某个item宽度最后小于0的情况。

实际的算法是将item宽度一起加入计算权重，`shrink * width / sum(shrink * width)`。

举例：

上例中container宽度变为500px，`shrink`值分别为1、2、3。

需要缩小的宽度为300 + 200 + 100 - 500 = 100px，缩小后item宽度分别为

300 - 1 * 300 / (1 * 300 + 2 * 200 + 3 * 100) * 100 = 270px

200 - 2 * 200 / (1 * 300 + 2 * 200 + 3 * 100) * 100 = 160px

100 - 3 * 100 / (1 * 300 + 2 * 200 + 3 * 100) * 100 = 70px

> 同grow，特别地，当sum(shrink) < 1的时候，缩小空间也要打折扣。

若`shrink`值分别为0.1、0.2、0.3，则item宽度最后分别为

300 - 0.1 * 300 / (0.1 * 300 + 0.2 * 200 + 0.3 * 100) * 100 * 0.6 = 282px

200 - 0.2 * 200 / (0.1 * 300 + 0.2 * 200 + 0.3 * 100) * 100 * 0.6 = 176px

100 - 0.3 * 100 / (0.1 * 300 + 0.2 * 200 + 0.3 * 100) * 100 * 0.6 = 82px

所以总结起来，每个item宽度的计算公式为

width + shrink * width / sum(shrink * width) * (containerWidth - sum(width)) * min(sum(shrink), 1)

## Unity中的Flexbox

### UI Toolkit

[UI Toolkit用户手册 (2020.3)](https://docs.unity3d.com/2020.3/Documentation/Manual/UIElements.html)

UI Toolkit编辑界面一览

![](https://user-images.githubusercontent.com/1536338/163669159-bbfca20b-8aa4-44dc-a751-c2480fbf2989.png)

默认所有UI元素都是以Flexbox来布局，可以轻而易举实现有最大宽度的可变宽文字和固定宽度按钮的整体居中功能。

![](https://user-images.githubusercontent.com/1536338/163669305-c12dae12-016f-400e-b0b7-3c2dc207215b.gif)

> UI Toolkit的Layout Engine基于[Yoga layout](https://yogalayout.com/)

### UGUI

默认不支持Flexbox布局，assetstore可以搜到一个收费package[Flexbox 4 Unity ($29.99)](https://assetstore.unity.com/packages/tools/gui/flexbox-4-unity-139571)，基本实现了完整的Flexbox布局功能。

#### 基于UGUI的Flexbox实现

源码详见`FlexContainer.cs`和`FlexItem.cs`，详细使用见`FlexboxSample.prefab`。

当前实现情况如下，已满足基本使用。

Container:
* flex-direction
    * [x] row
    * [ ] row-reverse
    * [x] column
    * [ ] column-reverse
* flex-wrap
    * [x] no-wrap
    * [ ] wrap
* justify-content
    * [x] flex-start
    * [x] flex-end
    * [x] center
    * [x] space-between
    * [x] space-around
    * [x] space-evenly
* align-items
    * [x] center
    * [x] flex-start
    * [x] flex-end
    * [x] stretch
* align-content
    * [x] normal
    * [ ] flex-start
    * [ ] flexend
    * [ ] center
    * [ ] space-between
    * [ ] space-around
    * [ ] space-evenly
    * [ ] stretch
* [x] gap

Item:
* [ ] order
* [x] flex-basis
* [x] flex-grow
* [x] flex-shrink
* [x] min-size
* [x] max-size
* [x] align-self

TODO list:
* 避免`DoLayout`无效的调用
* 修改`Container`或者`Item`跟`transform`相关的属性，自动重新计算布局
* 嵌套布局有任何改动，自动自顶向下重新计算布局
* 创建`FlexboxLayoutRebulder`进一步优化，减少计算
* 优化编辑器所见即所得编辑体验
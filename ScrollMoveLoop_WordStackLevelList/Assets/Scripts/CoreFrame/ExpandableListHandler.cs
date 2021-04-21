using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpandableListHandler<T>
{
    #region Classes

    private class Animation
    {
        private RectTransform target;
        private int index;
        private float timer;
        private float from;
        private float to;
    }

    #endregion

    #region Inspector Variables

    #endregion

    #region Member Variables

    //PackInfo
    private List<T> dataObjects;

    //packListItemPrefab
    private ExpandableListItem<T> listItemPrefab;

    //Content
    private RectTransform listContainer;
    private ScrollRect listScrollRect;
    private float expandAnimDuration;

    //列表对象池
    private ObjectPool listItemPool;

    //放置列表的父物体
    private List<RectTransform> listItemPlaceholders;
    private int topItemIndex;
    private int bottomItemIndex;
    private int expandedItemIndex;
    private float expandedHeight;

    #endregion

    #region Properties

    public System.Action<ExpandableListItem<T>> OnItemCreated { get; set; }
    public bool IsExpandingOrCollapsing { get; private set; }

    private Vector2 ListItemSize
    {
        get { return listItemPrefab.RectT.sizeDelta; }
    }

    #endregion

    #region Constructor

    public ExpandableListHandler(List<T> dataObjects, ExpandableListItem<T> listItemPrefab,
        RectTransform listContainer, ScrollRect listScrollRect, float expandAnimDuration)
    {
        this.dataObjects = dataObjects;
        this.listItemPrefab = listItemPrefab;
        this.listContainer = listContainer;
        this.listScrollRect = listScrollRect;
        this.expandAnimDuration = expandAnimDuration;

        listItemPool = new ObjectPool(listItemPrefab.gameObject, 0,
            ObjectPool.CreatePoolContainer(listContainer));
        listItemPlaceholders = new List<RectTransform>();
    }

    #endregion

    #region Public Methods

    public void Setup()
    {
        listScrollRect.onValueChanged.AddListener(OnListScrolled);

        CreateListItemPlaceholders();

        //强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContainer);

        Reset();
    }

    public void Reset()
    {
        listContainer.anchoredPosition = Vector2.zero;

        for (int i = 0; i < listItemPlaceholders.Count; i++)
        {
            RemoveListItem(listItemPlaceholders[i]);
        }

        //强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContainer);

        expandedItemIndex = -1;

        UpdateList(true);
    }

    public void ExpandListItem(int index, float extraHeight)
    {
        if (IsExpandingOrCollapsing)
        {
            return;
        }

        if (expandedItemIndex != -1)
        {
            CollapseListItem(expandedItemIndex, true);
        }

        IsExpandingOrCollapsing = true;

        RectTransform placeholder = listItemPlaceholders[index];
        float toHeight = ListItemSize.y + extraHeight;

        ScrollToMiddle(placeholder, toHeight, index);

        expandedItemIndex = index;
        expandedHeight = toHeight;

        UIAnimation animation = UIAnimation.Height(placeholder, expandedHeight, expandAnimDuration);

        listScrollRect.velocity = Vector2.zero;
        listScrollRect.enabled = false;

        animation.OnAnimationFinished += (GameObject obj) =>
        {
            listScrollRect.enabled = true;
            IsExpandingOrCollapsing = false;
        };

        PlayAnimation(animation);

        CoroutineStarter.Start(UpdateListWhileAnimating());

        SetExpanded(placeholder, true);
    }

    public void CollapseListItem(int index, bool isExpandingNewItem = false)
    {
        if (IsExpandingOrCollapsing || expandedItemIndex != index)
        {
            return;
        }

        IsExpandingOrCollapsing = true;

        RectTransform placeholder = listItemPlaceholders[index];

        UIAnimation animation = UIAnimation.Height(placeholder, ListItemSize.y, expandAnimDuration);

        animation.OnAnimationFinished += (GameObject obj) =>
        {
            if (placeholder.childCount == 1)
            {
                placeholder.GetChild(0).GetComponent<ExpandableListItem<T>>().Collapsed();
            }
        };

        if (!isExpandingNewItem)
        {
            // If we are not expanding a new list item then scroll this list item to the middle of the screen
            ScrollToMiddle(placeholder, ListItemSize.y, index);
            expandedItemIndex = -1;

            listScrollRect.velocity = Vector2.zero;
            listScrollRect.enabled = false;

            animation.OnAnimationFinished += (GameObject obj) =>
            {
                listScrollRect.enabled = true;
                IsExpandingOrCollapsing = false;
            };

            CoroutineStarter.Start(UpdateListWhileAnimating());
        }

        PlayAnimation(animation);

        SetExpanded(placeholder, false);
    }

    public void Refresh()
    {
        for (int i = topItemIndex; i <= bottomItemIndex; i++)
        {
            RectTransform placeholder = listItemPlaceholders[i];

            if (placeholder.childCount == 1)
            {
                ExpandableListItem<T> listItem =
                    placeholder.GetChild(0).GetComponent<ExpandableListItem<T>>();

                listItem.IsExpanded = i == expandedItemIndex;
                listItem.Setup(dataObjects[i], i == expandedItemIndex);
            }
        }
    }

    #endregion

    #region Private Methods

    private void OnListScrolled(Vector2 pos)
    {
        UpdateList();
    }

    private void CreateListItemPlaceholders()
    {
        for (int i = 0; i < dataObjects.Count; i++)
        {
            GameObject placeholder = new GameObject("list_item");
            RectTransform placholderRectT = placeholder.AddComponent<RectTransform>();

            placholderRectT.SetParent(listContainer, false);

            placholderRectT.sizeDelta = ListItemSize;

            listItemPlaceholders.Add(placholderRectT);
        }
    }

    /// <summary>
    /// 更新列表
    /// </summary>
    /// <param name="reset"></param>
    private void UpdateList(bool reset = false)
    {
        if (reset)
        {
            topItemIndex = 0;
            bottomItemIndex = FillList(topItemIndex, 1);
        }
        else
        {
            RecycleList();

            topItemIndex = FillList(topItemIndex, -1);
            bottomItemIndex = FillList(bottomItemIndex, 1);
        }
    }

    /// <summary>
    /// 如果是Scroll可视范围内，则开始填充列表
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="indexInc"></param>
    /// <returns></returns>
    private int FillList(int startIndex, int indexInc)
    {
        int lastVisibleIndex = startIndex;

        for (int i = startIndex; i >= 0 && i < listItemPlaceholders.Count; i += indexInc)
        {
            RectTransform placeholder = listItemPlaceholders[i];

            if (!IsVisible(i, placeholder))
            {
                break;
            }

            lastVisibleIndex = i;

            if (placeholder.childCount == 0)
            {
                //如果indexInc == -1 ,并且是可扩张状态，则进行列表扩张
                AddListItem(i, placeholder, indexInc == -1);
            }
        }

        return lastVisibleIndex;
    }

    /// <summary>
    /// 回收列表
    /// </summary>
    private void RecycleList()
    {
        for (int i = topItemIndex; i <= bottomItemIndex; i++)
        {
            RectTransform placeholder = listItemPlaceholders[i];

            if (IsVisible(i, placeholder))
            {
                break;
            }
            else if (placeholder.childCount == 1)
            {
                RemoveListItem(placeholder);

                topItemIndex++;
            }
        }

        for (int i = bottomItemIndex; i >= topItemIndex; i--)
        {
            RectTransform placeholder = listItemPlaceholders[i];

            if (IsVisible(i, placeholder))
            {
                break;
            }
            else if (placeholder.childCount == 1)
            {
                RemoveListItem(placeholder);

                bottomItemIndex--;
            }
        }

        // 检查顶部索引现在是否大于底部索引，如果是，则所有元素都被回收，因此我们需要找到新的顶部索引
        if (topItemIndex > bottomItemIndex)
        {
            int targetIndex = (topItemIndex < listItemPlaceholders.Count) ? topItemIndex : bottomItemIndex;
            RectTransform targetPlaceholder = listItemPlaceholders[targetIndex];
            float viewportTop = listContainer.anchoredPosition.y;

            if (-targetPlaceholder.anchoredPosition.y < viewportTop)
            {
                for (int i = targetIndex; i < listItemPlaceholders.Count; i++)
                {
                    if (IsVisible(i, listItemPlaceholders[i]))
                    {
                        topItemIndex = i;
                        bottomItemIndex = i;
                        break;
                    }
                }
            }
            else
            {
                for (int i = targetIndex; i >= 0; i--)
                {
                    if (IsVisible(i, listItemPlaceholders[i]))
                    {
                        topItemIndex = i;
                        bottomItemIndex = i;
                        break;
                    }
                }
            }
        }
    }

    private bool IsVisible(int index, RectTransform placeholder)
    {
        RectTransform viewport = listScrollRect.viewport as RectTransform;

        float placeholderTop = -placeholder.anchoredPosition.y - placeholder.rect.height / 2f;
        float placeholderbottom = -placeholder.anchoredPosition.y + placeholder.rect.height / 2f;

        float viewportTop = listContainer.anchoredPosition.y;
        float viewportbottom = listContainer.anchoredPosition.y + viewport.rect.height;

        return placeholderTop < viewportbottom && placeholderbottom > viewportTop;
    }

    private void AddListItem(int index, RectTransform placeholder, bool addingOnTop)
    {
        bool itemInstantiated;

        ExpandableListItem<T> listItem =
            listItemPool.GetObject<ExpandableListItem<T>>(placeholder, out itemInstantiated);

        SetPlaceholderSize(index, placeholder, addingOnTop);

        listItem.RectT.anchorMin = Vector2.zero;
        listItem.RectT.anchorMax = Vector2.one;
        listItem.RectT.offsetMin = Vector2.zero;
        listItem.RectT.offsetMax = Vector2.zero;

        listItem.Index = index;
        listItem.ExpandableListHandler = this;

        if (itemInstantiated)
        {
            if (OnItemCreated != null)
            {
                OnItemCreated(listItem);
            }

            listItem.Initialize(dataObjects[index]);
        }

        listItem.IsExpanded = (index == expandedItemIndex);
        listItem.Setup(dataObjects[index], index == expandedItemIndex);
    }

    private void SetPlaceholderSize(int index, RectTransform placeholder, bool addingOnTop)
    {
        //根据是否是伸张高度，获取到期望的高度
        float expectedHeight = (index == expandedItemIndex) ? expandedHeight : ListItemSize.y;

        if (addingOnTop)
        {
            float offset = expectedHeight - placeholder.rect.height;

            listContainer.anchoredPosition = new Vector2(listContainer.anchoredPosition.x,
                listContainer.anchoredPosition.y + offset);
        }

        placeholder.sizeDelta = new Vector2(placeholder.sizeDelta.x, expectedHeight);

        //将给定的 RectTransform 标记为需要在下一布局过程中重新计算其布局。
        LayoutRebuilder.MarkLayoutForRebuild(listContainer);
    }

    private void RemoveListItem(Transform placeholder)
    {
        if (placeholder.childCount == 1)
        {
            ExpandableListItem<T> listItem =
                placeholder.GetChild(0).GetComponent<ExpandableListItem<T>>();

            // Return the list item object to the pool
            ObjectPool.ReturnObjectToPool(listItem.gameObject);

            // Notify that it has been removed from the list
            listItem.Removed();
        }
    }

    /// <summary>
    /// 将给定的展开占位符滚动到视口的中间位置
    /// </summary>
    private void ScrollToMiddle(RectTransform placeholder, float height, int index)
    {
        float viewportMiddle = listContainer.anchoredPosition.y + listScrollRect.viewport.rect.height / 2f;
        float placeholderMiddle = -(placeholder.anchoredPosition.y + placeholder.rect.height / 2f) + height / 2f;

        if (expandedItemIndex != -1 && index > expandedItemIndex)
        {
            placeholderMiddle -= expandedHeight - ListItemSize.y;
        }

        float moveAmt = placeholderMiddle - viewportMiddle;

        // Make sure the list items top is not move passed to viewports top
        float viewportTop = listContainer.anchoredPosition.y;
        float placeholderTopAfterMove = placeholderMiddle - height / 2f - moveAmt;

        if (placeholderTopAfterMove < viewportTop)
        {
            moveAmt -= viewportTop - placeholderTopAfterMove;
        }

        // 确保移动量不会将容器上/下边缘移过视口边界
        if (moveAmt > 0)
        {
            float listHeight = (expandedItemIndex == -1)
                ? listContainer.rect.height + height - ListItemSize.y
                : listContainer.rect.height;
            float viewportBottom =
                listHeight - listContainer.anchoredPosition.y - listScrollRect.viewport.rect.height;

            if (moveAmt > viewportBottom)
            {
                moveAmt = viewportBottom;
            }
        }
        else if (moveAmt < 0 && Mathf.Abs(moveAmt) > listContainer.anchoredPosition.y)
        {
            moveAmt = -listContainer.anchoredPosition.y;
        }

        float toPos = listContainer.anchoredPosition.y + moveAmt;

        PlayAnimation(UIAnimation.PositionY(listContainer, toPos, expandAnimDuration));
    }

    private void PlayAnimation(UIAnimation anim)
    {
        anim.style = UIAnimation.Style.EaseOut;
        anim.startOnFirstFrame = true;
        anim.Play();
    }

    private IEnumerator UpdateListWhileAnimating()
    {
        while (IsExpandingOrCollapsing)
        {
            LayoutRebuilder.MarkLayoutForRebuild(listContainer);

            yield return new WaitForEndOfFrame();

            UpdateList();
        }
    }

    private void SetExpanded(RectTransform placeholder, bool isExpanded)
    {
        if (placeholder.childCount == 1)
        {
            placeholder.GetChild(0).GetComponent<ExpandableListItem<T>>().IsExpanded = isExpanded;
        }
    }

    #endregion
}
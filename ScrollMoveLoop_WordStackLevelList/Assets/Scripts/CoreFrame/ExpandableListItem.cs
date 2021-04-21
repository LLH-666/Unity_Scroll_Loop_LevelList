using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ExpandableListItem<T> : MonoBehaviour
{
    #region Properties

    public int Index { get; set; }
    public ExpandableListHandler<T> ExpandableListHandler { get; set; }

    public RectTransform RectT
    {
        get { return transform as RectTransform; }
    }

    public bool IsExpanded { get; set; }

    #endregion

    #region Abstract Methods

    public abstract void Initialize(T dataObject);
    public abstract void Setup(T dataObject, bool isExpanded);

    /// <summary>
    /// 折叠
    /// </summary>
    public abstract void Collapsed();

    public abstract void Removed();

    #endregion

    #region Protected Methods

    protected void Expand(float extraHeight)
    {
        ExpandableListHandler.ExpandListItem(Index, extraHeight);
    }

    protected void Collapse()
    {
        ExpandableListHandler.CollapseListItem(Index);
    }

    #endregion
}
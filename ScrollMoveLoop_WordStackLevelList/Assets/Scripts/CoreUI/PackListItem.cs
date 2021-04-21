using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PackListItem : ExpandableListItem<PackInfo>
{
    #region Inspector Variables

    [SerializeField] private Text packNameText = null;
    [SerializeField] private Text levelRangeText = null;
    [SerializeField] private GameObject lockedIcon = null;
    [SerializeField] private GameObject completedIcon = null;
    [SerializeField] private RectTransform levelListContent = null;

    #endregion

    #region Member Variables

    private PackInfo packInfo;
    private ObjectPool levelListItemPool;
    private List<GameObject> activeLevelListItems;

    #endregion

    #region Public Methods

    public override void Initialize(PackInfo dataObject)
    {
        activeLevelListItems = new List<GameObject>();
    }

    public override void Setup(PackInfo dataObject, bool isExpanded)
    {
        packInfo = dataObject;

        packNameText.text = packInfo.DisplayName.ToUpper();
        levelRangeText.text = string.Format("{0} - {1}", packInfo.FromLevelNumber, packInfo.ToLevelNumber);

        lockedIcon.SetActive(GameManager.Instance.IsLevelLocked(packInfo.FromLevelNumber));

        completedIcon.SetActive(packInfo.ToLevelNumber <= GameManager.Instance.LastCompletedLevel);

        if (isExpanded)
        {
            SetupLevelListItems();
        }
    }

    public override void Collapsed()
    {
        ReturnLevelListItemsToPool();
    }

    public override void Removed()
    {
        ReturnLevelListItemsToPool();
    }

    public void SetLevelListItemPool(ObjectPool levelListItemPool)
    {
        this.levelListItemPool = levelListItemPool;
    }

    public void OnItemClicked()
    {
        // Don't call Expand or collapse while the handler is expanding an item, the handler will just ignore those calls
        if (ExpandableListHandler.IsExpandingOrCollapsing)
        {
            return;
        }

        if (IsExpanded)
        {
            // If the item is expanded then collapse it
            Collapse();
        }
        else
        {
            // Set the level list items
            SetupLevelListItems();

            // Force the level list container to resize
            LayoutRebuilder.ForceRebuildLayoutImmediate(levelListContent);

            // Expand this pack item so all the levels appear
            Expand(levelListContent.rect.height);
        }
    }

    #endregion

    #region Private Methods

    private void SetupLevelListItems()
    {
        ReturnLevelListItemsToPool();

        for (int i = 0; i < packInfo.LevelDatas.Count; i++)
        {
            LevelData levelData = packInfo.LevelDatas[i];
            LevelListItem levelListItem = levelListItemPool.GetObject<LevelListItem>(levelListContent);

            levelListItem.Setup(levelData);

            levelListItem.OnLevelItemSelected = OnLevelItemSelected;

            activeLevelListItems.Add(levelListItem.gameObject);
        }
    }

    private void ReturnLevelListItemsToPool()
    {
        for (int i = 0; i < activeLevelListItems.Count; i++)
        {
            ObjectPool.ReturnObjectToPool(activeLevelListItems[i]);
        }

        activeLevelListItems.Clear();
    }

    private void OnLevelItemSelected(LevelData levelData)
    {
        //GameManager.Instance.TryShowInterstitialAd();

        GameManager.Instance.StartLevel(packInfo, levelData);
    }

    #endregion
}
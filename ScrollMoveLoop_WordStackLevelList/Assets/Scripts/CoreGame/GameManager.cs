using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using UnityEngine.Serialization;

public class GameManager : SingletonComponent<GameManager>
{
    [SerializeField] private ScrollRect packListScrollRect = null;
    [SerializeField] private RectTransform packListContainer = null;
    [SerializeField] private PackListItem packListItemPrefab = null;
    [SerializeField] private LevelListItem levelListItemPrefab = null;
    [SerializeField] private float expandAnimDuration = 0.5f;
    
    [Space]
    [SerializeField] private bool disableLevelLocking;

    private ExpandableListHandler<PackInfo> expandableListHandler;
    private ObjectPool levelListItemPool;
    private List<PackInfo> packInfos = new List<PackInfo>();

    public List<PackInfo> PackInfos
    {
        get { return packInfos; }
    }

    public int LastCompletedLevel { get; private set; }

    public bool Debug_DisableLevelLocking
    {
        get { return disableLevelLocking; }
    }

    private void Start()
    {
        LoadData();

        Initialize();
        
        Show();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Show();
        }
    }

    private void LoadData()
    {
        TextAsset json = Resources.Load<TextAsset>("Json/PackInfo");
        packInfos = JsonMapper.ToObject<List<PackInfo>>(json.text);
        
        LastCompletedLevel = 1;
    }

    private void Initialize()
    {
        levelListItemPool =
            new ObjectPool(levelListItemPrefab.gameObject, 1, ObjectPool.CreatePoolContainer(transform));
        expandableListHandler = new ExpandableListHandler<PackInfo>(PackInfos,
            packListItemPrefab, packListContainer, packListScrollRect, expandAnimDuration);

        // Add a listener for when a PackListItem is first created to pass it the level list item pool
        expandableListHandler.OnItemCreated += (ExpandableListItem<PackInfo> packListItem) =>
        {
            (packListItem as PackListItem)?.SetLevelListItemPool(levelListItemPool);
        };

        expandableListHandler.Setup();
    }

    private void Show(bool back = false)
    {
        if (!back)
        {
            levelListItemPool.ReturnAllObjectsToPool();

            expandableListHandler.Reset();
        }
        else
        {
            expandableListHandler.Refresh();
        }
    }

    /// <summary>
    /// Returns true if the given level number is locked
    /// </summary>
    public bool IsLevelLocked(int levelNumber)
    {
        if (Debug_DisableLevelLocking)
        {
            return false;
        }

        return levelNumber > LastCompletedLevel + 1;
    }

    public void StartLevel(PackInfo packInfo, LevelData levelData)
    {
        Debug.Log($"click packInfo:{packInfo.DisplayName},levelData:{levelData.Lv}");
    }
}
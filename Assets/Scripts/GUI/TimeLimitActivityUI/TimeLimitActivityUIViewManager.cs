using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mogo.Util;
using Mogo.GameData;

public enum ActivityStatus
{
    HasReward = 1, // 已领取奖励
    HasFinished = 2, // 已完成
    TimeUseUp = 3, // 任务超时
    OtherStatus = 4, // 其他
}
public class LimitActivityGridData
{
    public string CDText;
    public string TitleText;
    public string FGImgName;
    public ActivityStatus Status = ActivityStatus.OtherStatus;
}

public class LimitActivityInfoGridData
{
    public int ID;
    public string Title;
    public string Desc;
    public string Rule;
    public string CDText;
    public string InfoImgName;
    public List<int> ItemListID = new List<int>();
    public ActivityStatus Status = ActivityStatus.OtherStatus;
}

public class TimeLimitActivityUIViewManager : MogoUIBehaviour 
{
    private static TimeLimitActivityUIViewManager m_instance;
    public static TimeLimitActivityUIViewManager Instance { get { return TimeLimitActivityUIViewManager.m_instance; } }

    private static int INSTANCE_COUNT = 0; // 异步加载资源数量

    public static Dictionary<string, Action<int>> ButtonTypeToEventUp = new Dictionary<string, Action<int>>();
    const int ACTIVITYGRIDWIDTH = 380;

    // 限时活动列表
    private GameObject m_goTimeLimitActivityUIActivityListUI;
    private Camera m_camActivityGridList;
    private MyDragableCamera m_dragableCameraActivityGridList;
    private GameObject m_goActivityGridList;   
    private GameObject m_goGOTimeLimitActivityUIActivityListPage;

    private List<GameObject> m_listActivityGirdObject = new List<GameObject>();
    private List<TimeLimitActivityGrid> m_listActivityGird = new List<TimeLimitActivityGrid>();

    // 限时活动详细列表
    private GameObject m_goTimeLimitActivityUIActivityInfoListUI;
    private Transform m_tranTimeLimitActivityInfoGridList;
    private MogoListImproved m_infoGridListMogoListImproved;

    // 限时活动列表特效
    private Camera m_timeLimitActivityGridListCameraFX;
    private GameObject m_goTimeLimitActivityGridListParticleAnimRoot;

    // 限时活动详细列表特效
    private GameObject m_goTimeLimitActivityInfoGridListFx;

    void Awake()
    {
        m_instance = this;
        m_myTransform = transform;
        FillFullNameData(m_myTransform);

        Camera camera = GameObject.Find("MogoMainUI").transform.FindChild("Camera").GetComponentsInChildren<Camera>(true)[0];

        m_camActivityGridList = FindTransform("TimeLimitActivityGridListCamera").GetComponentsInChildren<Camera>(true)[0];
        m_camActivityGridList.GetComponentsInChildren<UIViewport>(true)[0].sourceCamera = camera;
        m_dragableCameraActivityGridList = m_camActivityGridList.GetComponentsInChildren<MyDragableCamera>(true)[0];
        m_dragableCameraActivityGridList.LeftArrow = FindTransform("TimeLimitActivityUIActivityListArrowL").gameObject;
        m_dragableCameraActivityGridList.RightArrow = FindTransform("TimeLimitActivityUIActivityListArrowR").gameObject;
        m_goActivityGridList = FindTransform("TimeLimitActivityGridList").gameObject;
        m_goTimeLimitActivityUIActivityListUI = FindTransform("TimeLimitActivityUIActivityList").gameObject;
        m_goGOTimeLimitActivityUIActivityListPage = FindTransform("GOTimeLimitActivityUIActivityListPage").gameObject;

        m_goTimeLimitActivityUIActivityInfoListUI = FindTransform("TimeLimitActivityUIActivityInfoList").gameObject;
        m_tranTimeLimitActivityInfoGridList = FindTransform("TimeLimitActivityInfoGridList");
        m_infoGridListMogoListImproved = m_tranTimeLimitActivityInfoGridList.GetComponentsInChildren<MogoListImproved>(true)[0];
        m_infoGridListMogoListImproved.LeftArrow = FindTransform("TimeLimitActivityUIActivityInfoArrowL").gameObject;
        m_infoGridListMogoListImproved.RightArrow = FindTransform("TimeLimitActivityUIActivityInfoArrowR").gameObject;

        m_timeLimitActivityGridListCameraFX = FindTransform("TimeLimitActivityGridListCameraFX").GetComponentsInChildren<Camera>(true)[0];
        m_timeLimitActivityGridListCameraFX.GetComponentsInChildren<UIViewport>(true)[0].sourceCamera = camera;
        m_goTimeLimitActivityGridListParticleAnimRoot = FindTransform("TimeLimitActivityGridListParticleAnimRoot").gameObject;

        m_goTimeLimitActivityInfoGridListFx = FindTransform("TimeLimitActivityInfoGridListFx").gameObject;

        Initialize();

        EventDispatcher.TriggerEvent(Events.OperationEvent.GetActivityMessage);
    }   
  
    #region 事件

    void Initialize()
    {
        TimeLimitActivityUILogicManager.Instance.Initialize();

        EventDispatcher.AddEventListener<int>(TimeLimitActivityUILogicManager.TimeLimitActivityUIEvent.ActivityGridUp, OnActivityGridUp);
        FindTransform("TimeLimitActivityUIActivityInfoListBtnBack").GetComponentsInChildren<MogoButton>(true)[0].clickHandler += OnActivityInfoBackBtn;    
    }

    public void Release()
    {
        ButtonTypeToEventUp.Clear();

        EventDispatcher.RemoveEventListener<int>(TimeLimitActivityUILogicManager.TimeLimitActivityUIEvent.ActivityGridUp, OnActivityGridUp);
        FindTransform("TimeLimitActivityUIActivityInfoListBtnBack").GetComponentsInChildren<MogoButton>(true)[0].clickHandler -= OnActivityInfoBackBtn;        

        TimeLimitActivityUILogicManager.Instance.Release();

        for (int i = 0; i < m_listActivityGirdObject.Count; ++i)
        {
            AssetCacheMgr.ReleaseInstance(m_listActivityGirdObject[i]);
            m_listActivityGirdObject[i] = null;
        }
        m_listActivityGirdObject.Clear();    
        m_listActivityGird.Clear();     
    }

    /// <summary>
    /// 点击限时活动
    /// </summary>
    /// <param name="id"></param>
    void OnActivityGridUp(int id)
    {
        Mogo.Util.LoggerHelper.Debug(id);        
        SwitchActivityInfoUI(id);        
    }     

    #endregion

    #region 限时活动和限时活动详细信息列表   

    #region 限时活动列表

    readonly static private int ActivityGridCountOnePage = 3;// 每页显示活动数量
    private int m_iActivityGridPageNum = 0;

    public void ResetActivityGridList()
    {
        EmptyActivityGridList();
        EmptyActivityGridPageList();
    }

    // 清空Grid
    void EmptyActivityGridList()
    {
        for (int i = 0; i < m_listActivityGirdObject.Count; ++i)
        {
            int index = i;
            AssetCacheMgr.ReleaseInstance(m_listActivityGirdObject[index]);
        }

        m_listActivityGird.Clear();
        m_listActivityGirdObject.Clear();
    }

    /// <summary>
    /// 清空页数信息
    /// </summary>
    void EmptyActivityGridPageList()
    {
        m_dragableCameraActivityGridList.DestroyMovePagePosList(); // 删除翻页位置
        m_dragableCameraActivityGridList.DestroyDOTPageList(); // 删除页点   
        m_iActivityGridPageNum = 0;
    }    

    /// <summary>
    /// 创建Grid
    /// </summary>
    /// <param name="gridData"></param>
    /// <param name="theID"></param>
    public void AddActivityGrid(LimitActivityGridData gridData, int theID)
    {
        INSTANCE_COUNT++;
        MogoGlobleUIManager.Instance.ShowWaitingTip(true);

        AssetCacheMgr.GetUIInstance("TimeLimitActivityGrid.prefab", (prefab, id, go) =>
        {
            for (int i = 0; i < m_listActivityGird.Count; ++i)
            {
                if (m_listActivityGird[i].Id == theID)
                {
                    AssetCacheMgr.ReleaseInstance((GameObject)go);
                    return;
                }
            }
            GameObject obj = (GameObject)go;
            TimeLimitActivityGrid gridUI = obj.AddComponent<TimeLimitActivityGrid>();

            obj.name = "TimeLimitActivityGrid" + m_listActivityGird.Count;

            gridUI.LoadResourceInsteadOfAwake();
            gridUI.Id = theID;
            gridUI.Index = m_listActivityGird.Count;
            gridUI.CDText = gridData.CDText;
            gridUI.TitleText = gridData.TitleText;
            gridUI.GridFGName = gridData.FGImgName;
            gridUI.SetActivityStatus(gridData.Status);               

            obj.transform.parent = m_goActivityGridList.transform;
            obj.transform.localPosition = new Vector3(m_listActivityGirdObject.Count * ACTIVITYGRIDWIDTH, 0, -1);
            obj.transform.localScale = new Vector3(1, 1, 1);

            m_listActivityGirdObject.Add(obj);  
            m_listActivityGird.Add(gridUI);

            obj.GetComponentsInChildren<MyDragCamera>(true)[0].RelatedCamera = m_camActivityGridList.transform.GetComponentsInChildren<Camera>(true)[0];

            INSTANCE_COUNT--;
            if (INSTANCE_COUNT <= 0)
                MogoGlobleUIManager.Instance.ShowWaitingTip(false);

            if (m_listActivityGirdObject.Count - ActivityGridCountOnePage >= 0)
                m_dragableCameraActivityGridList.MAXX = 380 + ACTIVITYGRIDWIDTH * (m_listActivityGirdObject.Count - 3);
            else
                m_dragableCameraActivityGridList.MAXX = 380;

            EventDispatcher.TriggerEvent<int>("LoadTimeLimitActivityGridDone", gridUI.Id);

            if (obj.name == MogoUIManager.Instance.WaitingWidgetName)
            {
                EventDispatcher.TriggerEvent("WaitingWidgetFinished");
            }

            // 创建翻页位置
            int index = m_listActivityGirdObject.Count - 1;
            if (index % ActivityGridCountOnePage == 0)
            {
                GameObject trans = new GameObject();
                trans.transform.parent = m_camActivityGridList.transform;
                trans.transform.localPosition = new Vector3(index / ActivityGridCountOnePage * 1140 + 380, 0, 0);
                trans.transform.localEulerAngles = Vector3.zero;
                trans.transform.localScale = new Vector3(1, 1, 1);
                trans.name = "ActivityGridPagePosHorizon" + index / ActivityGridCountOnePage;
                m_dragableCameraActivityGridList.transformList.Add(trans.transform);
                m_dragableCameraActivityGridList.SetCurrentPage(0);

                // 创建页数点
                ++m_iActivityGridPageNum;
                int num = m_iActivityGridPageNum;
                AssetCacheMgr.GetUIInstance("ChooseServerPage.prefab", (prefabPage, idPage, goPage) =>
                {
                    GameObject objPage = (GameObject)goPage;

                    objPage.transform.parent = m_goGOTimeLimitActivityUIActivityListPage.transform;
                    objPage.transform.localPosition = new Vector3((num - 1) * 40, 0, 0);
                    objPage.transform.localScale = Vector3.one;
                    objPage.name = "ActivityGridPage" + num;
                    m_camActivityGridList.GetComponentsInChildren<MyDragableCamera>(true)[0].ListPageDown.Add(objPage.GetComponentsInChildren<UISprite>(true)[1].gameObject);
                    m_goGOTimeLimitActivityUIActivityListPage.transform.localPosition = new Vector3(-20 * (num - 1), m_goGOTimeLimitActivityUIActivityListPage.transform.localPosition.y, 0);

                    // 选择当前页
                    if (num - 1 == m_dragableCameraActivityGridList.GetCurrentPage())
                        objPage.GetComponentsInChildren<UISprite>(true)[1].gameObject.SetActive(true);
                    else
                        objPage.GetComponentsInChildren<UISprite>(true)[1].gameObject.SetActive(false);
                    m_dragableCameraActivityGridList.GODOTPageList = m_goGOTimeLimitActivityUIActivityListPage;
                    m_dragableCameraActivityGridList.SetCurrentPage(m_dragableCameraActivityGridList.GetCurrentPage());
                });
            }         
        });
    }   
 

    /// <summary>
    /// 显示活动Grid停止CD
    /// </summary>
    /// <param name="id"></param>
    public void SetGridCountDownStop(int id)
    {
        foreach (var grid in m_listActivityGird)
        {
            if (grid.Id == id)
            {
                grid.SetCountDownStop();
                break;
            }
        }
    } 

    #endregion

    #region 限时活动详细列表

    List<LimitActivityInfoGridData> m_listLimitActivityInfoData;

    /// <summary>
    /// 设置限时活动详细列表Data
    /// </summary>
    /// <param name="listActivityInfoData"></param>
    public void SetLimitActivityInfoListData(List<LimitActivityInfoGridData> listActivityInfoData)
    {
        m_listLimitActivityInfoData = listActivityInfoData;        
    }

    /// <summary>
    /// 创建限时活动详细Grid
    /// </summary>
    /// <param name="data"></param>
    private void GenerateLimitActivityInfoList(Action action = null)
    {
        m_infoGridListMogoListImproved.SetGridLayout<TimeLimitActivityInfo>(m_listLimitActivityInfoData.Count, m_tranTimeLimitActivityInfoGridList.transform, () =>
            {
                LimitActivityInfoListResourceLoaded();

                if (action != null)
                    action();
            });
    }

    /// <summary>
    /// 设置限时活动详细列表界面Data
    /// </summary>
    private void LimitActivityInfoListResourceLoaded()
    {
        var m_dataList = m_infoGridListMogoListImproved.DataList;

        for (int i = 0; i < m_dataList.Count; i++)
        {
            TimeLimitActivityInfo gridUI = (TimeLimitActivityInfo)m_dataList[i];
            LimitActivityInfoGridData gridData = m_listLimitActivityInfoData[i];

            gridUI.LoadResourceInsteadOfAwake();
            gridUI.ActivityID = gridData.ID;
            gridUI.CDText = gridData.CDText;
            gridUI.InfoDesc = gridData.Desc;
            gridUI.InfoTitle = gridData.Title;
            gridUI.Rule = gridData.Rule;
            gridUI.InfoImgName = gridData.InfoImgName;
            gridUI.ListItemID = gridData.ItemListID;
            gridUI.SetActivityStatus(gridData.Status);
        }     
    }

    /// <summary>
    /// 限时活动详细界面显示或隐藏按钮
    /// 只有在进入详细界面时才根据数据列表刷新，而在界面中只根据界面刷新，防止跳位
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isShow"></param>
    public void SetLimitActivityInfoHasReward(int id)
    {
        var m_gridUIList = m_infoGridListMogoListImproved.DataList;
        for (int i = 0; i < m_gridUIList.Count; i++)
        {
            TimeLimitActivityInfo gridUI = (TimeLimitActivityInfo)m_gridUIList[i];
            if (gridUI.ActivityID == id)
            {
                gridUI.CDText = LanguageData.GetContent(7134);
                gridUI.SetActivityStatus(ActivityStatus.HasReward);
                return;
            }
        }         
    }

    /// <summary>
    /// 限时活动详细界面返回
    /// </summary>
    private void OnActivityInfoBackBtn()
    {
        Mogo.Util.LoggerHelper.Debug("BackBtn");
        SwitchActivityUI();
    }

    /// <summary>
    /// 设置限时活动详细列表当前页
    /// </summary>
    /// <param name="id"></param>
    private void SetLimitActivityInfoListCurrentPageByID(int id)
    {
        for (int i = 0; i < m_listLimitActivityInfoData.Count; i++)
        {
            if (m_listLimitActivityInfoData[i].ID == id)
            {
                if(i <= m_infoGridListMogoListImproved.MaxPageIndex)
                    m_infoGridListMogoListImproved.TweenTo(i, true);

                return;
            }
        }
    }    

    #endregion

    // true=限时活动列表界面
    // false = 限时活动详细信息列表界面
    public bool IsCurrentInActivityUI = true;

    /// <summary>
    /// 切换到限时活动列表界面
    /// </summary>
    public void SwitchActivityUI()
    {
        IsCurrentInActivityUI = true;

        ShowTimeLimitActivityUIActivityListUI(true);
        ShowTimeLimitActivityUIActivityInfoListUI(false);
    }

    /// <summary>
    /// 切换到限时活动详细信息列表界面
    /// </summary>
    public void SwitchActivityInfoUI(int id)
    {
        IsCurrentInActivityUI = false;

        // 在切换为详细信息界面时创建，在该界面操作不刷新，防止跳位
        GenerateLimitActivityInfoList(() =>
            {
                SetLimitActivityInfoListCurrentPageByID(id);
            });

        ShowTimeLimitActivityUIActivityInfoListUI(true);
        ShowTimeLimitActivityUIActivityListUI(false);
    }

    private void ShowTimeLimitActivityUIActivityListUI(bool isShow)
    {
        m_goTimeLimitActivityUIActivityListUI.SetActive(isShow);
    }

    private void ShowTimeLimitActivityUIActivityInfoListUI(bool isShow)
    {
        m_goTimeLimitActivityUIActivityInfoListUI.SetActive(isShow);

        if(!isShow)
            ReleaseAllTimeLimitActivityInfoUIFx();
    }

    #endregion    

    #region 限时活动列表完成特效

    Dictionary<int, GameObject> m_listFXinActivityUI = new Dictionary<int, GameObject>();

    /// <summary>
    /// 限时活动界面完成特效
    /// </summary>
    public void AttachFXToTimeLimitActivityUI(string animName, int id, int index = 0)
    {
        AttachParticleAnim(animName, m_goTimeLimitActivityGridListParticleAnimRoot, id, index * ACTIVITYGRIDWIDTH, 5, -120, 0);
    }

    private void AttachParticleAnim(string animName, GameObject animRoot, int activityID, float gridOffset, float xOffset = 0, float yOffset = 0, float zOffset = 0)
    {
        if (m_listFXinActivityUI.ContainsKey(activityID))
        {
            GameObject obj = m_listFXinActivityUI[activityID];
            obj.transform.parent = animRoot.transform;
            obj.transform.localPosition = new Vector3(gridOffset + xOffset, yOffset, zOffset);
            obj.transform.localScale = Vector3.one;
        }
        else
        {
            INSTANCE_COUNT++;
            MogoGlobleUIManager.Instance.ShowWaitingTip(true);

            AssetCacheMgr.GetUIInstance(animName, (prefab, id, go) =>
            {
                GameObject obj = (GameObject)go;
                obj.transform.parent = animRoot.transform;
                obj.transform.localPosition = new Vector3(gridOffset + xOffset, yOffset, zOffset);
                obj.transform.localScale = Vector3.one;

                m_listFXinActivityUI[activityID] = obj;

                INSTANCE_COUNT--;
                if (INSTANCE_COUNT <= 0)
                    MogoGlobleUIManager.Instance.ShowWaitingTip(false);
            });
        }        
    }

    public void ReleaseFXFromActivityUI(int id)
    {
        if (m_listFXinActivityUI.ContainsKey(id))
        {
            GameObject fx = m_listFXinActivityUI[id];
            m_listFXinActivityUI.Remove(id);
            AssetCacheMgr.ReleaseInstance(fx);
        }
    }

    /// <summary>
    /// 释放所有限时活动界面完成特效
    /// </summary>
    public void ReleaseAllTimeLimitActivityUIFx()
    {
        foreach (GameObject goFx in m_listFXinActivityUI.Values)
        {
            AssetCacheMgr.ReleaseInstance(goFx);
        }

        m_listFXinActivityUI.Clear();        
    }

    #endregion

    #region 限时活动详细列表完成特效

    private Camera m_gridTimeLimitActivityListCamera = null;
    private Camera m_gridTimeLimitActivityListCameraFX = null;

    public void AttachFXToTimeLimitActivityInfoUI(Transform tranFxPos, Action<GameObject> action)
    {
        if(m_infoGridListMogoListImproved.SourceCamera == null)
            m_infoGridListMogoListImproved.SourceCamera = GameObject.Find("Camera").GetComponentsInChildren<Camera>(true)[0];

        if (m_infoGridListMogoListImproved.ObjCamera == null)
        {
            LoggerHelper.Error("m_infoGridListMogoListImproved.ObjCamera is null");
            return;
        }

        INSTANCE_COUNT++;
        MogoGlobleUIManager.Instance.ShowWaitingTip(true);

        if (m_gridTimeLimitActivityListCamera == null)
        {
            m_gridTimeLimitActivityListCamera = m_infoGridListMogoListImproved.ObjCamera.GetComponentsInChildren<Camera>(true)[0];
        }

        if (m_gridTimeLimitActivityListCameraFX == null)
        {
            GameObject goTimeLimitActivityInfoCameraFx = new GameObject();
            goTimeLimitActivityInfoCameraFx.name = "TimeLimitActivityInfoCameraFx";
            goTimeLimitActivityInfoCameraFx.transform.parent = m_infoGridListMogoListImproved.ObjCamera.transform;
            goTimeLimitActivityInfoCameraFx.transform.localPosition = new Vector3(0, 0, 0);
            goTimeLimitActivityInfoCameraFx.transform.localScale = new Vector3(1, 1, 1);

            // 设置特效摄像机
            m_gridTimeLimitActivityListCameraFX = goTimeLimitActivityInfoCameraFx.AddComponent<Camera>();
            m_gridTimeLimitActivityListCameraFX.clearFlags = CameraClearFlags.Depth;
            m_gridTimeLimitActivityListCameraFX.cullingMask = 1 << 0;
            m_gridTimeLimitActivityListCameraFX.orthographic = true;
            m_gridTimeLimitActivityListCameraFX.orthographicSize = m_gridTimeLimitActivityListCamera.orthographicSize;
            m_gridTimeLimitActivityListCameraFX.nearClipPlane = -50;
            m_gridTimeLimitActivityListCameraFX.farClipPlane = 50;
            m_gridTimeLimitActivityListCameraFX.depth = 30;
            m_gridTimeLimitActivityListCameraFX.rect = m_gridTimeLimitActivityListCamera.rect;
        }

        Vector3 pos = m_gridTimeLimitActivityListCamera.WorldToScreenPoint(tranFxPos.position);
        pos = m_gridTimeLimitActivityListCameraFX.ScreenToWorldPoint(pos);        

        AssetCacheMgr.GetUIInstance("fx_ui_skill_yes.prefab", (prefab, id, go) =>
        {
            GameObject goFx = null;
            goFx = (GameObject)go;
            goFx.name = "TimeLimitActivityInfoFinishedFx";
            goFx.transform.parent = m_goTimeLimitActivityInfoGridListFx.transform;
            goFx.transform.position = pos;
            goFx.transform.localPosition += new Vector3(3, -120, 0);
            goFx.transform.localScale = new Vector3(1f, 1f, 1f);

            INSTANCE_COUNT--;
            if (INSTANCE_COUNT <= 0)
                MogoGlobleUIManager.Instance.ShowWaitingTip(false);

            if (action != null)
                action(goFx);
        });
    }

    /// <summary>
    /// 释放所有的限时活动详细列表完成特效
    /// </summary>
    private void ReleaseAllTimeLimitActivityInfoUIFx()
    {
        var m_dataList = m_infoGridListMogoListImproved.DataList;

        for (int i = 0; i < m_dataList.Count; i++)
        {
            TimeLimitActivityInfo gridUI = (TimeLimitActivityInfo)m_dataList[i];
            gridUI.ReleaseFXFromTimeLimitActivityInfoUI();
        }
    }

    #endregion

    #region 界面打开和关闭

    void ReleasePreLoadResrouces()
    {
        AssetCacheMgr.ReleaseResourceImmediate("TimeLimitActivityGrid.prefab");
    }

    public void DestroyUIAndResources()
    {
        if (SystemSwitch.DestroyAllUI)
        {
            ReleasePreLoadResrouces();
            MogoUIManager.Instance.DestroyTimeLimitActivityUI();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_goTimeLimitActivityUIActivityListUI.SetActive(true);
        m_goTimeLimitActivityUIActivityInfoListUI.SetActive(false);
    }

    void OnDisable()
    {
        ReleaseAllTimeLimitActivityUIFx();
        ReleaseAllTimeLimitActivityInfoUIFx();

        if (SystemSwitch.DestroyAllUI)
        {
            DestroyUIAndResources();
        }
    }

    #endregion
}

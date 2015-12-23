using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mogo.Util;

public class ChargeRewardUIViewManager : MogoUIBehaviour
{
    private static ChargeRewardUIViewManager m_instance;
    public static ChargeRewardUIViewManager Instance { get { return ChargeRewardUIViewManager.m_instance; } }
 
    public static Dictionary<string, Action<int>> ButtonTypeToEventUp = new Dictionary<string, Action<int>>();

    private UITexture m_texGril;
    private UITexture m_texBG;

    #region 充值

    private GameObject m_chargeBtn;
    private GameObject m_goChargeRewardUIBtnVIP0;
    private GameObject m_getRewardBtn;

    #endregion

    #region 首次充值

    protected GameObject m_firstChargeRewardMessage;

    #endregion

    #region 非首次充值

    protected GameObject m_chargeRewardMessage;
    protected Transform m_chargeRewardMessageProgressMark;
    protected Vector3 m_chargeRewardMessageProgressMarkPosition;
    protected ChargeRewardUIGameObjectGroup m_btnGroup;

    #endregion

    #region 充值奖励格

    List<GameObject> m_chargeRewardList = new List<GameObject>();
    List<InventoryGrid> m_chargeRewardGrid = new List<InventoryGrid>();
    List<UISprite> m_chargeRewardListIcon = new List<UISprite>();
    List<UISprite> m_chargeRewardListIconBG = new List<UISprite>();

    #endregion

    void Awake()
    {
        m_myTransform = transform;

        FillFullNameData(m_myTransform);

        m_instance = m_myTransform.GetComponentsInChildren<ChargeRewardUIViewManager>(true)[0];

        m_texGril = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardUIGirl"]).GetComponentsInChildren<UITexture>(true)[0];
        m_texBG = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardUIBG"]).GetComponentsInChildren<UITexture>(true)[0];

        #region 初始化充值

        m_chargeBtn = m_myTransform.FindChild(m_widgetToFullName["ChargeBtn"]).gameObject;
        m_goChargeRewardUIBtnVIP0 = FindTransform("ChargeRewardUIBtnVIP0").gameObject;
        m_getRewardBtn = m_myTransform.FindChild(m_widgetToFullName["GetRewardBtn"]).gameObject;

        #endregion

        #region 初始化首次充值

        m_firstChargeRewardMessage = m_myTransform.FindChild(m_widgetToFullName["FirstChargeRewardMessage"]).gameObject;

        #endregion

        #region 初始化非首次充值

        m_chargeRewardMessage = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardMessage"]).gameObject;
        m_chargeRewardMessageProgressMark = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardMessageProgressBottomMark"]);
        m_chargeRewardMessageProgressMarkPosition = new Vector3(m_chargeRewardMessageProgressMark.localPosition.x, m_chargeRewardMessageProgressMark.localPosition.y, m_chargeRewardMessageProgressMark.localPosition.z);
        //m_btnGroup = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardMessageProgressBtnGroup"]).GetComponent<ChargeRewardUIGameObjectGroup>();

        m_btnGroup = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardMessageProgressBtnGroup"]).gameObject.AddComponent<ChargeRewardUIGameObjectGroup>();

        // InitBtnGroup();

        #endregion

        #region 充值奖励格

        if (m_chargeRewardList == null)
            m_chargeRewardList = new List<GameObject>();
        if (m_chargeRewardListIcon == null)
            m_chargeRewardListIcon = new List<UISprite>();
        if (m_chargeRewardListIconBG == null)
            m_chargeRewardListIconBG = new List<UISprite>();
        for (int i = 0; i < 5; i++)
        {
            GameObject temp = m_myTransform.FindChild(m_widgetToFullName["ChargeReward" + i]).gameObject;
            m_chargeRewardList.Add(temp);

            InventoryGrid tempGrid = temp.GetComponent<InventoryGrid>();
            if (tempGrid != null)
                m_chargeRewardGrid.Add(tempGrid);

            UISprite tempSpIcon = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardIcon" + i]).gameObject.GetComponent<UISprite>();
            m_chargeRewardListIcon.Add(tempSpIcon);

            UISprite tempSpBG = m_myTransform.FindChild(m_widgetToFullName["ChargeRewardGrid" + i]).gameObject.GetComponent<UISprite>();
            m_chargeRewardListIconBG.Add(tempSpBG);
        }

        #endregion

        Initialize();

        EventDispatcher.TriggerEvent(Events.OperationEvent.GetChargeRewardMessage);
    }

    #region 事件

    void Initialize()
    {
        ChargeRewardUILogicManager.Instance.Initialize();

        #region 按钮

        FindTransform("ChargeBtn").GetComponentsInChildren<MogoButton>(true)[0].clickHandler += OnChargeUp;
        FindTransform("ChargeRewardUIBtnVIP0").GetComponentsInChildren<MogoButton>(true)[0].clickHandler += OnBtnVIPUp;


        if (!ButtonTypeToEventUp.ContainsKey("GetRewardBtn"))
            ButtonTypeToEventUp.Add("GetRewardBtn", OnGewRewardUp);

        #endregion
    }

    public void Release()
    {
        ButtonTypeToEventUp.Clear();

        ChargeRewardUILogicManager.Instance.Release();
    }

    #region 按钮事件函数

    public Action CHARGEBUTTONUP;
    public Action BTNVIPUP;
    public Action GETREWARDBUTTONUP;

    /// <summary>
    /// 充值按钮
    /// </summary>
    void OnChargeUp()
    {
        if (CHARGEBUTTONUP != null)
            CHARGEBUTTONUP();
    }

    /// <summary>
    /// VIP特权按钮
    /// </summary>
    void OnBtnVIPUp()
    {
        if (BTNVIPUP != null)
            BTNVIPUP();
    }

    /// <summary>
    /// 领取奖励按钮
    /// </summary>
    /// <param name="i"></param>
    void OnGewRewardUp(int i)
    {
        if (GETREWARDBUTTONUP != null)
            GETREWARDBUTTONUP();
    }

    #endregion

    #endregion

    #region 初始化Button组

    public void InitBtnGroup()
    {
        m_btnGroup.InitData();
    }

    #endregion

    #region 显示/不显示

    public void ShowFirstChargeRewardMessage()
    {
        m_firstChargeRewardMessage.SetActive(true);
        m_chargeRewardMessage.SetActive(false);
    }

    public void ShowChargeRewardMessage()
    {
        m_firstChargeRewardMessage.SetActive(false);
        m_chargeRewardMessage.SetActive(true);
    }

    public void SetChargeRewardListEnable(int num)
    {
        for (int i = m_chargeRewardList.Count - 1; i >= 0; i--)
        {
            if (i >= num)
            {
                m_chargeRewardList[i].SetActive(false);
            }
            else
                m_chargeRewardList[i].SetActive(true);
        }
    }

    public void SetBtnGroupPress(int i)
    {
        m_btnGroup.SetItemPress(i);
    }

    #endregion

    #region 修改数据

    public void SetChargeRewardListGrid(int i, int id)
    {
        m_chargeRewardGrid[i].iconID = id;
    }

    public void SetChargeRewardListIcon(int i, int id, int num)
    {
        InventoryManager.SetIcon(id, m_chargeRewardListIcon[i]);
    }

    public void SetChargeRewardListGrid(int[] ids)
    {
        int count = ids.Length > m_chargeRewardGrid.Count ? m_chargeRewardGrid.Count : ids.Length;
        for (int i = 0; i < count; i++)
        {
            m_chargeRewardGrid[i].iconID = ids[i];
        }
    }

    public void SetChargeRewardListIcon(int[] ids)
    {
        int count = ids.Length > m_chargeRewardListIcon.Count ? m_chargeRewardListIcon.Count : ids.Length;
        for (int i = 0; i < count; i++)
        {
            InventoryManager.SetIcon(ids[i], m_chargeRewardListIcon[i]);
        }
    }

    public void SetChargeRewardListGrid(List<int> ids)
    {
        int count = ids.Count > m_chargeRewardGrid.Count ? m_chargeRewardGrid.Count : ids.Count;
        for (int i = 0; i < count; i++)
        {
            m_chargeRewardGrid[i].iconID = ids[i];
        }
    }

    public void SetChargeRewardListIcon(List<int> ids)
    {
        int count = ids.Count > m_chargeRewardListIcon.Count ? m_chargeRewardListIcon.Count : ids.Count;
        for (int i = 0; i < count; i++)
        {
            InventoryManager.SetIcon(ids[i], m_chargeRewardListIcon[i]);
        }
    }

    public void SetChargeRewardListIcon(List<KeyValuePair<int, int>> idMessages)
    {
        int count = idMessages.Count > m_chargeRewardListIcon.Count ? m_chargeRewardListIcon.Count : idMessages.Count;
        for (int i = 0; i < count; i++)
        {
            InventoryManager.SetIcon(idMessages[i].Key, m_chargeRewardListIcon[i], idMessages[i].Value, null, m_chargeRewardListIconBG[i]);
        }
    }

    public void SetProgressMark(int i)
    {
        m_chargeRewardMessageProgressMark.localPosition = new Vector3
            (m_chargeRewardMessageProgressMarkPosition.x + i * 145,
            m_chargeRewardMessageProgressMarkPosition.y,
            m_chargeRewardMessageProgressMarkPosition.z);
    }

    public void SetBtnGroupText(List<int> data)
    {
        int count = data.Count > m_btnGroup.items.Length ? m_btnGroup.items.Length : data.Count;

        for (int i = 0; i < count; i++)
        {
            m_btnGroup.SetItemTextNum(i, data[i]);
        }
    }

    public void SwitchBtn(bool isCharge)
    {
        if (isCharge)
        {
            m_chargeBtn.SetActive(true);
            m_goChargeRewardUIBtnVIP0.SetActive(true);
            m_getRewardBtn.SetActive(false);
        }
        else
        {
            m_chargeBtn.SetActive(false);
            m_goChargeRewardUIBtnVIP0.SetActive(false);
            m_getRewardBtn.SetActive(true);
        }
    }

    #endregion

    #region 充值按钮特效

    private string m_fx1ChargeButton = "ChargeButtonFX1";

     /// <summary>
    /// 在充值按钮上附加特效
    /// </summary>
    private void AttachChargeButtonAnimation()
    {
        if (m_chargeBtn == null)
        {
            LoggerHelper.Error("m_chargeBtn is null");
            return;
        }

        MogoGlobleUIManager.Instance.ShowWaitingTip(true);

        MogoFXManager.Instance.AttachParticleAnim("fx_ui_baoxiangxingxing.prefab", m_fx1ChargeButton, m_chargeBtn.transform.position,
            MogoUIManager.Instance.GetMainUICamera().GetComponent<Camera>(), 0, 0, 0, () =>
            {
                MogoGlobleUIManager.Instance.ShowWaitingTip(false);
            });
    }

    /// <summary>
    /// 释放充值按钮特效
    /// </summary>
    private void ReleaseChargeButtonAnimation()
    {
        MogoFXManager.Instance.ReleaseParticleAnim(m_fx1ChargeButton);
    }

    #endregion

    #region 界面打开和关闭

    protected override void OnEnable()
    {
        base.OnEnable();
        AttachChargeButtonAnimation();

        if (!SystemSwitch.DestroyResource)
        {
            return;
        }

        //if (m_texGril.mainTexture == null)
        //{
        //    AssetCacheMgr.GetUIResource("gn-npc.png", (obj) =>
        //        {
        //            m_texGril.mainTexture = (Texture)obj;
        //        });
        //}
        //if (m_texBG.mainTexture == null)
        //{
        //    AssetCacheMgr.GetUIResource("ChargeRewardUIBG.png", (obj) =>
        //        {
        //            m_texBG.mainTexture = (Texture)obj;
        //        });
        //}
    }

    public void DestroyUIAndResources()
    {
        //Debug.LogError("1");
        if (SystemSwitch.DestroyAllUI)
        {
            MogoUIManager.Instance.DestroyChargeRewardUI();
        }
        if (!SystemSwitch.DestroyResource)
        {
            return;
        }
        //if (m_texGril != null)
        //{
        //    m_texGril.mainTexture = null;
        //    AssetCacheMgr.ReleaseResourceImmediate("gn-npc.png");
        //}

        //if (m_texBG != null)
        //{
        //    m_texBG.mainTexture = null;
        //    AssetCacheMgr.ReleaseResourceImmediate("ChargeRewardUIBG.png");
        //}
    }

    void OnDisable()
    {
        ReleaseChargeButtonAnimation();

        if (SystemSwitch.DestroyAllUI)
        {
            DestroyUIAndResources();
        }
    }

    #endregion
}

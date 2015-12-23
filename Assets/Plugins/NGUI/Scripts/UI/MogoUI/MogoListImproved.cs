using System;
using System.Collections.Generic;
using UnityEngine;
using Mogo.Util;

/// <summary>
/// 左负右正 上正下负 即原点在左下角  
/// unity3d中的屏幕坐标系是以屏幕左下角为（0,0）点，右上角为（Screen.Width,Screen.Height)
/// 为了方便，我们定义往右往上为1，所以向右边滑为1，向下面滑为-1
/// </summary>
public class MogoListImproved : MonoBehaviour
{
    private Camera sourceCamera = null;
    public Camera SourceCamera
    {
        get
        {
            return sourceCamera;
        }
        set
        {
            if (sourceCamera == null)
            {
                sourceCamera = value;
                ResetAffectBySourceCamera();
                TweenTo(CurrentPage, true);
            }
        }
    }

    private GameObject m_objCamera;
    public GameObject ObjCamera
    {
        get
        {
            return m_objCamera;
        }
        protected set
        {
            m_objCamera = value;
        }
    }

    public bool IsHorizontal = true;
    public bool IsOneButton = true; // true = 加上整体的一个BoxCollider
    public int IsOpposite = 1;
    public bool IsPaged = true; // true = 翻页, false = 滑动
    public bool IsBound = true; // true = 回弹
    public float UnitWidth;
    public float UnitHeight;
    public float XMargin;
    public float YMargin;
    public int RowSize;
    public int ColSize;
    public string PrefabName;
    public float stageSize = 1280;
    public int cameraDepth;
    public float SlideFactor = 1.0f;
    private Vector3 preDrag;
    private BoxCollider bc;
    private Transform m_transform;
    private bool tweenAble = false;
    private float tweenSpeed = 0.1f;
    private float tweenTimes = 10.0f; //tween步数
    private Vector3 tweenEnd;
    public Vector2 lastPoint;
    public Vector2 zeroPoint;
    public float width;
    public float height;
    public SortedList<int, UnityEngine.Object> DataList = new SortedList<int, UnityEngine.Object>();
    private Stack<UnityEngine.Object> DataGUIDList = new Stack<UnityEngine.Object>();
    private float XOffset;
    private Camera m_camera;

    /// <summary>
    /// 最大页数索引
    /// </summary>
    private byte m_maxPageIndex;
    public byte MaxPageIndex
    {
        get
        {
            return m_maxPageIndex;
        }
        protected set
        {
            m_maxPageIndex = value;
        }
    }

    /// <summary>
    /// 当前页
    /// </summary>
    private int m_currentPage;
    public int CurrentPage
    {
        get
        {
            return m_currentPage;
        }
        protected set
        {
            m_currentPage = value;
        }
    }

    void Start()
    {
        if (SourceCamera == null)
            SourceCamera = GameObject.Find("MogoMainUI").transform.FindChild("Camera").GetComponentsInChildren<Camera>(true)[0];
    }

    void ResetAffectBySourceCamera()
    {
        m_transform = transform;
        m_transform.localScale = new Vector3(1 / stageSize, 1 / stageSize, 1);
        if (width == 0)
            width = ColSize * UnitWidth + (ColSize - 1) * XMargin;
        if (height == 0)
            height = RowSize * UnitHeight + (RowSize - 1) * YMargin;
        if (IsOneButton)
        {
            bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(width, height, 0);
            bc.isTrigger = true;
            var cameraBtn = gameObject.AddComponent<MogoButton>();
            cameraBtn.pressHandler = PressHandler;
            cameraBtn.dragHandler = DragHandler;
        }

        ObjCamera = new GameObject();
        ObjCamera.transform.parent = transform;
        ObjCamera.transform.localScale = new Vector3(1, 1, 1);
        ObjCamera.name = transform.name + "Camera";
        m_camera = ObjCamera.AddComponent<Camera>();
        m_camera.clearFlags = CameraClearFlags.Depth;
        m_camera.cullingMask = 1 << gameObject.layer;
        m_camera.orthographic = true;
        m_camera.nearClipPlane = -50;
        m_camera.farClipPlane = 50;

        Vector3 center = m_transform.localPosition;
        m_transform.localPosition = new Vector3(-width / 2 + center.x, height / 2 + center.y, 0);
        Vector3 tl = sourceCamera.WorldToScreenPoint(m_transform.position);
        m_transform.localPosition = new Vector3(width / 2 + center.x, -height / 2 + center.y, 0);
        Vector3 br = sourceCamera.WorldToScreenPoint(m_transform.position);
        Rect rect = new Rect(tl.x / Screen.width, br.y / Screen.height,
            (br.x - tl.x) / Screen.width, (tl.y - br.y) / Screen.height);
        m_camera.rect = rect;
        m_camera.orthographicSize = rect.height;
        m_camera.depth = cameraDepth;
        UICamera uiCamera = ObjCamera.AddComponent<UICamera>();
        uiCamera.eventReceiverMask = 1 << gameObject.layer;

        TweenPosition tp = ObjCamera.AddComponent<TweenPosition>();
        tp.enabled = false; // 第一次打开设置当前页由于TweenPosition无效，故在此设置enabled为false

        // Mogo.Util.TimerHeap.AddTimer<Vector3>(500, 0, ResetPos, transform.localPosition);
        ResetPos(transform.localPosition);// 如果延时500ms调用，则Camera显示区域在这期间显示黑屏
    }

    private Vector2 getStartPoint(int page)
    {
        if (IsHorizontal)
        {
            return new Vector2((-width / 2 + page * width) * IsOpposite,
                           height / 2 * IsOpposite);
        }
        else
        {
            return new Vector2(-width / 2 * IsOpposite,
                        (height / 2 - page * height) * IsOpposite);
        }
    }

    public void SetGridLayout<T>(int NewCount, Transform parentTrans, Action loaded) where T : Component
    {
        if (width == 0)
            width = ColSize * UnitWidth + (ColSize - 1) * XMargin;
        if (height == 0)
            height = RowSize * UnitHeight + (RowSize - 1) * YMargin;
        int PageSize = ColSize * RowSize;
        MaxPageIndex = (byte)(Math.Ceiling((double)NewCount / PageSize) - 1);
        int curGridCount = DataGUIDList.Count;
        if (curGridCount < NewCount)
        {
            for (int index = curGridCount; index < NewCount; index++)
            {
                var uniqueID = index;
                var curPage = index / PageSize;
                var curIndex = index % PageSize;
                var curRow = curIndex / ColSize;
                var curCol = curIndex % ColSize;
                var startPoint = new Vector2();

                startPoint = getStartPoint(curPage);
                zeroPoint = Vector2.zero;
                var size = IsHorizontal ? ColSize : RowSize;
                var lastPageNum = NewCount % PageSize == 0 ? PageSize : NewCount % PageSize;
                var cmr = (NewCount / PageSize - 1)
                    + (lastPageNum >= size ? 1 : lastPageNum % size / (float)size);
                if (IsHorizontal)
                {
                    lastPoint = new Vector2(width * IsOpposite * cmr, 0);
                }
                else
                {
                    lastPoint = new Vector2(0, -height * IsOpposite * cmr);
                }

                AssetCacheMgr.GetUIInstance(PrefabName + ".prefab", (prefab, id, go) =>
                {
                    GameObject obj = (GameObject)go;
                    obj.transform.parent = parentTrans;
                    obj.transform.localPosition = new Vector3(startPoint.x
                        + (curCol * (XMargin + UnitWidth) + UnitWidth / 2) * IsOpposite
                        ,
                        startPoint.y
                        - (curRow * (YMargin + UnitHeight) + UnitHeight / 2) * IsOpposite
                        , 0.0f);
                    obj.transform.localScale = new Vector3(1, 1, 1);
                    if (!IsOneButton)
                    {
                        var btnList = obj.transform.GetComponentsInChildren<MogoButton>(true);
                        foreach (var single in btnList)
                        {
                            single.pressHandler += PressHandler;
                            single.dragHandler += DragHandler;
                        }
                    }
                    DataGUIDList.Push(go);

                    UnityEngine.Component unit = obj.AddComponent<T>();

                    /****手机系统上无法用该方式添加控件
                    if (!unit)
                    {
                        var type = Type.GetType(PrefabName);
                        if (type != null)
                            unit = obj.AddComponent(type);
                    }
                    if (unit == null)
                        LoggerHelper.Error("SetGridLayout unit is null: " + PrefabName);       
                     ****/

                    DataList.Add(uniqueID, unit);

                    if (DataList.Count == NewCount)
                    {
                        if (loaded != null)
                        {
                            loaded();
                        }
                    }
                });
            }
        }
        else
        {
            for (int i = 0; i < curGridCount - NewCount; i++)
            {
                DataList.RemoveAt(curGridCount - 1 - i);
                AssetCacheMgr.ReleaseInstance(DataGUIDList.Pop());
            }
            if (loaded != null)
            {
                loaded();
            }
        }
    }

    void Update()
    {
        if (tweenAble)
        {
            if (IsHorizontal)
            {
                ObjCamera.transform.localPosition = ObjCamera.transform.localPosition + new Vector3(tweenSpeed, 0, 0);
                if (Math.Abs(ObjCamera.transform.localPosition.x - tweenEnd.x) <= Math.Abs(tweenSpeed))
                {
                    ObjCamera.transform.localPosition = new Vector3(tweenEnd.x, ObjCamera.transform.localPosition.y, 0);
                    if (IsOneButton)
                    {
                        bc.center = new Vector3(tweenEnd.x, 0, 0);
                    }
                    else
                    {

                    }

                    tweenAble = false;
                    EventDispatcher.TriggerEvent<byte>(UIEvent.SlipPage, 0);
                    OnMovePageDone();
                }
            }
            else
            {
                ObjCamera.transform.localPosition = ObjCamera.transform.localPosition - new Vector3(0, tweenSpeed, 0);
                if (Math.Abs(ObjCamera.transform.localPosition.y - tweenEnd.y) <= Math.Abs(tweenSpeed))
                {
                    ObjCamera.transform.localPosition = new Vector3(ObjCamera.transform.localPosition.x, tweenEnd.y, 0);
                    if (IsOneButton)
                    {
                        bc.center = new Vector3(0, tweenEnd.y, 0);
                    }
                    else
                    {

                    }

                    tweenAble = false;
                    EventDispatcher.TriggerEvent<byte>(UIEvent.SlipPage, 0);
                    OnMovePageDone();
                }
            }
        }
    }

    /// <summary>
    /// 重置位置，包括GameObject和Camera
    /// GameObject默认位置为(2000, 2000, 0)
    /// Camera默认位置为Vector3.zero
    /// </summary>
    /// <param name="p"></param>
    private void ResetPos(Vector3 p)
    {
        ObjCamera.transform.localPosition = Vector3.zero;
        transform.localPosition = new Vector3(2000, 2000, 0);
    }

    /// <summary>
    /// 重置Camera位置,即移到第一页位置
    /// Camera默认位置为Vector3.zero
    /// </summary>
    public void ResetCameraPos()
    {
        if (ObjCamera != null)
            ObjCamera.transform.localPosition = Vector3.zero;
    }

    #region 事件

    #region 翻页

    /// <summary>
    /// 最后一页滑动回滚
    /// </summary>
    public Action<int> BackToMaxPage = null;
    private void OnBackToMaxPage()
    {
        if (BackToMaxPage != null)
            BackToMaxPage(MaxPageIndex);
    }

    /// <summary>
    /// 停止滑动
    /// </summary>
    public Action MovePageDone = null;
    private void OnMovePageDone()
    {
        if (MovePageDone != null)
            MovePageDone();

        SetArrow();
    }

    #endregion

    #region 滑动

    /// <summary>
    /// 最后一页滑动回滚
    /// </summary>
    public Action BackToLastPoint = null;
    private void OnBackToLastPoint()
    {
        if (BackToLastPoint != null)
            BackToLastPoint();
    }

    #endregion

    #region 事件监听

    // 外部监听
    public void PressHandlerOutSide(bool pressed)
    {
        PressHandler(pressed);
    }

    // 外部监听
    public void DragHandlerOutSide(Vector2 delta)
    {
        DragHandler(delta);
    }

    // 内部监听
    private void PressHandler(bool pressed)
    {
        if (pressed)
        {
            preDrag = ObjCamera.transform.localPosition;
        }
        else
        {
            if (IsPaged)
            {
                if (IsHorizontal)
                {
                    checkBounceForPage(preDrag.x - ObjCamera.transform.localPosition.x);
                }
                else
                {
                    checkBounceForPage(ObjCamera.transform.localPosition.y - preDrag.y);

                }
            }
            else
            {
                if (IsHorizontal)
                {
                    checkBounceForSmooth(preDrag.x - ObjCamera.transform.localPosition.x);
                }
                else
                {
                    checkBounceForSmooth(ObjCamera.transform.localPosition.y - preDrag.y);
                }
            }
        }
    }

    // 内部监听
    private void DragHandler(Vector2 delta)
    {

        if (IsHorizontal)
        {
            if (!IsBound && !verifyDragBound(delta.x))
            {
                ObjCamera.transform.localPosition -= new Vector3(delta.x, 0, 0);
            }
            else if (IsBound)
            {
                ObjCamera.transform.localPosition -= new Vector3(delta.x, 0, 0);
            }
        }
        else
        {
            if (!IsBound && !verifyDragBound(-delta.y))
            {
                ObjCamera.transform.localPosition -= new Vector3(0, delta.y, 0);
            }
            else if (IsBound)
            {
                ObjCamera.transform.localPosition -= new Vector3(0, delta.y, 0);
            }
        }
    }

    #endregion

    #region 边缘检测

    /// <summary>
    /// 翻页边缘检测
    /// </summary>
    /// <param name="delta"></param>
    private void checkBounceForPage(float delta)
    {
        if (delta * IsOpposite > 0)
        {
            if (CurrentPage <= 0)
            {
                TweenTo(0);
                return;
            }
            CurrentPage--;
            TweenTo(CurrentPage);
        }
        else if (delta * IsOpposite < 0)
        {
            if (CurrentPage >= MaxPageIndex)
            {
                // 最后一页回弹
                OnBackToMaxPage();
                TweenTo(MaxPageIndex);
                return;
            }
            CurrentPage++;
            TweenTo(CurrentPage);
        }
        else
        {
            //delta为0，没有滑动位置
        }
    }

    /// <summary>
    /// 滑动边缘检测
    /// </summary>
    /// <param name="delta"></param>
    private void checkBounceForSmooth(float delta)
    {
        float destination = 0.0f;
        if (!IsHorizontal)
        {
            destination = ObjCamera.transform.localPosition.y;
        }
        else
        {
            destination = ObjCamera.transform.localPosition.x;
        }
        if (delta * IsOpposite > 0)
        {
            //往零点划
            if (!IsHorizontal && destination * IsOpposite > zeroPoint.y * IsOpposite)
            {
                TweenToPos(zeroPoint.y);               
                return;
            }
            else if (IsHorizontal && destination * IsOpposite < zeroPoint.x * IsOpposite)
            {
                TweenToPos(zeroPoint.x);
                return;
            }
            if (!IsHorizontal)
            {
                destination += delta * SlideFactor;
                TweenToPos(destination * IsOpposite > zeroPoint.y * IsOpposite ? zeroPoint.y : destination);
            }
            else
            {
                destination -= delta * SlideFactor;
                TweenToPos(destination * IsOpposite < zeroPoint.x * IsOpposite ? zeroPoint.x : destination);
            }

        }
        else
        {
            //往终点划
            if (!IsHorizontal && destination * IsOpposite < lastPoint.y * IsOpposite)
            {
                TweenToPos(lastPoint.y);
                OnBackToLastPoint(); // 最后一点回弹
                return;
            }
            else if (IsHorizontal && destination * IsOpposite > lastPoint.x * IsOpposite)
            {
                TweenToPos(lastPoint.x);
                OnBackToLastPoint(); // 最后一点回弹
                return;
            }
            if (!IsHorizontal)
            {
                destination += delta * SlideFactor;
                if(destination * IsOpposite < lastPoint.y * IsOpposite)
                {
                    TweenToPos(lastPoint.y);
                    OnBackToLastPoint(); // 最后一点回弹
                }
                else
                {
                    TweenToPos(destination);
                }
            }
            else
            {
                destination -= delta * SlideFactor;
                if(destination * IsOpposite > lastPoint.x * IsOpposite)
                {
                    TweenToPos(lastPoint.x);
                    OnBackToLastPoint(); // 最后一点回弹
                }
                else
                {
                    TweenToPos(destination);
                }                
            }
        }
    }

    private bool verifyDragBound(float delta)
    {
        if (delta * IsOpposite > 0)
        {
            if (!IsHorizontal && ObjCamera.transform.localPosition.y < zeroPoint.y)
            {
                return true;
            }
            else if (IsHorizontal && ObjCamera.transform.localPosition.x > zeroPoint.x)
            {
                return true;
            }
        }
        else if (delta * IsOpposite < 0)
        {
            if (!IsHorizontal && ObjCamera.transform.localPosition.y > lastPoint.y)
            {
                return true;
            }
            else if (IsHorizontal && ObjCamera.transform.localPosition.x < lastPoint.x)
            {
                return true;
            }
        }
        return false;
    }

    #endregion    

    #endregion

    public void TweenToPos(float dis, bool bRightNow = false)
    {
        if (IsHorizontal)
        {
            tweenEnd = new Vector3(dis, 0, 0);

            if (!bRightNow)
            {
                tweenSpeed = (dis - ObjCamera.transform.localPosition.x) / tweenTimes;
                tweenAble = true;
            }
            else
            {
                Vector3 vector3 = new Vector3(tweenEnd.x, ObjCamera.transform.localPosition.y, 0);
                SetCameraLocalPosition(vector3);
                //Mogo.Util.TimerHeap.AddTimer<Vector3>(1000, 0, SetCameraLocalPosition, vector3);

                if (IsOneButton)
                {
                    bc.center = new Vector3(tweenEnd.x, 0, 0);
                }

                SetArrow();
            }
        }
        else
        {
            tweenEnd = new Vector3(0, dis, 0);

            if (!bRightNow)
            {
                tweenSpeed = (-dis + ObjCamera.transform.localPosition.y) / tweenTimes;
                tweenAble = true;
            }
            else
            {
                Vector3 vector3 = new Vector3(ObjCamera.transform.localPosition.x, tweenEnd.y, 0);
                SetCameraLocalPosition(vector3);
                //Mogo.Util.TimerHeap.AddTimer<Vector3>(1000, 0, SetCameraLocalPosition, vector3);

                if (IsOneButton)
                {
                    bc.center = new Vector3(0, tweenEnd.y, 0);
                }

                SetArrow();
            }
        }
    }

    /// <summary>
    /// 移到到某一页
    /// </summary>
    /// <param name="page">目标页</param>
    /// <param name="bRightNow">true:无滑动，直接移到目标页</param>
    public void TweenTo(int page, bool bRightNow = false)
    {
        if (page > MaxPageIndex)
        {
            LoggerHelper.Error("page > maxPage.");
            return;
        }

        CurrentPage = page;
        if (ObjCamera == null)
        {
            return; // ObjCamera在Start时创建, 故界面未第一次激活时为null
        }
#if UNITY_IPHONE
#else
        EventDispatcher.TriggerEvent<byte, byte>(UIEvent.ChangePage, (byte)page, (byte)MaxPageIndex);
#endif
        float dis = 0.0f;
        if (IsHorizontal)
        {
            dis = page * width * IsOpposite;
        }
        else
        {
            dis = -page * height * IsOpposite;
        }
        TweenToPos(dis, bRightNow);
    }

    /// <summary>
    /// 停止滑动
    /// 对于在滑动中创建Grid的时候,需要停止滑动,防止位置错乱
    /// </summary>
    public void StopTween()
    {
        tweenEnd = Vector3.zero;
        tweenSpeed = 0;
        tweenAble = false;
    }

    private void SetCameraLocalPosition(Vector3 vector3)
    {
        if (ObjCamera != null)
            ObjCamera.transform.localPosition = vector3;
    }

    #region  提示箭头和页点控制

    // 提示箭头
    public GameObject LeftArrow;
    public GameObject RightArrow;

    /// <summary>
    /// 设置提示箭头
    /// </summary>
    public void SetArrow()
    {
        if (LeftArrow != null && RightArrow != null)
        {
            if (MaxPageIndex > 0)
            {
                if (CurrentPage == 0)
                {
                    LeftArrow.SetActive(false);
                    RightArrow.SetActive(true);
                }
                else if (CurrentPage == MaxPageIndex)
                {
                    LeftArrow.SetActive(true);
                    RightArrow.SetActive(false);
                }
                else
                {
                    LeftArrow.SetActive(true);
                    RightArrow.SetActive(true);
                }
            }
            else
            {
                LeftArrow.SetActive(false);
                RightArrow.SetActive(false);
            }
        }
    }

    #endregion
}

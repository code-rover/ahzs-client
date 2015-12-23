/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：OKMsgBox
// 创建者：Joe Mo
// 修改者列表：
// 创建日期：2013-5-15
// 模块描述：MsgBox界面管理
//----------------------------------------------------------------*/

using UnityEngine;
using System.Collections;
using Mogo.Util;
using System;

public class OKMsgBox : MonoBehaviour
{

    UILabel m_lblBoxText;
    UILabel m_lblOKBtnText;

    Transform m_myTransform;

    Action m_actCallback;


    void Awake()
    {
        gameObject.SetActive(false);

        m_myTransform = transform;

        Initialize();

        m_lblBoxText = m_myTransform.FindChild("MsgBoxText").GetComponentsInChildren<UILabel>(true)[0];
        m_lblOKBtnText = m_myTransform.FindChild("MsgBoxBtn/MsgBoxBtnText").GetComponentsInChildren<UILabel>(true)[0];
        m_myTransform.FindChild("MsgBoxBtn").gameObject.AddComponent<OKCancelBoxButton>();
        m_myTransform.FindChild("MsgBoxBGMask").gameObject.AddComponent<MsgBoxBGMask>();
    }

    void OnOKButtonUp()
    {
        if(m_actCallback != null)
        {
            m_actCallback();
        }
    }


    void Initialize()
    {
        EventDispatcher.AddEventListener("MsgBoxBtnUp", OnOKButtonUp);
    }

    void Release()
    {
        EventDispatcher.RemoveEventListener("MsgBoxBtnUp", OnOKButtonUp);
    }

    public void SetBoxText(string text)
    {
        m_lblBoxText.text = text;
    }

    public void SetOKBtnText(string text)
    {
        m_lblOKBtnText.text = text;
    }


    public void SetCallback(Action callback)
    {
        m_actCallback = callback;
    }

}

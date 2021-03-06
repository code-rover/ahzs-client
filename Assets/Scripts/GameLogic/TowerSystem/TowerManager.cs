/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：TowerManager
// 创建者：Charles Zuo
// 修改者列表：
// 创建日期：2013-5-16
// 模块描述：爬塔管理
//----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Mogo.Game;
using Mogo.GameData;
using Mogo.Util;
using UnityEngine;

public class TowerData
{
    public int Highest { get; set; }
    public int CurrentLevel { get; set; }
    public Dictionary<int, int> Items { get; set; }
    public uint CountDown { get; set; }
    public int FailCount { get; set; }
    public int VIPSweepUsed { get; set; }
    public int NormalSweepUsed { get; set; }
}
public class ReportData
{
    public Int32 exp { get; set; }
    public Dictionary<int, int> Items { get; set; }
    public Int32 money { get; set; }
}
public class TowerInfo
{
    public Int32 cd { get; set; }
    public Int32 level { get; set; }
}
public enum MsgType : byte
{
    MSG_GET_TOWER_INFO = 27,            //         --获取试炼之塔的数据
    MSG_ENTER_TOWER = 28,               //         --进入指试炼之塔的指定层
    MSG_TOWER_SWEEP = 29,               //         --普通扫荡
    MSG_TOWER_VIP_SWEEP = 30,           //         --VIP扫荡
    MSG_CLEAR_TOWER_SWEEP_CD = 31,      //         --清除扫荡副本的CD
    MSG_CLIENT_TOWER_SUCCESS = 32,          //试炼之塔成功后由服务器返回到客户端
    MSG_CLIENT_TOWER_FAIL = 33,          //试炼之态失败后有服务器返回到客户端
    MSG_CLIENT_REPORT = 34,          //服务器向客户端发送战报
    MSG_TOWER_SWEEP_ALL = 35,
    MSG_TOWER_NOTIFY_COUNT_DOWN = 77,   //通知客户端倒数开始
    MSG_TOWER_START_DESTROY = 78,   //告诉客户端开始破坏
};

public class TowerManager : IEventManager
{

    private EntityMyself m_myself;

    public TowerManager(EntityMyself _myself)
    {
        m_myself = _myself;
        AddListeners();
    }

    public void AddListeners()
    {
        EventDispatcher.AddEventListener(Events.TowerEvent.EnterMap, OnEnterMap);
        EventDispatcher.AddEventListener(Events.TowerEvent.NormalSweep, NormalSweep);
        EventDispatcher.AddEventListener(Events.TowerEvent.VIPSweep, VIPSweep);
        EventDispatcher.AddEventListener(Events.TowerEvent.SweepAll, SweepAll);
        EventDispatcher.AddEventListener(Events.TowerEvent.GetInfo, GetInfo);
        EventDispatcher.AddEventListener(Events.TowerEvent.ClearCD, ClearCD);
        EventDispatcher.AddEventListener<int, bool>(Events.InstanceEvent.InstanceUnLoaded, OnInstanceLeave);
        EventDispatcher.AddEventListener<int, bool>(Events.InstanceEvent.InstanceLoaded, OnInstanceEnter);
    }
    public void RemoveListeners()
    {
        EventDispatcher.RemoveEventListener(Events.TowerEvent.EnterMap, OnEnterMap);
        EventDispatcher.RemoveEventListener(Events.TowerEvent.NormalSweep, NormalSweep);
        EventDispatcher.RemoveEventListener(Events.TowerEvent.VIPSweep, VIPSweep);
        EventDispatcher.RemoveEventListener(Events.TowerEvent.SweepAll, SweepAll);
        EventDispatcher.RemoveEventListener(Events.TowerEvent.GetInfo, GetInfo);
        EventDispatcher.RemoveEventListener(Events.TowerEvent.ClearCD, ClearCD);
        EventDispatcher.RemoveEventListener<int, bool>(Events.InstanceEvent.InstanceUnLoaded, OnInstanceLeave);
        EventDispatcher.RemoveEventListener<int, bool>(Events.InstanceEvent.InstanceLoaded, OnInstanceEnter);
    }

    private void OnInstanceLeave(int sceneID, bool isInstance)
    {

    }
    private void OnInstanceEnter(int sceneID, bool isInstance)
    {
        if (MapData.dataMap.Get(sceneID).type == MapType.ClimbTower)
        {
            MainUIViewManager.Instance.SetHpBottleVisible(false);
            MogoWorld.thePlayer.PlayFx(6029);
        }
        else if (!isInstance)
        {
            if (MainUIViewManager.Instance != null)
            {
                MainUIViewManager.Instance.SetHpBottleVisible(true);
                MogoWorld.thePlayer.RemoveFx(6029);
                MainUIViewManager.Instance.ShowClimbTowerCurrentInfo(false);
                MainUIViewManager.Instance.BeginCountDown1(false);                
            }

        }
    }
    private void TimerShow(int totalSeconds)
    {
        TimerHeap.AddTimer<int>(1000, 0,
            (sec) =>
            {
                Debug.LogError(sec);
                if (sec > 0)
                {
                    sec--;
                    MainUIViewManager.Instance.SetSelfAttackText(sec.ToString(), false);
                    TimerShow(sec);
                }
                else
                {
                    MainUIViewManager.Instance.SetSelfAttackText(String.Empty, false);
                }
            },
            totalSeconds);
    }

    private void ClearCD()
    {
        m_myself.RpcCall("TowerReq", MsgType.MSG_CLEAR_TOWER_SWEEP_CD, 0, 0, "");
    }
    private void OnEnterMap()
    {
        m_myself.RpcCall("TowerReq", MsgType.MSG_ENTER_TOWER, 0, 0, "");
    }
    private void NormalSweep()
    {
        m_myself.RpcCall("TowerReq", MsgType.MSG_TOWER_SWEEP, 0, 0, "");
    }
    private void VIPSweep()
    {
        m_myself.RpcCall("TowerReq", MsgType.MSG_TOWER_VIP_SWEEP, 0, 0, "");
    }
    private void SweepAll()
    {
        LoggerHelper.Debug("Sweep All");
        m_myself.RpcCall("TowerReq", MsgType.MSG_TOWER_SWEEP_ALL, 0, 0, "");
    }
    private void GetInfo()
    {
        m_myself.RpcCall("TowerReq", MsgType.MSG_GET_TOWER_INFO, 0, 0, "");
    }
    void ShowTooltips(String text)
    {
        MogoGlobleUIManager.Instance.Confirm(text, (rst) => { MogoGlobleUIManager.Instance.ConfirmHide(); }, LanguageData.GetContent(25561), LanguageData.GetContent(25562));
    }
    public void TowerResp(byte msgID, LuaTable info)
    {
        LoggerHelper.Warning("TowerResp" + msgID + ":" + info.ToString());
        int err = 0;
        switch (msgID)
        {
            case (byte)MsgType.MSG_GET_TOWER_INFO:
                TowerData towerData;
                if (Utils.ParseLuaTable(info, out towerData))
                {
                    ClimbTowerUILogicManager.Instance.Data = towerData;
                    if (MogoUIManager.Instance.IsWindowOpen((int)WindowName.Tower))
                    {
                        ClimbTowerUILogicManager.Instance.RefreshUI(towerData);
                    }
                    else
                    {
                        ChallengeUILogicManager.Instance.RefreshUI((int)ChallengeGridID.ClimbTower);
                    }
                }
                break;
            case (byte)MsgType.MSG_ENTER_TOWER:
                Dictionary<int, int> rst;
                if (Utils.ParseLuaTable(info, out rst))
                {
                    err = rst[1];
                    BattleMenuUILogicManager.Instance.m_TowerFinishSingle = false;
                }
                break;


            case (byte)MsgType.MSG_TOWER_SWEEP:
            case (byte)MsgType.MSG_TOWER_VIP_SWEEP:
            case (byte)MsgType.MSG_CLEAR_TOWER_SWEEP_CD:
                Dictionary<int, int> rst_sweep;
                if (Utils.ParseLuaTable(info, out rst_sweep))
                {
                    err = rst_sweep[1];


                }
                break;
            case (byte)MsgType.MSG_CLIENT_TOWER_SUCCESS:
                int level = Convert.ToInt32(info["1"]) + 1;
                EventDispatcher.TriggerEvent<bool, int>(Events.TowerEvent.CreateDoor, true, level);
                BattleMenuUILogicManager.Instance.FinishSingle();
                break;
            case (byte)MsgType.MSG_CLIENT_REPORT:
                {
                    Action act = () =>
                    {
                        MogoUIManager.Instance.OpenWindow((int)WindowName.Tower,
                            () =>
                            {
                                ClimbTowerUILogicManager.Instance.SetTowerGridLayout(
                                    () =>
                                    {
                                        //MogoGlobleUIManager.Instance.ShowWaitingTip(true);
                                        ClimbTowerUILogicManager.Instance.ResourceLoaded();
                                        ReportData reportData;
                                        if (Utils.ParseLuaTable(info, out reportData))
                                        {
                                            ClimbTowerUILogicManager.Instance.OpenReport(reportData);
                                        }
                                        EventDispatcher.TriggerEvent(Events.TowerEvent.GetInfo);
                                    });

                            });
                    };
                    if (MogoUIManager.Instance.IsWindowOpen((int)WindowName.Tower))
                    {
                        ClimbTowerUILogicManager.Instance.SetTowerGridLayout(
                            () =>
                            {
                                //MogoGlobleUIManager.Instance.ShowWaitingTip(true);
                                ClimbTowerUILogicManager.Instance.ResourceLoaded();
                                ReportData reportData;
                                if (Utils.ParseLuaTable(info, out reportData))
                                {
                                    ClimbTowerUILogicManager.Instance.OpenReport(reportData);
                                }
                                EventDispatcher.TriggerEvent(Events.TowerEvent.GetInfo);
                            });
                    }
                    else
                    {
                        MogoUIQueue.Instance.PushOne(act, MogoUIManager.Instance.m_NormalMainUI, "CLIENT_REPORT");
                    }

                }
                break;
            case (byte)MsgType.MSG_TOWER_NOTIFY_COUNT_DOWN:
                {
                    TowerInfo towerInfo;
                    if (Utils.ParseLuaTable(info, out towerInfo))
                    {
                        var cd = towerInfo.cd;
                        int hour = (int)cd / 3600;
                        int minute = (int)cd % 3600 / 60;
                        int sec = (int)cd % 60;
                        MainUIViewManager.Instance.BeginCountDown1(true, MogoCountDownTarget.ClimbTower, hour, minute, sec);
                        MainUIViewManager.Instance.SetClimbTowerCurrentNum(towerInfo.level);
                        MogoWorld.thePlayer.CurMissionID = (int)InstanceIdentity.TOWER;
                        MogoWorld.thePlayer.CurMissionLevel = towerInfo.level;
                        MainUIViewManager.Instance.ShowClimbTowerCurrentInfo(true);
                        MainUIViewManager.Instance.ResetPlayerBloodAnim();
                    }
                }
                break;
            case (byte)MsgType.MSG_TOWER_START_DESTROY:
                {
                    ClientEventData.TriggerGearEvent(180);
                    break;
                }
            default:
                break;

        }
        if (err > 0)
        {
            MogoMsgBox.Instance.ShowFloatingText(LanguageData.dataMap.Get(823 + err).content);
        }
    }
}
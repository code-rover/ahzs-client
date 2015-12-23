/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：BillboardLogicManager
// 创建者：MaiFeo
// 修改者列表：
// 创建日期：
// 模块描述：Billboard逻辑控制器
//----------------------------------------------------------------*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mogo.Util;
using Mogo.Game;
using Mogo.GameData;
using Mogo.View;

public enum GlobalBillBoardType
{
    Normal,
    OccupyTower
}

public class BillboardLogicManager : UILogicManager
{
    public static class BillboardLogicEvent
    {
        public const string SETBILLBOARDBLOOD = "SetBillboardBlood";
        public const string UPDATEBILLBOARDNAME = "SetBillboardName";
        public const string UPDATEBILLBOARDTONG = "SetBillboardTong";
        public const string UPDATEBILLBOARDPOS = "SetBillboardPos";
        public const string REMOVEBILLBOARD = "DeleteBillboard";
    }

    protected GlobalBillBoardType globalBillBoardCurrentType = GlobalBillBoardType.Normal;
    public GlobalBillBoardType GlobalBillBoardCurrentType
    {
        get
        {
            return globalBillBoardCurrentType;
        }

        set
        {
            globalBillBoardCurrentType = value;
            if (MogoWorld.thePlayer != null)
            {
                switch (globalBillBoardCurrentType)
                {
                    case GlobalBillBoardType.OccupyTower:
                        foreach (var entity in MogoWorld.Entities)
                        {
                            if (entity.Value is EntityPlayer)
                            {
                                EntityPlayer player = entity.Value as EntityPlayer;
                                if (player.factionFlag != MogoWorld.thePlayer.factionFlag)
                                {
                                    // 队友
                                    BillboardLogicManager.Instance.AddInfoBillboard(player.ID, player.Transform, player, true, Mogo.View.HeadBloodColor.Blue);
                                    BillboardViewManager.Instance.SetHead(player, BillboardViewManager.HeadStatus.PVP, BillboardViewManager.PVPCamp.CampOwn);
                                }
                                else
                                {
                                    // 对手
                                    BillboardLogicManager.Instance.AddInfoBillboard(player.ID, player.Transform, player, true, Mogo.View.HeadBloodColor.Red);
                                    BillboardViewManager.Instance.SetHead(player, BillboardViewManager.HeadStatus.PVP, BillboardViewManager.PVPCamp.CampEnemy);
                                }
                            }
                        }
                        break;

                    case GlobalBillBoardType.Normal:
                        foreach (var entity in MogoWorld.Entities)
                        {
                            if (entity.Value is EntityPlayer)
                            {
                                // 全部去除
                                BillboardLogicManager.Instance.AddInfoBillboard(entity.Value.ID, entity.Value.Transform, entity.Value, false);
                                BillboardViewManager.Instance.SetHead(entity.Value, BillboardViewManager.HeadStatus.Normal);
                            }
                        }
                        break;
                }
            }
        }
    }


    private static BillboardLogicManager m_instance;

    public static BillboardLogicManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new BillboardLogicManager();
            }

            return BillboardLogicManager.m_instance;

        }
    }

    Dictionary<uint, Transform> m_dicePlayerIdtoTransformInfo = new Dictionary<uint, Transform>();

    public void AddSplitBattleBillboard(Vector3 pos, int blood, SplitBattleBillboardType type)
    {
        pos = GameObject.Find("Camera").GetComponentsInChildren<Camera>(true)[0].ScreenToWorldPoint(Camera.mainCamera.WorldToScreenPoint(pos));
        BillboardViewManager.Instance.AddSplitBattleBillboard(pos, blood, type);
    }

    public void AddAloneBattleBillboard(Vector3 pos, int num, AloneBattleBillboardType type)
    {
        pos = GameObject.Find("Camera").GetComponentsInChildren<Camera>(true)[0].ScreenToWorldPoint(Camera.mainCamera.WorldToScreenPoint(pos));
        BillboardViewManager.Instance.AddAloneBattleBillboard(pos, num, type);
    }

    /// <summary>
    /// 附加头顶信息
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="trans"></param>
    /// <param name="showBlood">是否显示血条</param>
    /// <param name="bloodColor">血条颜色</param>
    /// <param name="showAnger">是否显示怒气条</param>
    /// <param name="self"></param>
    public void AddInfoBillboard(uint playerId, Transform trans, EntityParent self, 
        bool showBlood, HeadBloodColor bloodColor = HeadBloodColor.Red, 
        bool showAnger = false)
    {
        if (trans == null)
            return;

        if (trans.FindChild("slot_billboard") != null)                        //MaiFeo Begin
        {
            trans = trans.FindChild("slot_billboard");
        }

        BillboardViewManager.Instance.AddInfoBillboard(playerId, trans.FindChild("slot_billboard"), self, showBlood, bloodColor, showAnger);
        if (m_dicePlayerIdtoTransformInfo.ContainsKey(playerId))
        {
            m_dicePlayerIdtoTransformInfo.Remove(playerId);
        }
        m_dicePlayerIdtoTransformInfo.Add(playerId, trans);                   //MaiFeo End
    }

    public void AddTaskBillboard(uint playerId)
    {
        BillboardViewManager.Instance.AddTaskBillboard(playerId);
    }

    public void RemoveBillboard(uint playerId)
    {
        BillboardViewManager.Instance.RemoveBillboard(playerId);
    }

    public void RemoveAllBillboard()
    {
        foreach (var item in m_dicePlayerIdtoTransformInfo)
        {
            RemoveBillboard(item.Key);
        }
    }

    //InfoBillboardFunc
    /// <summary>
    /// 设置头顶名称
    /// </summary>
    /// <param name="entity"></param>
    public void SetHead(EntityParent entity)
    {
        BillboardViewManager.Instance.SetHead(entity);
    }

    //TaskBillboardFunc

    public void ShowTaskIcon(uint playerId, uint idx)
    {
        BillboardViewManager.Instance.ShowTaskIcon(playerId, idx);
    }

    public override void Release()
    {
        base.Release();
        m_dicePlayerIdtoTransformInfo.Clear();
    }
}

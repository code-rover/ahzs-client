// 模块名   :  PrefabScriptManager
// 创建者   :  莫卓豪
// 创建日期 :  2013-8-1
// 描    述 :  PrefabScriptManager

using UnityEngine;
using System.Collections;

public class PrefabScriptManager : MonoBehaviour
{

    void Awake()
    {
        GameObject.Find("MogoNotice").AddComponent<MogoNotice>();
        GameObject.Find("MogoNotice2").AddComponent<MogoNotice2>();
        GameObject.Find("MsgBoxPanel").AddComponent<MogoMsgBox>();
        GameObject.Find("EquipTipRoot").AddComponent<EquipTipManager>();
        GameObject.Find("EquipUpgrade").AddComponent<EquipUpgradeViewManager>();
        GameObject.Find("EquipExchange").AddComponent<EquipExchangeUIViewManager>();
        GameObject.Find("BattleRecord").AddComponent<BattleRecordUIViewManager>();
        GameObject.Find("Enhant").AddComponent<FumoUIViewManager>();

        GameObject mogoMainUIPanel = GameObject.Find("MogoMainUIPanel");
        GameObject.Find("MogoMainUI").transform.FindChild("Camera").gameObject.AddComponent<BillboardViewManager>();
        mogoMainUIPanel.transform.FindChild("DebugUI").gameObject.AddComponent<DebugUIViewManager>();

        GameObject.Find("MogoGlobleUIPanel").AddComponent<MogoGlobleUIManager>();
        mogoMainUIPanel.AddComponent<MogoUIManager>();
        //mogoMainUIPanel.transform.FindChild("TeachUICamera/Anchor/TeachUIPanel").gameObject.AddComponent<TeachUIViewManager>();
        GameObject.Find("TeachUIPanel").AddComponent<TeachUIViewManager>();
        GameObject.Find("MogoGlobleUIPanel").transform.FindChild("MogoGlobleLoadingUI").gameObject.AddComponent<MogoGlobleLoadingUI>();
        GameObject.Find("MogoGlobleUIPanel").transform.FindChild("PassRewardUI").gameObject.AddComponent<PassRewardUI>();
        //GameObject.Find("BillboardPanel").transform.FindChild("SandFX").gameObject.AddComponent<SandFX>().scrollSpeed = 3f;

        mogoMainUIPanel.AddComponent<MogoUIQueue>();
        mogoMainUIPanel.AddComponent<MogoOKCancelBoxQueue>();

        mogoMainUIPanel.transform.FindChild("MainUI").gameObject.AddComponent<MainUIViewManager>();
        mogoMainUIPanel.transform.FindChild("NormalMainUI").gameObject.AddComponent<NormalMainUIViewManager>();
        mogoMainUIPanel.transform.FindChild("NormalMainUI").gameObject.SetActive(true);
        mogoMainUIPanel.transform.FindChild("NormalMainUI").gameObject.SetActive(false);
    }
}

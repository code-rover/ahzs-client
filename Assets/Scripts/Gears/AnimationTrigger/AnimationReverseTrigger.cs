using UnityEngine;
using System.Collections;

using Mogo.Util;

public class AnimationReverseTrigger : GearParent
{
    public AnimationClip clip;
    public bool isTriggerRepeat;

    void Start()
    {
        gearType = "AnimationTrigger";
        //triggleEnable = true;
        //stateOne = true;

        ID = (uint)defaultID;

        AddListeners();
    }

    void OnDestroy()
    {
        RemoveListeners();
    }


    #region 碰撞触发

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == GearParent.MogoPlayerTag)
        {
            if (triggleEnable)
            {
                animation[clip.name].speed = 1;
                animation.CrossFade(clip.name);

                if (!isTriggerRepeat)
                    triggleEnable = false;
            }
        }
    }

    #endregion


    #region 机关触发

    protected override void SetGearEventStateOne(uint stateOneID)
    {
        if (stateOneID == ID)
        {
            animation[clip.name].normalizedTime = 1f;
            animation[clip.name].speed = -1;
            animation.Play();
            base.SetGearEventStateOne(stateOneID);
        }
    }

    protected override void SetGearEventStateTwo(uint stateTwoID)
    {
        if (stateTwoID == ID)
        {
            animation[clip.name].normalizedTime = 0f;
            animation[clip.name].speed = 1;
            animation.Play();
            base.SetGearEventStateTwo(stateTwoID);
        }
    }

    #endregion


    #region 断线重连

    protected override void SetGearStateOne(uint stateOneID)
    {
        if (stateOneID == ID)
        {
            animation[clip.name].normalizedTime = 1f;
            animation[clip.name].speed = -1;
            animation.Play();
            base.SetGearStateOne(stateOneID);
        }
    }

    protected override void SetGearStateTwo(uint stateTwoID)
    {
        if (stateTwoID == ID)
        {
            animation[clip.name].normalizedTime = 0f;
            animation[clip.name].speed = 1;
            animation.Play();
            base.SetGearStateTwo(stateTwoID);
        }
    }

    #endregion
}

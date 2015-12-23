/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：EntityParent
// 创建者：Steven Yang
// 修改者列表：
// 创建日期：2013-1-29
// 模块描述：客户端 Entity基类
//----------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections.Generic;

using Mogo.FSM;
using Mogo.GameData;
using Mogo.Util;
using Mogo.RPC;
using Mogo.Task;

namespace Mogo.Game
{
    /// <summary>
    /// 逻辑控制类，这里处理Entity数据变化，状态变化等逻辑
    /// </summary>
    public partial class EntityParent : NotifyPropChanged, INotifyPropChanged
    {
        #region 虚方法

        virtual public void MainCameraCompleted()
        {

        }

        virtual public void CreateModel()
        {
            if (GameObject)
            {
                MogoWorld.GameObjects.Add(GameObject.GetInstanceID(), this);
            }
        }

        virtual public void CreateActualModel()
        {

        }

        virtual public void CreateDeafaultModel()
        {

        }

        virtual public void ApplyRootMotion(bool b)
        {
            if (animator == null)
            {
                return;
            }
            animator.applyRootMotion = b;
        }

        virtual public void SetAction(int act)
        {
            if (animator == null)
            {
                return;
            }
            animator.SetInteger("Action", act);
            if (weaponAnimator)
            {
                weaponAnimator.SetInteger("Action", act);
            }
            if (act == ActionConstants.HIT_AIR)
            {
                stiff = true;
                hitAir = true;
                //Actor.HitStateChanged = HitStateChange;
                //TimerHeap.DelTimer(revertHitTimerID);
                //revertHitTimerID = TimerHeap.AddTimer(ActionTime.HIT_AIR, 0, DelayCheck);
            }
            else if (act == ActionConstants.KNOCK_DOWN)
            {
                stiff = true;
                knockDown = true;
                //Actor.HitStateChanged = HitStateChange;
                //TimerHeap.DelTimer(revertHitTimerID);
                //revertHitTimerID = TimerHeap.AddTimer(ActionTime.KNOCK_DOWN, 0, DelayCheck);
            }
            else if (act == ActionConstants.HIT)
            {
                stiff = true;
                //Actor.HitStateChanged = HitStateChange;
                //TimerHeap.DelTimer(revertHitTimerID);
                //revertHitTimerID = TimerHeap.AddTimer(ActionTime.HIT, 0, DelayCheck);
            }
            else if (act == ActionConstants.PUSH)
            {
                stiff = true;
                //Actor.HitStateChanged = HitStateChange;
                //TimerHeap.DelTimer(revertHitTimerID);
                //revertHitTimerID = TimerHeap.AddTimer(ActionTime.PUSH, 0, DelayCheck);
            }
            else if (act == ActionConstants.HIT_GROUND)
            {
                stiff = true;
                hitGround = true;
                //Actor.HitStateChanged = HitStateChange;
                //TimerHeap.DelTimer(revertHitTimerID);
                //revertHitTimerID = TimerHeap.AddTimer(ActionTime.HIT_GROUND, 0, DelayCheck);
            }
        }

        private void HitStateChange(string name, bool start)
        {
            if ((name.EndsWith("ready") || name.EndsWith("run")) && start)
            {
                Actor.HitStateChanged = null;
                ClearHitAct();
            }
        }

        public void ClearHitAct()
        {
            if (this is EntityMyself)
            {
                //Transform.localRotation = preQuaternion;
                currSpellID = -1; //用于攻击中受击打断后的再次容错
            }
            ChangeMotionState(MotionState.IDLE);
            hitAir = false;
            knockDown = false;
            hitGround = false;
            stiff = false;
            EventDispatcher.TriggerEvent(Events.AIEvent.DummyStiffEnd, Transform.gameObject);
            //TimerHeap.AddTimer(500, 0, DelayCheck);//延时再次容错判定
        }

        private void DelayCheck()
        {
            if (animator == null)
            {
                return;
            }
            if (CurrentMotionState == MotionState.HIT && animator.GetInteger("Action") == 0)
            {
                ClearHitAct();
            }
            if (stiff && animator.GetInteger("Action") == 0)
            {
                ClearHitAct();
            }
        }

        virtual public void SetSpeed(float speed)
        {
            if (animator == null)
            {
                return;
            }
            animator.SetFloat("Speed", speed);
        }

        virtual public bool IsInTransition()
        {
            return animator.IsInTransition(0);
        }

        virtual public void ChangeMotionState(string newState, params System.Object[] args)
        {
            fsmMotion.ChangeStatus(this, newState, args);
        }

        virtual public void ChangeMotionStateInFrames(string newState, params System.Object[] args)
        {
            fsmMotion.ChangeStatus(this, newState, args);
        }

        // 对象进入场景，在这里初始化各种数据， 资源， 模型等
        // 传入数据。
        virtual public void OnEnterWorld()
        {
            // todo: 这里会加入数据解析
            buffManager = new BuffManager(this);
            EventDispatcher.AddEventListener<GameObject, Vector3>(MogoMotor.ON_MOVE_TO, OnMoveTo);
            EventDispatcher.AddEventListener<GameObject, Vector3, float>(MogoMotor.ON_MOVE_TO_FALSE, OnMoveToFalse);
        }

        // 对象从场景中删除， 在这里释放资源
        virtual public void OnLeaveWorld()
        {
            // todo: 这里会释放资源
            EventDispatcher.RemoveEventListener<GameObject, Vector3>(MogoMotor.ON_MOVE_TO, OnMoveTo);
            EventDispatcher.RemoveEventListener<GameObject, Vector3, float>(MogoMotor.ON_MOVE_TO_FALSE, OnMoveToFalse);
            if (buffManager != null)
            {
                buffManager.Clean();
            }
            RemoveListener();
            ClearBinding();
            if (GameObject)
            {
                MogoWorld.GameObjects.Remove(GameObject.GetInstanceID());
            }
            //if (MogoWorld.Entities.ContainsKey(ID))
            //{
            //    MogoWorld.Entities.Remove(ID);
            //}
            if (Actor)
                Actor.ReleaseController();
            GameObject.Destroy(GameObject);
            //AssetCacheMgr.ReleaseInstance(GameObject, false);
            GameObject = null;
            Transform = null;
            weaponAnimator = null;
            animator = null;
            motor = null;
            sfxHandler = null;
            audioSource = null;
        }

        virtual public void Idle()
        {
            if ((this is EntityMyself) && (this as EntityMyself).deathFlag == 1)
            {
                return;
            }
            if (battleManger == null)
            {
                ChangeMotionState(MotionState.IDLE);
            }
            else
            {
                this.battleManger.Idle();
            }
        }

        virtual public void Roll()
        {
            this.battleManger.Roll();
        }

        #endregion

        #region 公共方法

        #region 技能相关

        /// <summary>
        /// 去除重力，会连动作的自带位移都去掉，慎用
        /// （animator对带chactorController的物体自带一个重力，暂没找到更好的去除方法）
        /// </summary>
        public void RemoveGravity()
        {
            motor.gravity = 0;
            //animator.applyRootMotion = false;
        }

        public void SetGravity(float gravity = 20)
        {
            motor.gravity = gravity;
            //animator.applyRootMotion = true;
        }

        /// <summary>
        /// 冻结
        /// </summary>
        public void SetFreeze()
        {
            if (this is EntityMyself)
            {
                motor.enableStick = false;
            }
            SetSpeedReduce();

        }

        /// <summary>
        /// 解冻
        /// </summary>
        public void SetThaw()
        {
            SetSpeedRecover();
            motor.enableStick = true;
        }

        /// <summary>
        /// 减速
        /// </summary>
        /// <param name="speedRate">减速率</param>
        public void SetSpeedReduce(float speedRate = 0)
        {
            if (isSrcSpeed)
            {
                isSrcSpeed = false;
                srcSpeed = animator.speed;
                gearMoveSpeedRate = speedRate;
            }
            animator.speed = srcSpeed * speedRate;
            // motor.SetSpeed(motor.speed);
        }

        /// <summary>
        /// 恢复速度
        /// </summary>
        public void SetSpeedRecover()
        {
            if (!isSrcSpeed)
            {
                gearMoveSpeedRate = 1;
                isSrcSpeed = true;
                animator.speed = srcSpeed;
                // motor.SetSpeed(0);
            }
        }

        /// <summary>
        /// 分身
        /// </summary>
        public void CreateDuplication()
        {
            GameObject duplication = (GameObject)UnityEngine.Object.Instantiate(Actor.gameObject, Vector3.zero, Quaternion.identity);

            MonoBehaviour[] scripts = duplication.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script.GetType() != typeof(Transform))
                {
                    UnityEngine.Object.Destroy(script);
                }
            }

            Animator anima = duplication.GetComponent<Animator>();
            if (anima != null)
                UnityEngine.Object.Destroy(anima);

            CharacterController chaController = duplication.GetComponent<CharacterController>();
            if (chaController != null)
                UnityEngine.Object.Destroy(chaController);

            NavMeshAgent navAgent = duplication.GetComponent<NavMeshAgent>();
            if (navAgent != null)
                UnityEngine.Object.Destroy(navAgent);

            duplication.transform.position = Transform.position;
            duplication.transform.rotation = Transform.rotation;
            TimerHeap.AddTimer<GameObject>(1000, 0, (copy) => { UnityEngine.Object.Destroy(copy); }, duplication);
        }

        #endregion

        public int GetIntAttr(string attrName)
        {
            return intAttrs.GetValueOrDefault(attrName, 0);
        }

        public void SetIntAttr(string attrName, int value)
        {
            intAttrs[attrName] = value;
        }

        public void SetDoubleAttr(string attrName, double value)
        {
            doubleAttrs[attrName] = value;
        }

        public double GetDoubleAttr(string attrName)
        {
            return doubleAttrs.GetValueOrDefault(attrName, 0);
        }

        public string GetStringAttr(string attrName)
        {
            return stringAttrs.GetValueOrDefault(attrName, "");
        }

        public object GetObjectAttr(string attrName)
        {
            return objectAttrs.GetValueOrDefault(attrName, null);
        }

        public void SetObjectAttr(string attrName, object value)
        {
            objectAttrs[attrName] = value;
        }

        public void PlaySfx(int nSpellID)
        {
            if (sfxManager == null)
            {
                return;
            }
            sfxManager.PlaySfx(nSpellID);
        }

        public void RemoveSfx(int nSpellID)
        {
            if (sfxManager == null)
            {
                return;
            }
            sfxManager.RemoveSfx(nSpellID);
        }

        public void PlayFx(int fxID, Transform target = null, Action<GameObject, int> action = null)
        {
            if (sfxHandler)
                sfxHandler.HandleFx(fxID, target, action);
        }

        public void RemoveFx(int fxID)
        {
            if (sfxHandler)
                sfxHandler.RemoveFXs(fxID);
        }

        // 服务器远程过程调用
        public void RpcCall(string func, params object[] args)
        {
            ServerProxy.Instance.RpcCall(func, args);
        }

        public void OnPositionChange(float pX, float pY, float pZ)
        {
            RpcCall("OnPositionChange", pX, pY, pZ);
        }

        public void OnRotationChange(float rX, float rY, float rZ)
        {
            RpcCall("OnRotationChange", rX, rY, rZ);
        }

        public virtual void UpdatePosition()
        {
            if (MogoWorld.isLoadingScene)
                return;
            Vector3 point;
            if (Transform)
            {
                if (Mogo.Util.MogoUtils.GetPointInTerrain(position.x, position.z, out point))
                {
                    Transform.position = new Vector3(point.x, point.y + 0.3f, point.z);
                    if (rotation != Vector3.zero)
                        Transform.eulerAngles = new Vector3(0, rotation.y, 0);
                }
                else
                {
                    var myself = this as EntityMyself;
                    if (myself != null)//主角碰撞失败就拉到场景出生点
                    {
                        var map = MapData.dataMap.Get(myself.sceneId);
                        LoggerHelper.Warning("Pull character to born point: " + map.enterX * 0.01 + ", " + map.enterY * 0.01);
                        Vector3 bornPoint;
                        if (Mogo.Util.MogoUtils.GetPointInTerrain((float)(map.enterX * 0.01), (float)(map.enterY * 0.01), out bornPoint))
                        {
                            Transform.position = new Vector3(bornPoint.x, bornPoint.y + 0.5f, bornPoint.z);
                        }
                        else
                        {
                            Transform.position = new Vector3(bornPoint.x, bornPoint.y, bornPoint.z);
                            //if (motor)
                            //    motor.gravity = 10000f;
                        }
                    }
                    else
                    {
                        Transform.position = new Vector3(point.x, point.y + 1, point.z);
                        //if (motor)
                        //    motor.gravity = 10000f;
                    }
                }
            }

        }

        public void GotoPreparePosition()
        {
            if (Transform)
            {
                GameObject.layer = 14;

                Transform.position = Transform.position - new Vector3(0, 10000, 0);
            }
        }

        public void SetPosition()
        {
            if (Transform)
            {
                position = Transform.position;
                rotation = Transform.eulerAngles;
            }
        }

        public void SetPositon(float pX, float pY, float pZ)
        {
            if (Transform != null)
            {
                Transform.position = new Vector3(pX, pY, pZ);
            }

        }

        public void SetRotation(float rX, float rY, float rZ)
        {
            if (Transform != null)
            {
                Transform.eulerAngles = new Vector3(rX, rY, rZ);
            }

        }
        public bool isCreatingModel = false;

        public void Equip(int _equipId)
        {
            if (Transform == null)
            {
                return;
            }
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            if (!ItemEquipmentData.dataMap.ContainsKey(_equipId))
            {
                LoggerHelper.Error("can not find equip:" + _equipId);
                return;
            }
            ItemEquipmentData equip = ItemEquipmentData.dataMap[_equipId];
            if (equip.mode > 0)
            {
                if (Actor == null)
                {
                    return;
                }
                Actor.m_isChangingWeapon = true;
                Actor.Equip(equip.mode);
                if (equip.type == (int)EquipType.Weapon)
                {
                    ControllerOfWeaponData controllerData = ControllerOfWeaponData.dataMap[equip.subtype];
                    RuntimeAnimatorController controller;
                    if (animator == null) return;
                    string controllerName = (MogoWorld.inCity ? controllerData.controllerInCity : controllerData.controller);
                    if (animator.runtimeAnimatorController != null)
                    {
                        if (animator.runtimeAnimatorController.name == controllerName) return;
                        AssetCacheMgr.ReleaseResource(animator.runtimeAnimatorController);
                    }

                    AssetCacheMgr.GetResource(controllerName,
                    (obj) =>
                    {
                        controller = obj as RuntimeAnimatorController;
                        if (animator == null) return;
                        animator.runtimeAnimatorController = controller;
                        if (this is EntityMyself)
                        {
                            (this as EntityMyself).UpdateSkillToManager();
                            EventDispatcher.TriggerEvent<int, int>(InventoryEvent.OnChangeEquip, equip.type, equip.subtype);
                        }
                        if (this is EntityPlayer)
                        {
                            if (MogoWorld.inCity)
                            {
                                animator.SetInteger("Action", -1);
                            }
                            else
                            {
                                animator.SetInteger("Action", 0);
                            }
                            if (MogoWorld.isReConnect)
                            {
                                ulong s = stateFlag;
                                stateFlag = 0;
                                stateFlag = s;
                            }
                        }
                    });
                }

                stopWatch.Stop();

                //if (!isCreatingModel)
                //{
                //    SetPosition();
                //    stopWatch.Start();
                //    //AssetCacheMgr.ReleaseInstance(GameObject);
                //    CreateActualModel();
                //    stopWatch.Stop();
                //    Mogo.Util.LoggerHelper.Debug("CreateModel:" + stopWatch.Elapsed.Milliseconds);

                //}
            }

        }

        public void RemoveEquip(int _equipId)
        {
            //if (!ItemEquipmentData.dataMap.ContainsKey(_equipId))
            //{
            //    return;
            //}
            //ItemEquipmentData equip = ItemEquipmentData.dataMap[_equipId];
            //if (equip.mode > 0)
            //{
            //    Transform.GetComponent<ActorParent>().RemoveEquid(equip.mode);

            //    if (equip.type == (int)EquipType.Weapon)
            //    {
            //        ControllerOfWeaponData controllerData = ControllerOfWeaponData.dataMap[0];
            //        RuntimeAnimatorController controller;
            //        AssetCacheMgr.GetResource(controllerData.controller,
            //        (obj) =>
            //        {
            //            controller = obj as RuntimeAnimatorController;
            //            animator.runtimeAnimatorController = controller;
            //        });

            //    }

            //    //if (!isCreatingModel)
            //    //{
            //    //    System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            //    //    stopWatch.Start();
            //    //    SetPosition();
            //    //    AssetCacheMgr.ReleaseInstance(GameObject);
            //    //    CreateActualModel();
            //    //    stopWatch.Stop();
            //    //    Mogo.Util.LoggerHelper.Debug("CreateModel:" + stopWatch.Elapsed.Milliseconds);
            //    //    MogoMainCamera.Instance.target = Mogo.Util.MogoUtils.GetChild(GameObject.transform, "slot_camera");
            //    //}
            //}
        }

        /// <summary>
        /// 根据网络数据设置实体属性值。
        /// </summary>
        /// <param name="args"></param>
        public void SetEntityInfo(BaseAttachedInfo info)
        {
            ID = info.id;
            dbid = info.dbid;
            entity = info.entity;
            SynEntityAttrs(info);
        }

        /// <summary>
        /// 根据网络数据设置实体属性值。
        /// </summary>
        /// <param name="args"></param>
        public void SetEntityCellInfo(CellAttachedInfo info)
        {
            position = info.position;
            //rotation = info.rotation;
            SynEntityAttrs(info);
            hadSyncProp = true;
            //不应该在属性同步后立即更新坐标，有问题找邓永健
            //if (Transform)
            //    UpdatePosition();
        }

        public void SynEntityAttrs(AttachedInfo info)
        {
            if (info.props == null)
                return;
            var type = this.GetType();
            foreach (var prop in info.props)
            {
                //Mogo.Util.LoggerHelper.Debug("SynEntityAttrs:------ " + prop.Key + " " + prop.Value);
                SetAttr(prop.Key, prop.Value, type);
            }
            //if (!MogoWorld.isLoadingScene)
            //{
            //    UpdateView();
            //}
            //else
            //{
            //    hasCache = true;
            //}
        }

        /// <summary>
        /// 更新UI，由子类实现
        /// </summary>
        public virtual void UpdateView()
        {
        }


        #endregion

        #region 受保护方法

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        protected void SetAttr(EntityDefProperties propInfo, object value, Type type)
        {
            var prop = type.GetProperty(propInfo.Name);
            try
            {
                if (prop != null)
                {
                    // Mogo.Util.LoggerHelper.Debug("prop: " + prop.Name + " value: " + value);
                    prop.SetValue(this, value, null);
                }
                else
                {
                    var typeCode = Type.GetTypeCode(propInfo.VType.VValueType);
                    if (m_intSet.Contains(typeCode))
                        intAttrs[propInfo.Name] = Convert.ToInt32(value);
                    else if (m_doubleSet.Contains(typeCode))
                        doubleAttrs[propInfo.Name] = Convert.ToDouble(value);
                    else if (propInfo.VType.VValueType == typeof(string))
                        stringAttrs[propInfo.Name] = value as string;
                    else
                        objectAttrs[propInfo.Name] = value;
                    //LoggerHelper.Info("Static property not found: " + propInfo.Name);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("SetAttr error: " + propInfo.VType.VValueType + ":" + propInfo.Name + " " + value.GetType() + ":" + value + "\n" + ex);
                LoggerHelper.Error("prop: " + prop + " this: " + this.GetType());
            }
        }

        /// <summary>
        /// 订阅Define文件里网络回调函数。
        /// </summary>
        protected void AddListener()
        {
            var ety = Mogo.RPC.DefParser.Instance.GetEntityByName(entityType);
            if (ety == null)
            {
                LoggerHelper.Warning("Entity not found: " + entityType);
                return;
            }
            foreach (var item in ety.ClientMethodsByName)
            {
                var methodName = item.Key;
                var method = this.GetType().GetMethod(methodName, ~System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    var e = new KeyValuePair<string, Action<object[]>>(String.Concat(Mogo.Util.Utils.RPC_HEAD, methodName), (args) =>
                    {//RPC回调事件处理
                        try
                        {
                            //LoggerHelper.Debug("RPC_resp: " + methodName);
                            method.Invoke(this, args);
                        }
                        catch (Exception ex)
                        {
                            var sb = new System.Text.StringBuilder();
                            sb.Append("method paras are: ");
                            foreach (var methodPara in method.GetParameters())
                            {
                                sb.Append(methodPara.ParameterType + " ");
                            }
                            sb.Append(", rpc resp paras are: ");
                            foreach (var realPara in args)
                            {
                                sb.Append(realPara.GetType() + " ");
                            }

                            Exception inner = ex;
                            while (inner.InnerException != null)
                            {
                                inner = inner.InnerException;
                            }
                            LoggerHelper.Error(String.Format("RPC resp error: method name: {0}, message: {1} {2} {3}", methodName, sb.ToString(), inner.Message, inner.StackTrace));
                        }
                    });
                    EventDispatcher.AddEventListener<object[]>(e.Key, e.Value);
                    m_respMethods.Add(e);
                }
                else
                    LoggerHelper.Warning("Method not found: " + item.Key);
            }
        }

        /// <summary>
        /// 移除订阅Define文件里网络回调函数。
        /// </summary>
        protected void RemoveListener()
        {
            //LoggerHelper.Warning("EventDispatcher.TheRouter.Count: " + EventDispatcher.TheRouter.Count);

            //LoggerHelper.Warning("m_respMethods: " + m_respMethods.Count);
            foreach (var e in m_respMethods)
            {
                EventDispatcher.RemoveEventListener<object[]>(e.Key, e.Value);
            }
            m_respMethods.Clear();
            //LoggerHelper.Warning("EventDispatcher.TheRouter.Count: " + EventDispatcher.TheRouter.Count);
        }

        #endregion

        #region 包装事件监听部分

        // 将 entity id 包含到 eventType 中，生成唯一的 eventType, 
        // 用来进行不同实例的消息
        public string GenUniqMessage(string eventType)
        {
            return String.Concat(eventType, this.ID);
        }

        virtual public void AddUniqEventListener(string eventType, Action handler)
        {
            EventDispatcher.AddEventListener(GenUniqMessage(eventType), handler);
        }

        virtual public void AddUniqEventListener<T>(string eventType, Action<T> handler)
        {
            EventDispatcher.AddEventListener<T>(GenUniqMessage(eventType), handler);
        }

        virtual public void AddUniqEventListener<T, U>(string eventType, Action<T, U> handler)
        {
            EventDispatcher.AddEventListener<T, U>(GenUniqMessage(eventType), handler);
        }

        virtual public void AddUniqEventListener<T, U, V>(string eventType, Action<T, U, V> handler)
        {
            EventDispatcher.AddEventListener<T, U, V>(GenUniqMessage(eventType), handler);
        }

        virtual public void RemoveUniqEventListener(string eventType, Action handler)
        {
            EventDispatcher.RemoveEventListener(GenUniqMessage(eventType), handler);
        }

        virtual public void RemoveUniqEventListener<T>(string eventType, Action<T> handler)
        {
            EventDispatcher.RemoveEventListener<T>(GenUniqMessage(eventType), handler);
        }

        virtual public void RemoveUniqEventListener<T, U>(string eventType, Action<T, U> handler)
        {
            EventDispatcher.RemoveEventListener<T, U>(GenUniqMessage(eventType), handler);
        }

        virtual public void RemoveUniqEventListener<T, U, V>(string eventType, Action<T, U, V> handler)
        {
            EventDispatcher.RemoveEventListener<T, U, V>(GenUniqMessage(eventType), handler);
        }

        virtual public void TriggerUniqEvent(string eventType)
        {
            EventDispatcher.TriggerEvent(GenUniqMessage(eventType));
        }

        virtual public void TriggerUniqEvent<T>(string eventType, T arg1)
        {
            EventDispatcher.TriggerEvent<T>(GenUniqMessage(eventType), arg1);
        }

        virtual public void TriggerUniqEvent<T, U>(string eventType, T arg1, U arg2)
        {
            EventDispatcher.TriggerEvent<T, U>(GenUniqMessage(eventType), arg1, arg2);
        }

        virtual public void TriggerUniqEvent<T, U, V>(string eventType, T arg1, U arg2, V arg3)
        {
            EventDispatcher.TriggerEvent<T, U, V>(GenUniqMessage(eventType), arg1, arg2, arg3);
        }

        #endregion 包装事件监听部分

        #region 包装基于帧的回调函数

        // 基于帧的回调函数。 用于处理必须在异帧 完成的事情。
        public void AddCallbackInFrames(Action callback, int inFrames = 3)
        {
            if (Actor)
                Actor.AddCallbackInFrames(callback, inFrames);
        }

        public void AddCallbackInFrames<U>(Action<U> callback, U arg1, int inFrames = 3)
        {
            if (Actor)
                Actor.AddCallbackInFrames<U>(callback, arg1, inFrames);
        }

        public void AddCallbackInFrames<U, V>(Action<U, V> callback, U arg1, V arg2, int inFrames = 3)
        {
            if (Actor)
                Actor.AddCallbackInFrames<U, V>(callback, arg1, arg2, inFrames);
        }

        public void AddCallbackInFrames<U, V, T>(Action<U, V, T> callback, U arg1, V arg2, T arg3, int inFrames = 3)
        {
            if (Actor)
            {
                if (inFrames == 0)
                    inFrames = 3;
                Actor.AddCallbackInFrames<U, V, T>(callback, arg1, arg2, arg3, inFrames);
            }
        }

        public void AddCallbackInFrames<U, V, T, W>(Action<U, V, T, W> callback, U arg1, V arg2, T arg3, W arg4, int inFrames = 3)
        {
            if (Actor)
            {
                if (inFrames == 0)
                    inFrames = 3;
                Actor.AddCallbackInFrames<U, V, T, W>(callback, arg1, arg2, arg3, arg4, inFrames);
            }
        }

        #endregion

        /// <summary>
        /// buff的客户端表现和buff控制
        /// </summary>
        /// <param name="buffId"></param>
        /// <param name="isAdd"></param>
        /// <param name="time"></param>
        public void HandleBuff(ushort buffId, byte isAdd, uint time)
        {
            if (buffManager == null)
            {
                LoggerHelper.Debug("buffManager == null,obejct:" + Transform.gameObject.name + ",id:" + ID);
                return;
            }
            buffManager.HandleBuff(buffId, isAdd, time);
        }

        public void ClientAddBuff(int id)
        {
            if (buffManager == null)
            {
                return;
            }
            buffManager.ClientAddBuff(id);
        }

        public void ClientDelBuff(int id)
        {
            if (buffManager == null)
            {
                return;
            }
            buffManager.ClientDelBuff(id);
        }

        /// <summary>
        /// Buff(登陆时服务器同步)
        /// </summary>
        public Dictionary<int, UInt32> m_skillBuffClient;
        public LuaTable skillBuffClient
        {
            set
            {
                Mogo.Util.Utils.ParseLuaTable(value, out m_skillBuffClient);
                if (buffManager != null && buffManager.HasGotLoginBuff == false)
                {
                    buffManager.HasGotLoginBuff = true;
                    foreach (KeyValuePair<int, UInt32> pair in m_skillBuffClient)
                    {
                        buffManager.HandleBuff((ushort)pair.Key, 1, pair.Value);
                    }
                }
            }
        }
    }
}
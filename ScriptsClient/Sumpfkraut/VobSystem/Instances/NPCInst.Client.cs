﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Network;
using GUC.Scripts.Sumpfkraut.WorldSystem;
using GUC.Scripts.Sumpfkraut.Visuals;
using GUC.Types;
using Gothic.Objects;
using GUC.Scripts.Sumpfkraut.VobSystem.Definitions;
using GUC.Scripts.Sumpfkraut.Networking;
using GUC.WorldObjects;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Instances
{
    public partial class NPCInst
    {
        public static readonly Networking.Requests.NPCRequestSender Requests = new Networking.Requests.NPCRequestSender();

        public static NPCInst Hero { get { return (NPCInst)NPC.Hero?.ScriptObject; } }

        /*
        basis -> item = iteminstance <- scripts
        basis -> iteminstance = itemdefinition <- scripts
        */

        public void SetMovement(NPCMovement state)
        {
            if (state == this.Movement)
                return;

            BaseInst.SetMovement(state);
        }

        partial void pAfterSpawn()
        {
            if (UseCustoms)
            {
                using (var vec = Gothic.Types.zVec3.Create(CustomScale.X, 1, CustomScale.Z))
                    this.BaseInst.gVob.SetModelScale(vec);

                this.BaseInst.gVob.SetAdditionalVisuals(this.Definition.BodyMesh, (int)CustomBodyTex, 0, CustomHeadMesh.ToString(), (int)CustomHeadTex, 0, -1);
                this.BaseInst.gVob.Voice = (int)CustomVoice;
                this.BaseInst.gVob.SetFatness(CustomFatness);
                this.BaseInst.gVob.Name.Set(CustomName);
            }

            this.BaseInst.ForEachEquippedItem(i => this.pEquipItem(i.Slot, (ItemInst)i.ScriptObject));

            if (this.HP <= 0)
                this.BaseInst.gVob.Name.Clear();
            else
            {
                // because monsters were looking at some weird angle
                this.BaseInst.gAI.SetLookAtTarget(1.0f, 1.0f); // need to change the value or it's not updated
                this.BaseInst.gAI.LookAtTarget(); // update
                this.BaseInst.gAI.StopLookAtTarget(); // change back to default
            }
        }

        #region Equipment

        partial void pEquipItem(int slot, ItemInst item)
        {
            if (!this.IsSpawned)
                return;

            oCNpc gNpc = this.BaseInst.gVob;
            oCItem gItem = item.BaseInst.gVob;

            if (item.BaseInst.IsEquipped)
            {
                pBeginUnequipItem(item);
            }

            Gothic.Types.zString node;
            bool undraw = true;
            switch ((SlotNums)slot)
            {
                case SlotNums.Sword:
                    node = oCNpc.NPCNodes.Sword;
                    break;
                case SlotNums.Longsword:
                    node = oCNpc.NPCNodes.Longsword;
                    break;
                case SlotNums.Bow:
                    node = oCNpc.NPCNodes.Bow;
                    break;
                case SlotNums.XBow:
                    node = oCNpc.NPCNodes.Crossbow;
                    break;
                case SlotNums.Torso:
                    node = oCNpc.NPCNodes.Torso;
                    gItem.VisualChange.Set(item.Definition.VisualChange);
                    break;
                case SlotNums.Righthand:
                    node = oCNpc.NPCNodes.RightHand;
                    if (item.ItemType == ItemTypes.WepXBow && this.ammo != null)
                    {
                        gNpc.PutInSlot(oCNpc.NPCNodes.LeftHand, ammo.BaseInst.gVob, true);
                    }
                    undraw = false;
                    break;
                case SlotNums.Lefthand:
                    node = oCNpc.NPCNodes.LeftHand;
                    if (item.ItemType == ItemTypes.WepBow && this.ammo != null)
                    {
                        gNpc.PutInSlot(oCNpc.NPCNodes.RightHand, ammo.BaseInst.gVob, true);
                    }
                    undraw = false;
                    break;
                default:
                    return;
            }

            gItem.Material = (int)item.Material;
            gNpc.PutInSlot(node, gItem, true);
            PlayDrawItemSound(item, undraw);

            Menus.PlayerInventory.Menu.UpdateEquipment();
        }

        partial void pBeginUnequipItem(ItemInst item)
        {
            if (!this.IsSpawned)
                return;

            oCNpc gNpc = this.BaseInst.gVob;
            oCItem gItem = item.BaseInst.gVob;

            switch ((SlotNums)item.BaseInst.Slot)
            {
                case SlotNums.Sword:
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.Sword, true, true);
                    break;

                case SlotNums.Longsword:
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.Longsword, true, true);
                    break;

                case SlotNums.Bow:
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.Bow, true, true);
                    break;

                case SlotNums.XBow:
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.Crossbow, true, true);
                    break;

                case SlotNums.Torso:
                    gItem.VisualChange.Set(item.Definition.VisualChange);
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.Torso, true, true);
                    break;

                case SlotNums.Righthand:
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.RightHand, true, true);
                    if (item.ItemType == ItemTypes.WepXBow)
                    {
                        gNpc.RemoveFromSlot(oCNpc.NPCNodes.LeftHand, true, true);
                    }
                    break;
                case SlotNums.Lefthand:
                    gNpc.RemoveFromSlot(oCNpc.NPCNodes.LeftHand, true, true);
                    if (item.ItemType == ItemTypes.WepBow)
                    {
                        gNpc.RemoveFromSlot(oCNpc.NPCNodes.RightHand, true, true);
                    }
                    break;

                default:
                    break;
            }
        }

        partial void pAfterUnequipItem(ItemInst item)
        {
            Menus.PlayerInventory.Menu.UpdateEquipment();
        }

        #endregion

        static readonly List<SoundDefinition> hitSounds = new List<SoundDefinition>()
        {
            new SoundDefinition("CS_IAM_ME_FL"),
            new SoundDefinition("CS_IAM_ME_FL_A1"),
            new SoundDefinition("CS_IAM_ME_FL_A2"),
            new SoundDefinition("CS_IAM_ME_FL_A3"),
            new SoundDefinition("CS_IAM_ME_FL_A4")
        };

        static readonly Dictionary<string, SoundDefinition> hitScreams = new Dictionary<string, SoundDefinition>();

        public override void OnReadScriptVobMsg(PacketReader stream)
        {
            var msgID = (ScriptVobMessageIDs)stream.ReadByte();
            switch (msgID)
            {
                case ScriptVobMessageIDs.HitMessage:
                    var targetID = stream.ReadUShort();
                    if (WorldInst.Current.BaseWorld.TryGetVob(targetID, out NPC target))
                    {
                        //this.BaseInst.gAI.CreateHit(target.gVob);
                        int index = Randomizer.GetInt(hitSounds.Count);
                        SoundHandler.PlaySound3D(hitSounds[index], target);

                        index = Randomizer.GetInt(6) - 2;
                        if (index > 0)
                        {
                            string str = string.Format("SVM_{0}_AARGH_{1}.WAV", (int)this.CustomVoice, index);
                            if (!hitScreams.TryGetValue(str, out SoundDefinition scream))
                            {
                                scream = new SoundDefinition(str);
                                hitScreams.Add(str, scream);
                            }
                            SoundHandler.PlaySound3D(scream, target);
                        }

                        if (!target.Model.IsInAnimation())
                            target.gModel.StartAni("T_GOTHIT", 0);

                        // fixme: transmit hit direction and use stumble animation
                    }
                    break;
                case ScriptVobMessageIDs.ParryMessage:
                    targetID = stream.ReadUShort();
                    if (WorldInst.Current.BaseWorld.TryGetVob(targetID, out NPC targetNPC))
                    {
                        this.BaseInst.gAI.StartParadeEffects(targetNPC.gVob);
                    }
                    break;
                case ScriptVobMessageIDs.Climb:
                    var ledge = new NPC.ClimbingLedge(stream);
                    this.BaseInst.SetGClimbingLedge(ledge);
                    break;
                default:
                    break;
            }
        }

        GUC.Utilities.LockTimer collisionFXTimer = new GUC.Utilities.LockTimer(300);
        void DoFightStuff()
        {
            if (!this.IsInFightMode)
                return;

            var aa = this.ModelInst.GetActiveAniFromLayer(2);
            if (aa == null)
                aa = this.ModelInst.GetActiveAniFromLayer(1);
            if (aa == null)
                return;

            var gModel = this.BaseInst.gModel;
            var gAniActive = gModel.GetActiveAni(gModel.GetAniIDFromAniName(aa.AniJob.Name));
            if (gAniActive.Address == 0)
                return;

            var gAni = gAniActive.ModelAni;
            int numEvents = gAni.NumAniEvents;
            for (int index = 0; index < numEvents; index++)
            {
                var aniEvent = gAni.GetAniEvent(index);
                if (aniEvent.AniType != Gothic.Objects.Meshes.zCModelAniEvent.Types.Tag)
                    continue;

                if (aniEvent.TagString.ToString() == "DEF_OPT_FRAME") // it's a attack ani
                {
                    var ai = this.BaseInst.gAI;
                    ai.ShowWeaponTrail();
                    ai.CorrectFightPosition();
                    /*if (this.DrawnWeapon != null && collisionFXTimer.IsReady)
                    {
                        ai.GetFightLimbs();
                        ai.CheckMeleeWeaponHitsLevel(this.DrawnWeapon.BaseInst.gVob);
                    }*/
                    return;
                }
            }
        }

        partial void pSetHealth(int hp, int hpmax)
        {
            if (hp <= 0)
            {
                this.BaseInst.gVob.Name.Clear();
                if (this == NPCInst.Hero)
                {
                    Menus.PlayerInventory.Menu.Close();
                }
            }
        }

        bool isGhost = false;
        public void SetToGhost(bool ghost)
        {
            if (ghost)
            {
                if (isGhost)
                    return;
                this.BaseInst.gVob.BitField1 |= zCVob.BitFlag0.visualAlphaEnabled;
                this.BaseInst.gVob.VisualAlpha = 0.5f;

                if (this.HP <= 0)
                    this.BaseInst.gVob.Name.Clear();

                isGhost = true;
            }
            else
            {
                if (!isGhost)
                    return;

                this.BaseInst.gVob.BitField1 &= ~zCVob.BitFlag0.visualAlphaEnabled;
                this.BaseInst.gVob.Name.Set(this.UseCustoms ? this.CustomName : this.Definition.Name);
                isGhost = false;
            }
        }

        public void OnTick(long now)
        {
            if (this.IsDead)
                return;

            UpdateFightStance();

            DoFightStuff();
            
            var gVob = BaseInst.gVob;
            var gModel = BaseInst.gModel;
            var gAI = BaseInst.gAI;
            if (gModel.IsAnimationActive("T_STAND_2_JUMP") || gModel.IsAnimationActive("S_JUMP")
                || gModel.IsAnimationActive("T_RUNL_2_JUMP"))
            {
                if ((gVob.BitField1 & zCVob.BitFlag0.physicsEnabled) != 0 && gAI.AboveFloor <= 0)
                {
                    if (gAI.Velocity.Y <= 0)
                    {
                        // LAND
                        int id = this.Movement == NPCMovement.Forward ? gAI._t_jump_2_runl : gAI._t_jump_2_stand;
                        gAI.LandAndStartAni(gModel.GetAniFromAniID(id));
                    }
                }
            }

            /*if (this.drawnWeapon != null)
            {
                var gModel = this.BaseInst.gModel;

                int aniID;
                if (this.DrawnWeapon.ItemType == ItemTypes.WepBow)
                {
                    aniID = gModel.GetAniIDFromAniName("S_BOWAIM");
                }
                else if (this.DrawnWeapon.ItemType == ItemTypes.WepXBow)
                {
                    aniID = gModel.GetAniIDFromAniName("S_CBOWAIM");
                }
                else
                {
                    return;
                }

                var aa = gModel.GetActiveAni(aniID);

                if (this.isAiming)
                {
                    if (aa.Address == 0)
                        gModel.StartAni(aniID, 0);
                }
                else
                {
                    if (aa.Address != 0)
                        gModel.StopAni(aa);
                }
            }*/
        }

        public void StartAnimation(Animations.Animation ani, object[] netArgs)
        {
            /*ScriptAni a = (ScriptAni)ani.ScriptObject;

            if (a.AniJob.IsJump)
            {
                this.StartAniJump(a, (int)netArgs[0], (int)netArgs[1]);
            }
            else if (a.AniJob.IsClimb)
            {
                this.StartAniClimb(a, (WorldObjects.NPC.ClimbingLedge)netArgs[0]);
            }
            else if (a.AniJob.IsDraw)
            {
                int itemID = (int)netArgs[0];
                WorldObjects.Item item;
                if (this.BaseInst.Inventory.TryGetItem(itemID, out item) && item.IsEquipped)
                {
                    this.StartAniDraw(a, (ItemInst)item.ScriptObject);
                }
            }
            else if (a.AniJob.IsUndraw)
            {
                int itemID = (int)netArgs[0];
                WorldObjects.Item item;
                if (this.BaseInst.Inventory.TryGetItem(itemID, out item) && item.IsEquipped)
                {
                    this.StartAniUndraw(a, (ItemInst)item.ScriptObject);
                }
            }*/

        }

        public void StartAniJump(ScriptAni ani, int fwdVelocity, int upVelocity)
        {
            /*this.StartAnimation(ani);

            var ai = this.BaseInst.HumanAI;
            ai.AniCtrlBitfield &= ~(1 << 3);
            //this.BaseInst.gVob.SetBodyState(8);

            var vel = new Gothic.Types.zVec3(ai.Address + 0x90);
            var dir = this.BaseInst.GetDirection();

            vel.X = dir.X * fwdVelocity;
            vel.Z = dir.Z * fwdVelocity;
            vel.Y = upVelocity;

            this.BaseInst.SetPhysics(true);

            this.BaseInst.SetVelocity((Vec3f)vel);*/
        }

        public void StartAniClimb(ScriptAni ani, WorldObjects.NPC.ClimbingLedge ledge)
        {
            /*this.BaseInst.SetGClimbingLedge(ledge);
            this.StartAnimation(ani);*/
        }

        static SoundDefinition sfx_DrawGeneric = new SoundDefinition("wurschtel.wav");

        static SoundDefinition sfx_DrawMetal = new SoundDefinition("Drawsound_ME.wav");
        static SoundDefinition sfx_DrawWood = new SoundDefinition("Drawsound_WO.wav");

        static SoundDefinition sfx_UndrawMetal = new SoundDefinition("Undrawsound_ME.wav");
        static SoundDefinition sfx_UndrawWood = new SoundDefinition("Undrawsound_WO.wav");

        void PlayDrawItemSound(ItemInst item, bool undraw)
        {
            SoundDefinition sound;
            switch (item.Definition.Material)
            {
                case ItemMaterials.Metal:
                    sound = undraw ? sfx_UndrawMetal : sfx_DrawMetal;
                    break;
                case ItemMaterials.Wood:
                    sound = undraw ? sfx_UndrawWood : sfx_DrawWood;
                    break;
                default:
                    sound = sfx_DrawGeneric;
                    break;
            }

            SoundHandler.PlaySound3D(sound, this.BaseInst);
        }

        public void StartAniDraw(ScriptAni ani, ItemInst item)
        {
            /*this.StartAnimation(ani);
            if (item.IsWepMelee)
            {
                drawTimer.SetInterval(ani.DrawTime);
                drawTimer.SetCallback(() =>
                {
                    drawTimer.Stop();
                    if (this.BaseInst.IsDead)
                        return;

                    if (item.Definition.Material == ItemMaterials.Metal)
                    {
                        SoundHandler.PlaySound3D(sfx_DrawMetal, this.BaseInst);
                    }
                    else if (item.Definition.Material == ItemMaterials.Wood)
                    {
                        SoundHandler.PlaySound3D(sfx_DrawWood, this.BaseInst);
                    }
                    else
                    {
                        SoundHandler.PlaySound3D(sfx_DrawGeneric, this.BaseInst);
                    }
                });
                drawTimer.Start();
            }*/
        }

        public void StartAniUndraw(ScriptAni ani, ItemInst item)
        {
            /*this.StartAnimation(ani);
            if (item.IsWepMelee)
            {
                drawTimer.SetInterval(ani.DrawTime);
                drawTimer.SetCallback(() =>
                {
                    drawTimer.Stop();
                    if (this.BaseInst.IsDead)
                        return;

                    if (item.Definition.Material == ItemMaterials.Metal)
                    {
                        SoundHandler.PlaySound3D(sfx_UndrawMetal, this.BaseInst);
                    }
                    else if (item.Definition.Material == ItemMaterials.Wood)
                    {
                        SoundHandler.PlaySound3D(sfx_UndrawWood, this.BaseInst);
                    }
                    else
                    {
                        SoundHandler.PlaySound3D(sfx_DrawGeneric, this.BaseInst);
                    }
                });
                drawTimer.Start();
            }*/
        }

        int fmode = 0;
        void UpdateFightStance()
        {
            int fmode;
            if (this.BaseInst.IsInFightMode)
            {
                if (this.drawnWeapon == null)
                {
                    fmode = 1; // fists
                }
                else
                {
                    if (this.drawnWeapon.ItemType == ItemTypes.Wep1H)
                        fmode = 3;
                    else if (this.drawnWeapon.ItemType == ItemTypes.Wep2H)
                        fmode = 4;
                    else if (this.drawnWeapon.ItemType == ItemTypes.WepBow)
                        fmode = 5;
                    else if (this.drawnWeapon.ItemType == ItemTypes.WepXBow)
                        fmode = 6;
                    else
                        fmode = 0;
                }
            }
            else
            {
                fmode = 0;
            }

            if (this.fmode != fmode)
            {
                var gNpc = this.BaseInst.gVob;
                var ai = gNpc.HumanAI;

                // check before changing animations, cause gothic only checks the current animation set
                bool running = ai.IsRunning();
                bool standing = ai.IsStanding();

                // set fight mode & animations in gothic
                gNpc.FMode = fmode;
                ai.SetFightAnis(fmode);
                ai.SetWalkMode(0);

                // override active animations from the old animation set
                var gModel = BaseInst.gModel;
                if (running)
                {
                    gModel.StartAni(ai._s_walkl, 0);
                }
                else if (standing)
                {
                    gModel.StartAni(ai._s_walk, 0);
                }

                // sets focus and camera modes
                if (this == ScriptClient.Client.Character)
                {
                    gNpc.SetWeaponMode(fmode);
                }

                this.fmode = fmode;
            }
        }

        public BaseVobInst GetFocusVob()
        {
            var vob = this.BaseInst.GetFocusVob();
            return vob != null ? (BaseVobInst)vob.ScriptObject : null;
        }
    }
}

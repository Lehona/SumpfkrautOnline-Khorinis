﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Network;
using GUC.Scripts.Sumpfkraut.WorldSystem;
using GUC.Utilities;
using GUC.Types;
using Gothic.Objects;
using GUC.Scripts.Sumpfkraut.VobSystem.Enumeration;
using GUC.Scripts.Sumpfkraut.VobSystem.Definitions;
using GUC.Scripts.Sumpfkraut.Networking;
using GUC.WorldObjects;
using GUC.Scripts.Sumpfkraut.VobSystem.Instances.Mobs;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Instances
{
    public partial class NPCInst
    {
        public static readonly Networking.Requests.NPCRequestSender Requests = new Networking.Requests.NPCRequestSender();

        public static NPCInst Hero { get { return (NPCInst)GUCNPCInst.Hero?.ScriptObject; } }

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

            doDrawItemSound = false;
            this.BaseInst.ForEachEquippedItem(i => this.pAfterEquip((NPCSlots)i.Slot, (ItemInst)i.ScriptObject));
            doDrawItemSound = true;

            if (this.HP > 0)
            {
                // because monsters were looking at some weird angle
                this.BaseInst.gAI.SetLookAtTarget(1.0f, 1.0f); // need to change the value or it's not updated
                this.BaseInst.gAI.LookAtTarget(); // update
                this.BaseInst.gAI.StopLookAtTarget(); // change back to default
            }
        }

        #region Equipment

        partial void pBeforeEquip(NPCSlots slot, ItemInst item)
        {
            if (item.IsEquipped)
                pBeforeUnequip(item);
        }

        partial void pAfterEquip(NPCSlots slot, ItemInst item)
        {
            if (!this.IsSpawned)
                return;

            oCNpc gNpc = this.BaseInst.gVob;
            oCItem gItem = item.BaseInst.gVob;

            Gothic.Types.zString node;
            bool undraw = true;
            bool ininv = true;
            switch (slot)
            {
                case NPCSlots.OneHanded1:
                    node = oCNpc.NPCNodes.Sword;
                    break;
                case NPCSlots.TwoHanded:
                    node = oCNpc.NPCNodes.Longsword;
                    break;
                case NPCSlots.Ranged:
                    node = item.ItemType == ItemTypes.WepBow ? oCNpc.NPCNodes.Bow : oCNpc.NPCNodes.Crossbow;
                    break;
                case NPCSlots.Armor:
                    node = oCNpc.NPCNodes.Torso;
                    gItem.VisualChange.Set(item.Definition.VisualChange);
                    break;
                case NPCSlots.RightHand:
                    node = oCNpc.NPCNodes.RightHand;
                    undraw = false;
                    break;
                case NPCSlots.LeftHand:
                    node = oCNpc.NPCNodes.LeftHand;
                    undraw = false;
                    break;
                default:
                    return;
            }

            if (item.ModelDef.Visual.EndsWith(".ZEN"))
                ininv = false;

            gItem.Material = (int)item.Material;
            gNpc.PutInSlot(node, gItem, ininv);
            PlayDrawItemSound(item, undraw);
        }

        partial void pBeforeUnequip(ItemInst item)
        {
            if (!this.IsSpawned)
                return;

            oCNpc gNpc = this.BaseInst.gVob;
            oCItem gItem = item.BaseInst.gVob;

            Gothic.Types.zString node;
            switch ((NPCSlots)item.BaseInst.Slot)
            {
                case NPCSlots.OneHanded1:
                    node = oCNpc.NPCNodes.Sword;
                    break;
                case NPCSlots.TwoHanded:
                    node = oCNpc.NPCNodes.Longsword;
                    break;
                case NPCSlots.Ranged:
                    node = item.ItemType == ItemTypes.WepBow ? oCNpc.NPCNodes.Bow : oCNpc.NPCNodes.Crossbow;
                    break;
                case NPCSlots.Armor:
                    node = oCNpc.NPCNodes.Torso;
                    break;
                case NPCSlots.RightHand:
                    node = oCNpc.NPCNodes.RightHand;
                    break;
                case NPCSlots.LeftHand:
                    node = oCNpc.NPCNodes.LeftHand;
                    break;
                default:
                    return;
            }

            gNpc.RemoveFromSlot(node, false, true);
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

        static readonly Dictionary<string, SoundDefinition> cachedVoices = new Dictionary<string, SoundDefinition>(100);

        LockTimer screamTimer = new LockTimer(1000);
        public override void OnReadScriptVobMsg(PacketReader stream)
        {
            var msgID = (ScriptVobMessageIDs)stream.ReadByte();
            switch (msgID)
            {
                case ScriptVobMessageIDs.HitMessage:
                    var attackerID = stream.ReadUShort();
                    if (WorldInst.Current.BaseWorld.TryGetVob(attackerID, out GUCNPCInst attacker))
                    {
                        int index = Randomizer.GetInt(hitSounds.Count);
                        SoundHandler.PlaySound3D(hitSounds[index], this.BaseInst);

                        if (this.CustomVoice > 0 && screamTimer.IsReady)
                        {
                            index = Randomizer.GetInt(6) - 2;
                            if (index > 0)
                            {
                                string str = string.Format("SVM_{0}_AARGH_{1}.WAV", (int)this.CustomVoice, index);
                                if (!cachedVoices.TryGetValue(str, out SoundDefinition scream))
                                {
                                    scream = new SoundDefinition(str);
                                    cachedVoices.Add(str, scream);
                                }
                                SoundHandler.PlaySound3D(scream, this.BaseInst, 3000, 0.8f);
                            }
                        }

                        if (!this.ModelInst.IsInAnimation())
                            this.BaseInst.gModel.StartAni("T_GOTHIT", 0);

                        // fixme: transmit hit direction and use stumble animation
                    }
                    break;
                case ScriptVobMessageIDs.ParryMessage:
                    attackerID = stream.ReadUShort();
                    if (WorldInst.Current.BaseWorld.TryGetVob(attackerID, out GUCNPCInst targetNPC))
                    {
                        this.BaseInst.gAI.StartParadeEffects(targetNPC.gVob);
                    }
                    break;
                case ScriptVobMessageIDs.Climb:
                    var ledge = new GUCNPCInst.ClimbingLedge(stream);
                    this.BaseInst.SetGClimbingLedge(ledge);
                    break;
                case ScriptVobMessageIDs.Uncon:
                    SetUnconsciousness((Unconsciousness)stream.ReadByte());
                    break;
                case ScriptVobMessageIDs.Voice:
                    DoVoice((VoiceCmd)stream.ReadByte());
                    break;
                case ScriptVobMessageIDs.VoiceShout:
                    DoVoice((VoiceCmd)stream.ReadByte(), 6000, 0.8f);
                    break;
                case ScriptVobMessageIDs.StartUsingMob:
                    if (WorldInst.Current.TryGetVob(stream.ReadUShort(), out MobInst mobInst))
                    {
                        mobInst.StartUsing(Hero);
                        UsedMob = mobInst;
                    }
                    break;
                case ScriptVobMessageIDs.StopUsingMob:
                    if (Hero.UsedMob != null)
                    {
                        UsedMob.StopUsing(Hero);
                        Hero.UsedMob = null;
                    }
                    break;

                default:
                    break;
            }
        }

        public void DoVoice(VoiceCmd cmd, float range = 3000, float volume = 0.7f)
        {
            if (this.CustomVoice <= 0)
                return;

            string str = string.Format("SVM_{0}_{1}.WAV", (int)this.CustomVoice, cmd);
            if (!cachedVoices.TryGetValue(str, out SoundDefinition scream))
            {
                scream = new SoundDefinition(str);
                cachedVoices.Add(str, scream);
            }
            SoundHandler.PlaySound3D(scream, this.BaseInst, range, volume);
        }

        public void SetUnconsciousness(Unconsciousness unconsciousness)
        {
            if (this.uncon != Unconsciousness.None && unconsciousness == Unconsciousness.None)
            {
                int voice = (int)this.CustomVoice;
                if (voice > 0 && voice < 20)
                {
                    string str = string.Format("SVM_{0}_OHMYHEAD.WAV", voice);
                    if (!cachedVoices.TryGetValue(str, out SoundDefinition scream))
                    {
                        scream = new SoundDefinition(str);
                        cachedVoices.Add(str, scream);
                    }
                    SoundHandler.PlaySound3D(scream, this.BaseInst, 3000, 0.7f);
                }
            }
            this.uncon = unconsciousness;
            if (this == Hero)
                oCNpcFocus.SetFocusMode(IsUnconscious ? 0 : 1);
        }


        LockTimer collisionFXTimer = new LockTimer(100);
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

                    /*var wep = GetDrawnWeapon();
                    if (wep != null && collisionFXTimer.IsReady)
                    {
                        ai.GetFightLimbs();
                        ai.CheckMeleeWeaponHitsLevel(wep.BaseInst.gVob);
                    }*/
                return;
                }
            }
        }

        partial void pSetHealth(int hp, int hpmax)
        {
            if (hp <= 0)
            {
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
            Update2nd1H();

            if (this.IsDead)
                return;

            UpdateFightStance();

            DoFightStuff();

            var gVob = BaseInst.gVob;
            var gModel = BaseInst.gModel;
            var gAI = BaseInst.gAI;
            if ((gVob.BitField1 & zCVob.BitFlag0.physicsEnabled) != 0 && gAI.AboveFloor <= 0 && gAI.Velocity.Y <= 0)
            {
                if (gModel.IsAniActive(gAI._t_jump_2_stand)
                 || gModel.IsAniActive(gAI._s_jump)
                 || gModel.IsAnimationActive("T_RUNL_2_JUMP"))
                {
                    // LAND
                    int id = this.Movement == NPCMovement.Forward ? gAI._t_jump_2_runl : gAI._t_jump_2_stand;
                    gAI.LandAndStartAni(gModel.GetAniFromAniID(id));
                }
            }
        }

        static SoundDefinition sfx_DrawGeneric = new SoundDefinition("wurschtel.wav");

        static SoundDefinition sfx_DrawMetal = new SoundDefinition("Drawsound_ME.wav");
        static SoundDefinition sfx_DrawWood = new SoundDefinition("Drawsound_WO.wav");

        static SoundDefinition sfx_UndrawMetal = new SoundDefinition("Undrawsound_ME.wav");
        static SoundDefinition sfx_UndrawWood = new SoundDefinition("Undrawsound_WO.wav");

        static bool doDrawItemSound = true; //improveme
        void PlayDrawItemSound(ItemInst item, bool undraw)
        {
            if (!doDrawItemSound) return;

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

        int fmode = -1;
        void UpdateFightStance()
        {
            int fmode;
            if (this.BaseInst.IsInFightMode)
            {
                ItemInst wep = GetDrawnWeapon();

                if (wep == null)
                {
                    fmode = 1; // fists
                }
                else
                {
                    if (wep.ItemType == ItemTypes.Wep1H)
                        fmode = 3;
                    else if (wep.ItemType == ItemTypes.Wep2H)
                        fmode = 4;
                    else if (wep.ItemType == ItemTypes.WepBow)
                        fmode = 5;
                    else if (wep.ItemType == ItemTypes.WepXBow)
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
                    if (fmode == 0)
                        oCNpcFocus.SetFocusMode(0);
                }

                this.fmode = fmode;
            }
        }
        
        zCVob secondMeleeVob;
        void Update2nd1H()
        {
            if (!IsSpawned)
                return;

            ItemInst weapon;
            if ((weapon = GetEquipmentBySlot(NPCSlots.OneHanded2)) == null)
            {
                if (secondMeleeVob != null)
                    secondMeleeVob.BitField1 &= ~zCVob.BitFlag0.showVisual;
                return;
            }

            if (secondMeleeVob == null)
            {
                secondMeleeVob = zCVob.Create();
                GothicGlobals.Game.GetWorld().AddVob(secondMeleeVob);
            }
            secondMeleeVob.BitField1 |= zCVob.BitFlag0.showVisual;
            secondMeleeVob.SetVisual(weapon.ModelDef.Visual);

            BaseInst.gVob.GetTrafoModelNodeToWorld(oCNpc.NPCNodes.Sword, secondMeleeVob.TrafoObjToWorld);
            var pos = (Vec3f)secondMeleeVob.Position;
            secondMeleeVob.SetPositionWorld(pos.X, pos.Y, pos.Z);
            secondMeleeVob.TrafoObjToWorld.PostRotateY(15);
            secondMeleeVob.TrafoObjToWorld.PostRotateZ(5);
        }

        partial void pDespawn()
        {
            if (secondMeleeVob != null)
                GothicGlobals.Game.GetWorld().RemoveVob(secondMeleeVob);
        }
    }
}

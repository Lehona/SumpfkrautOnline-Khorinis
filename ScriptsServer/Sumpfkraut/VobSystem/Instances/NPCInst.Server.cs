﻿using GUC.Log;
using GUC.Scripting;
using GUC.Scripts.Sumpfkraut.Networking;
using GUC.Scripts.Sumpfkraut.Visuals;
using GUC.Scripts.Sumpfkraut.Visuals.AniCatalogs;
using GUC.Scripts.Sumpfkraut.VobSystem.Definitions;
using GUC.Types;
using System;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Instances
{
    public partial class NPCInst
    {
        const int MaxNPCCorpses = 500000;

        public bool AllowHit(NPCInst target)
        {
            if (!target.IsPlayer)
            {
                return this.IsPlayer;
            }
            else if (this.IsPlayer) // pvp
            {
                if (this.TeamID == -1)
                {
                    return target.TeamID == -1 && ((Arena.ArenaClient)this.Client).DuelEnemy == target.Client;
                }
                else
                {
                    return this.TeamID != target.TeamID;
                }
            }

            return true;
        }


        public static readonly Networking.Requests.NPCRequestReceiver Requests = new Networking.Requests.NPCRequestReceiver();

        public delegate void NPCInstMoveHandler(NPCInst npc, Vec3f oldPos, Angles oldAng, NPCMovement oldMovement);
        public static event NPCInstMoveHandler sOnNPCInstMove;
        /// <summary>
        /// Not invoked at the moment.
        /// </summary>
        public event NPCInstMoveHandler OnNPCInstMove;
        static NPCInst()
        {
            WorldObjects.NPC.OnNPCMove += (npc, p, d, m) => sOnNPCInstMove((NPCInst)npc.ScriptObject, p, d, m);
            sOnNPCInstMove += (npc, p, d, m) => npc.ChangePosDir(p, d, m);
        }

        Vec3f lastRegPos;
        void ChangePosDir(Vec3f oldPos, Angles oldAng, NPCMovement oldMovement)
        {
            Vec3f pos = GetPosition();
            if (lastRegPos.GetDistance(pos) > 30.0f)
            {
                lastHitMoveTime = GameTime.Ticks;
                lastRegPos = pos;
            }

            if (this.FightAnimation != null && this.CanCombo && this.Movement != NPCMovement.Stand)
            { // so the npc can instantly stop the attack and run into a direction
                this.ModelInst.StopAnimation(this.fightAni, false);
            }

            if (this.TeamID != -1 && this.Client != null)
            {
                if (pos.GetDistancePlanar(Vec3f.Null) > Arena.TeamMode.ActiveTODef.MaxWorldDistance
                    || pos.Y > Arena.TeamMode.ActiveTODef.MaxHeight
                    || pos.Y < Arena.TeamMode.ActiveTODef.MaxDepth)
                {
                    ((Arena.ArenaClient)this.Client).KillCharacter();
                }
            }

            var env = this.Environment;
            if (env.WaterLevel > 0 && env.WaterDepth > 0.3f)
                if (this.IsPlayer) ((Arena.ArenaClient)this.Client).KillCharacter();
                else this.SetHealth(0);

            CheckUnconsciousness();
        }

        void CheckUnconsciousness()
        {
            if (!this.IsUnconscious || this.Environment.InAir)
                return;

            var cat = AniCatalog.Unconscious;
            var dropJob = uncon == Unconsciousness.Front ? cat.DropFront : cat.DropBack;
            if (dropJob == null) return;

            var aa = this.ModelInst.GetActiveAniFromLayer(1);
            if (aa != null)
            {
                var job = (ScriptAniJob)aa.AniJob.ScriptObject;
                if (job == dropJob || job == dropJob.NextAni)
                    return;

                var standJob = uncon == Unconsciousness.Front ? cat.StandUpFront : cat.StandUpBack;
                if (standJob != null && standJob == job)
                    return;
            }

            this.ModelInst.StartAniJob(dropJob);
        }

        #region Constructors

        public NPCInst(NPCDef def) : this()
        {
            this.Definition = def;
        }

        partial void pConstruct()
        {
        }

        #endregion

        public NPCCatalog AniCatalog { get { return (NPCCatalog)this.ModelDef?.Catalog; } }

        public bool IsPlayer { get { return this.BaseInst.IsPlayer; } }

        public ScriptClient Client { get { return (ScriptClient)this.BaseInst.Client?.ScriptObject; } }

        #region Jumps

        /// <summary>
        /// Starts an uncontrolled jump animation, throws the npc with velocity.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="velocity"></param>
        public void DoJump(JumpMoves move, Vec3f velocity)
        {
            ScriptAniJob job;
            switch (move)
            {
                case JumpMoves.Fwd:
                    job = AniCatalog.Jumps.Fwd;
                    break;
                case JumpMoves.Run:
                    job = AniCatalog.Jumps.Run;
                    break;
                case JumpMoves.Up:
                    job = AniCatalog.Jumps.Up;
                    break;
                default:
                    Logger.Log("Not existing jump move: " + move);
                    return;
            }

            if (job == null)
                return;

            this.ModelInst.StartAniJobUncontrolled(job);
            this.Throw(velocity);
        }

        #endregion

        #region Climbing


        public void DoClimb(ClimbMoves move, WorldObjects.NPC.ClimbingLedge ledge)
        {
            ScriptAniJob job;
            switch (move)
            {
                case ClimbMoves.High:
                    job = AniCatalog.Climbs.High;
                    break;
                case ClimbMoves.Mid:
                    job = AniCatalog.Climbs.Mid;
                    break;
                case ClimbMoves.Low:
                    job = AniCatalog.Climbs.Low;
                    break;
                default:
                    Logger.Log("Not existing climb move: " + move);
                    return;
            }

            if (job == null)
                return;


            var stream = this.BaseInst.GetScriptVobStream();
            stream.Write((byte)ScriptVobMessageIDs.Climb);
            ledge.WriteStream(stream);
            this.BaseInst.SendScriptVobStream(stream);

            this.ModelInst.StartAniJob(job);
        }

        #endregion

        #region Drop & Take

        /// <summary>
        /// Starts a drop animation and drops any item in front of the npc
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        public void DropItem(ItemInst item, int amount, float offset = 50)
        {
            if (item == null)
                return;

            //if (item.Container != this)
            //    return;

            item = item.Split(amount);

            Vec3f spawnPos = this.GetPosition();
            //Angles spawnAng = this.GetAngles();
            //spawnPos += spawnDir * offset;

            // fixme: drop item at the item drop frame
            ModelInst.StartAniJob(this.AniCatalog.ItemHandling.DropItem, () => item.Spawn(this.World, spawnPos, Angles.Null));
        }

        public void UseItem(byte itemID)
        {
            ItemInst item = Inventory.GetItem(itemID);
            if (item == null)
                return;

            if (this.ModelInst.BaseInst.IsInAnimation())
                return;

            if (this.Environment.InAir)
                return;

            if (this.Movement != NPCMovement.Stand)
                return;

            switch (item.ItemType)
            {
                case ItemTypes.SmallEatable:
                    // TODO: eat item zu bestimmtem frame aufrufen
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.EatSmall, () => { this.UnequipItem(item); this.EatItem(item); });
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
                case ItemTypes.LargeEatable:
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.EatLarge, () => this.UnequipItem(item));
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
                case ItemTypes.Mutton:
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.EatMutton, () => this.UnequipItem(item));
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
                case ItemTypes.Rice:
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.EatRice, () => this.UnequipItem(item));
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
                case ItemTypes.Drinkable:
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.DrinkPotion, () => this.UnequipItem(item));
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
                case ItemTypes.Readable:
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.ReadScroll, () => this.UnequipItem(item));
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
                case ItemTypes.Torch:
                    this.ModelInst.StartAniJob(AniCatalog.ItemHandling.UseTorch, () => this.UnequipItem(item));
                    this.EquipItem(NPCSlots.LeftHand, item);
                    break;
            }
        }

        public void EatItem(ItemInst item)
        {

        }
        #endregion

        #region Fight Moves

        Animations.ActiveAni fightAni;
        public Animations.ActiveAni FightAnimation { get { return this.fightAni; } }

        int comboNum;
        public int ComboNum { get { return comboNum; } }

        bool canCombo = true;
        public bool CanCombo { get { return canCombo; } }

        FightMoves currentFightMove = FightMoves.None;
        public FightMoves CurrentFightMove { get { return this.currentFightMove; } }

        public void DoFightMove(FightMoves move, int combo = 0)
        {
            NPCCatalog.FightAnis fightCatalog;
            var drawnWeapon = GetDrawnWeapon();
            if (drawnWeapon == null)
            {
                fightCatalog = AniCatalog.FightFist;
            }
            else
            {
                switch (drawnWeapon.ItemType)
                {
                    default:
                    case ItemTypes.Wep1H:
                        fightCatalog = AniCatalog.Fight1H;
                        break;
                    case ItemTypes.Wep2H:
                        fightCatalog = AniCatalog.Fight2H;
                        break;
                }
            }

            switch (move)
            {
                case FightMoves.Fwd:
                    DoAttack(fightCatalog.Fwd[combo], move, combo);
                    break;
                case FightMoves.Run:
                    DoAttack(fightCatalog.Run, move);
                    break;
                case FightMoves.Left:
                    DoAttack(fightCatalog.Left, move);
                    break;
                case FightMoves.Right:
                    DoAttack(fightCatalog.Right, move);
                    break;
                case FightMoves.Parry:
                    DoParry(fightCatalog.GetRandomParry());
                    break;
                case FightMoves.Dodge:
                    DoDodge(fightCatalog.Dodge);
                    break;
            }
        }

        void DoAttack(ScriptAniJob job, FightMoves move, int fwdCombo = 0)
        {
            if (job == null)
                return;

            if (!ModelInst.TryGetAniFromJob(job, out ScriptAni ani))
                return;

            // combo window
            if (!ani.TryGetSpecialFrame(SpecialFrame.Combo, out float comboFrame))
                comboFrame = ani.EndFrame;

            var comboPair = new Animations.FrameActionPair(comboFrame, () => OpenCombo());

            // hit frame
            float hitFrame;
            if (!ani.TryGetSpecialFrame(SpecialFrame.Hit, out hitFrame))
                hitFrame = comboFrame;

            if (hitFrame > comboFrame)
                hitFrame = comboFrame;

            var hitPair = new Animations.FrameActionPair(hitFrame, () => this.CalcHit());

            // end of animation
            var endPair = Animations.FrameActionPair.OnEnd(() => EndFightAni());

            // start ani first, because the OnEnd-Callback from the former attack resets the fight stance
            this.fightAni = this.ModelInst.StartAniJob(job, comboPair, hitPair, endPair);
            this.currentFightMove = move;
            this.canCombo = false;
            this.comboNum = fwdCombo;
        }

        void OpenCombo()
        {
            if (this.Movement != NPCMovement.Stand && this.fightAni != null)
            { // so the npc can instantly stop the attack and run into a direction
                this.ModelInst.StopAnimation(this.fightAni, false);
            }
            else
            {
                canCombo = true;
            }
        }

        void EndFightAni()
        {
            this.currentFightMove = FightMoves.None;
            this.canCombo = true;
            this.comboNum = 0;
            this.fightAni = null;
        }

        void DoParry(ScriptAniJob job)
        {
            if (job == null)
                return;

            ScriptAni ani;
            if (!ModelInst.TryGetAniFromJob(job, out ani))
                return;

            // end of animation
            var endPair = Animations.FrameActionPair.OnEnd(() => EndFightAni());

            this.fightAni = this.ModelInst.StartAniJob(job, endPair);
            this.currentFightMove = FightMoves.Parry;
            this.canCombo = false;
            this.comboNum = 0;
        }

        void DoDodge(ScriptAniJob job)
        {
            if (job == null)
                return;

            ScriptAni ani;
            if (!ModelInst.TryGetAniFromJob(job, out ani))
                return;

            // end of animation
            var endPair = Animations.FrameActionPair.OnEnd(() => EndFightAni());

            this.fightAni = this.ModelInst.StartAniJob(job, endPair);
            this.currentFightMove = FightMoves.Dodge;
            this.canCombo = false;
            this.comboNum = 0;
        }

        #endregion

        #region Weapon Drawing

        public void DoDrawWeapon(ItemInst item)
        {
            NPCCatalog.DrawWeaponAnis catalog;
            switch (item.ItemType)
            {
                default:
                case ItemTypes.Wep1H:
                    catalog = AniCatalog.Draw1H;
                    break;
                case ItemTypes.Wep2H:
                    catalog = AniCatalog.Draw2H;
                    break;
                case ItemTypes.WepBow:
                    catalog = AniCatalog.DrawBow;
                    break;
                case ItemTypes.WepXBow:
                    catalog = AniCatalog.DrawXBow;
                    break;
                case ItemTypes.Rune:
                case ItemTypes.Scroll:
                    catalog = AniCatalog.DrawMagic;
                    break;
            }

            if (this.IsInFightMode || GetDrawnWeapon() != null)
            {
                /*if (this.DrawnWeapon != null)
                {
                    ItemInst weapon = this.DrawnWeapon;
                    catalog = GetDrawWeaponCatalog(weapon.ItemType);
                    // weapon draw while running or when standing
                    if (this.Movement == NPCMovement.Forward || this.Movement == NPCMovement.Left || this.Movement == NPCMovement.Right)
                    {
                        this.ModelInst.StartAniJob(catalog.UndrawWhileRunning, 0.1f, () =>
                        {
                            this.UnequipItem(weapon); // take weapon from hand
                            this.EquipItem(weapon); // place weapon into parking slot
                        });
                    }
                    else
                    {
                        this.ModelInst.StartAniJob(catalog.Undraw, 0.1f, () =>
                        {
                            this.UnequipItem(weapon); // take weapon from hand
                            this.EquipItem(weapon); // place weapon into parking slot
                        });
                    }
                }
                this.SetFightMode(false);*/
            }
            else
            {
                ScriptAniJob job = (this.Movement == NPCMovement.Stand && !this.Environment.InAir) ? catalog.Draw : catalog.DrawWhileRunning;
                if (job == null || !this.ModelInst.TryGetAniFromJob(job, out ScriptAni ani)) // no animation
                {
                    DrawWeapon(item);
                }
                else
                {
                    if (!ani.TryGetSpecialFrame(SpecialFrame.Draw, out float drawFrame))
                        drawFrame = 0;

                    this.ModelInst.StartAniJob(job, new Animations.FrameActionPair(drawFrame, () => DrawWeapon(item)));
                }
            }
        }

        /// <summary>
        /// Instantly puts the item in the hand and activates fight mode
        /// </summary>
        void DrawWeapon(ItemInst item)
        {
            if (item.ItemType == ItemTypes.WepBow)
            {
                this.EquipItem(NPCSlots.LeftHand, item);
                var ammo = GetAmmo();
                if (ammo != null)
                    this.EquipItem(NPCSlots.RightHand, ammo);
            }
            else if (item.ItemType == ItemTypes.WepXBow)
            {
                this.EquipItem(NPCSlots.RightHand, item);
                var ammo = GetAmmo();
                if (ammo != null)
                    this.EquipItem(NPCSlots.LeftHand, ammo);
            }
            else
            {
                this.EquipItem(NPCSlots.RightHand, item); // put weapon into hand
                var melee2 = GetEquipmentBySlot(NPCSlots.OneHanded2);
                if (melee2 != null)
                    this.EquipItem(NPCSlots.LeftHand, melee2);
            }
            this.SetFightMode(true); // look angry
        }

        public void DoUndrawWeapon(ItemInst item)
        {
            NPCCatalog.DrawWeaponAnis catalog;
            switch (item.ItemType)
            {
                default:
                case ItemTypes.Wep1H:
                    catalog = AniCatalog.Draw1H;
                    break;
                case ItemTypes.Wep2H:
                    catalog = AniCatalog.Draw2H;
                    break;
                case ItemTypes.WepBow:
                    catalog = AniCatalog.DrawBow;
                    break;
                case ItemTypes.WepXBow:
                    catalog = AniCatalog.DrawXBow;
                    break;
                case ItemTypes.Rune:
                case ItemTypes.Scroll:
                    catalog = AniCatalog.DrawMagic;
                    break;
            }

            if (this.GetDrawnWeapon() == null)
            {
            }
            else
            {
                ScriptAniJob job = (this.Movement == NPCMovement.Stand && !this.Environment.InAir) ? catalog.Undraw : catalog.UndrawWhileRunning;
                if (job == null || !this.ModelInst.TryGetAniFromJob(job, out ScriptAni ani)) // no animation
                {
                    UndrawWeapon(item);
                }
                else
                {
                    if (!ani.TryGetSpecialFrame(SpecialFrame.Draw, out float drawFrame))
                        drawFrame = 0;

                    this.ModelInst.StartAniJob(job, new Animations.FrameActionPair(drawFrame, () => UndrawWeapon(item)));
                }
            }
        }

        /// <summary>
        /// Instantly puts the item back and deactivates fight mode
        /// </summary>
        void UndrawWeapon(ItemInst item)
        {
            switch (item.ItemType)
            {
                case ItemTypes.Wep1H:
                    EquipItem(NPCSlots.OneHanded1, item);
                    var other1H = GetLeftHand();
                    if (other1H != null && other1H.ItemType == ItemTypes.Wep1H)
                        EquipItem(NPCSlots.OneHanded2, other1H);
                    break;
                case ItemTypes.Wep2H:
                    EquipItem(NPCSlots.TwoHanded, item);
                    break;
                case ItemTypes.WepBow:
                    EquipItem(NPCSlots.Ranged, item);
                    var ammo = GetRightHand();
                    if (ammo != null && ammo.ItemType == ItemTypes.AmmoBow)
                        EquipItem(NPCSlots.Ammo, ammo);
                    break;
                case ItemTypes.WepXBow:
                    EquipItem(NPCSlots.Ranged, item);
                    ammo = GetLeftHand();
                    if (ammo != null && ammo.ItemType == ItemTypes.AmmoXBow)
                        EquipItem(NPCSlots.Ammo, ammo);
                    break;
            }

            this.SetFightMode(false);
        }

        /// <summary>
        /// Plays the draw animation and activates fight mode
        /// </summary>
        public void DoDrawFists()
        {
            ScriptAniJob drawAniJob;
            if (this.Movement == NPCMovement.Stand && !this.Environment.InAir && this.Environment.WaterLevel < 0.4f)
            {
                drawAniJob = AniCatalog.DrawFists.Draw;
            }
            else
            {
                drawAniJob = AniCatalog.DrawFists.DrawWhileRunning;
            }

            if (drawAniJob == null)
                return;

            if (!this.ModelInst.TryGetAniFromJob(drawAniJob, out ScriptAni ani))
                return;

            if (!ani.TryGetSpecialFrame(0, out float drawFrame))
                drawFrame = float.MaxValue;

            this.ModelInst.StartAniJob(drawAniJob, new Animations.FrameActionPair(drawFrame, () => this.SetFightMode(true)));
        }

        public void DoUndrawFists()
        {
            ScriptAniJob undrawAniJob;
            if (this.Movement == NPCMovement.Stand && !this.Environment.InAir && this.Environment.WaterLevel < 0.4f)
            {
                undrawAniJob = AniCatalog.DrawFists.Undraw;
            }
            else
            {
                undrawAniJob = AniCatalog.DrawFists.UndrawWhileRunning;
            }

            if (undrawAniJob == null)
                return;

            if (!this.ModelInst.TryGetAniFromJob(undrawAniJob, out ScriptAni ani))
                return;

            if (!ani.TryGetSpecialFrame(0, out float undrawFrame))
                undrawFrame = float.MaxValue;

            this.ModelInst.StartAniJob(undrawAniJob, new Animations.FrameActionPair(undrawFrame, () => this.SetFightMode(false)));
        }

        #endregion

        #region NPC Information

        public NPCCatalog.DrawWeaponAnis GetDrawWeaponCatalog(ItemTypes itemType)
        {
            switch (itemType)
            {
                case ItemTypes.Wep1H:
                    return AniCatalog.Draw1H;
                case ItemTypes.Wep2H:
                    return AniCatalog.Draw2H;
                case ItemTypes.WepBow:
                    return AniCatalog.DrawBow;
                case ItemTypes.WepXBow:
                    return AniCatalog.DrawXBow;
                case ItemTypes.Rune:
                    return AniCatalog.DrawMagic;
                case ItemTypes.Scroll:
                    return AniCatalog.DrawMagic;
                default:
                    return AniCatalog.Draw1H;
            }
        }
        #endregion

        static DespawnList<NPCInst> npcDespawnList = new DespawnList<NPCInst>(MaxNPCCorpses);

        partial void pSetHealth(int hp, int hpmax)
        {
            if (hp <= 0)
            {
                npcDespawnList.AddVob(this);
            }
        }


        #region Hit Detection

        long lastHitMoveTime;
        public long LastHitMove { get { return this.lastHitMoveTime; } }

        public void Hit(NPCInst attacker, int damage, bool fromFront = true)
        {
            var strm = this.BaseInst.GetScriptVobStream();
            strm.Write((byte)ScriptVobMessageIDs.HitMessage);
            strm.Write((ushort)this.ID);
            this.BaseInst.SendScriptVobStream(strm);

            if (Cast.Try(attacker.Client, out Arena.ArenaClient att) && attacker.TeamID != -1 && att.TOClass != null)
                damage += att.TOClass.Damage;

            int protection = 0;
            var armor = this.GetArmor();
            if (armor != null)
                protection += armor.Protection;

            // two weapons
            ItemInst otherMelee;
            if ((otherMelee = this.GetLeftHand()) != null && otherMelee.ItemType == ItemTypes.Wep1H)
                protection -= otherMelee.Damage / 4;

            if ((otherMelee = attacker.GetLeftHand()) != null && otherMelee.ItemType == ItemTypes.Wep1H)
                damage += otherMelee.Damage / 4;

            if (Cast.Try(this.Client, out Arena.ArenaClient tar) && this.TeamID != -1 && tar.TOClass != null)
                protection += tar.TOClass.Protection;

            damage -= protection;

            // ARENA
            if (this.TeamID != -1 && attacker.TeamID == this.TeamID) // same team
                damage /= 2;

            if (damage <= 0)
                damage = 1;

            int resultingHP = this.GetHealth() - damage;

            if (resultingHP <= 0 && !attacker.IsPlayer && tar != null && tar.HordeClass != null)
            {
                resultingHP = 1;
                this.DropUnconscious(!fromFront);
            }

            this.SetHealth(resultingHP);
            sOnHit?.Invoke(attacker, this, damage);
            OnHit?.Invoke(attacker, this, damage);
            lastHitMoveTime = GameTime.Ticks;
        }

        public float GetFightRange()
        {
            ItemInst drawnWeapon = GetDrawnWeapon();
            return this.ModelDef.Radius + (drawnWeapon == null ? ModelDef.FistRange : drawnWeapon.Definition.Range);
        }

        public delegate void OnHitHandler(NPCInst attacker, NPCInst target, int damage);
        public static event OnHitHandler sOnHit;
        public event OnHitHandler OnHit;

        void CalcHit()
        {
            try
            {
                if (this.IsDead || this.FightAnimation == null || this.IsUnconscious)
                    return;

                Vec3f attPos = this.GetPosition();
                Angles attAng = this.GetAngles();

                ItemInst drawnWeapon = GetDrawnWeapon();
                int baseDamage = drawnWeapon == null ? 10 : drawnWeapon.Damage;
                float weaponRange = GetFightRange();
                this.BaseInst.World.ForEachNPCRough(attPos, GUCScripts.BiggestNPCRadius + weaponRange, npc => // fixme: enemy model radius
                  {
                      NPCInst target = (NPCInst)npc.ScriptObject;
                      if (target == this || target.IsDead || target.IsUnconscious)
                          return;

                      if (!AllowHit(target))
                          return;

                      float realRange = weaponRange + target.ModelDef.Radius;
                      if (target.CurrentFightMove == FightMoves.Dodge)
                          realRange /= 3.0f; // decrease radius if target is backing up

                      Vec3f targetPos = npc.Position;
                      if (target.ModelDef.Visual == "CRAWLER.MDS") // fixme
                      {
                          targetPos += npc.GetAtVector() * 50;
                      }
                      if ((targetPos - attPos).GetLength() > realRange)
                          return; // not in range


                      float hitHeight;
                      float hitYaw;
                      if (CurrentFightMove == FightMoves.Left || CurrentFightMove == FightMoves.Right)
                      {
                          hitHeight = target.ModelDef.HalfHeight;
                          hitYaw = Angles.PI * 0.4f;
                      }
                      else
                      {
                          hitHeight = target.ModelDef.HalfHeight + this.ModelDef.HalfHeight;
                          hitYaw = Angles.PI * 0.2f;
                      }

                      if (Math.Abs(targetPos.Y - attPos.Y) > hitHeight)
                          return; // not same height

                      float yaw = Angles.GetYawFromAtVector(targetPos - attPos);
                      if (Math.Abs(Angles.Difference(yaw, attAng.Yaw)) > hitYaw)
                          return; // target is not in front of attacker

                      float tdiff = Math.Abs(Angles.Difference(target.GetAngles().Yaw, yaw));
                      if (target.CurrentFightMove == FightMoves.Parry && tdiff > Angles.PI / 2) // parry 180 degrees
                      {
                          var strm = this.BaseInst.GetScriptVobStream();
                          strm.Write((byte)ScriptVobMessageIDs.ParryMessage);
                          strm.Write((ushort)npc.ID);
                          this.BaseInst.SendScriptVobStream(strm);
                      }
                      else // HIT
                      {
                          int damage = baseDamage;
                          if (CurrentFightMove == FightMoves.Left || CurrentFightMove == FightMoves.Right)
                          {
                              damage -= 2;
                          }
                          else if (CurrentFightMove == FightMoves.Fwd)
                          {
                              damage += (comboNum - 1) * 2;
                          }
                          else if (CurrentFightMove == FightMoves.Run)
                          {
                              damage += 6;
                              if (Environment.InAir) // super jump attack
                              {
                                  damage += 2; // not too much because you can always jump
                              }
                              if (target.Environment.InAir)
                              {
                                  damage += 2;
                              }
                          }

                          bool frontAttack;
                          if (tdiff < Angles.PI / 4) // backstab
                          {
                              damage += 4;
                              frontAttack = false;
                          }
                          else
                          {
                              frontAttack = true;
                          }

                          target.Hit(this, damage, frontAttack);
                      }
                  });
            }
            catch (Exception e)
            {
                Logger.Log("CalcHit of npc " + this.ID + " " + this.BaseInst.HP + " " + e);
            }
        }

        #endregion

        partial void pBeforeSpawn()
        {
            if (this.ModelDef.Visual != "HUMANS.MDS" && this.ModelDef.Visual != "ORC.MDS")
                this.SetFightMode(true);
        }

        #region Ranged Fighting

        public bool IsAiming()
        {
            var drawnWeapon = GetDrawnWeapon();
            if (drawnWeapon != null && drawnWeapon.IsWepRanged)
            {
                var aa = ModelInst.GetActiveAniFromLayer(1);
                if (aa != null)
                {
                    var scriptJob = aa.AniJob.ScriptObject;
                    return scriptJob == AniCatalog.FightBow.Aiming || scriptJob == AniCatalog.FightXBow.Aiming;
                }
            }
            return false;
        }

        public void DoAim()
        {
            var drawnWeapon = GetDrawnWeapon();
            ScriptAniJob job = (drawnWeapon != null && drawnWeapon.ItemType == ItemTypes.WepXBow) ? AniCatalog.FightXBow.Aim : AniCatalog.FightBow.Aim;
            if (job == null || !ModelInst.TryGetAniFromJob(job, out ScriptAni ani))
            {
                return;
            }
            else
            {
                this.ModelInst.StartAniJob(job);
            }
        }

        public void DoUnaim()
        {
            var drawnWeapon = GetDrawnWeapon();
            ScriptAniJob job = (drawnWeapon != null && drawnWeapon.ItemType == ItemTypes.WepXBow) ? AniCatalog.FightXBow.Unaim : AniCatalog.FightBow.Unaim;
            if (job == null || !ModelInst.TryGetAniFromJob(job, out ScriptAni ani))
            {
                return;
            }
            else
            {
                this.ModelInst.StartAniJob(job);
            }
        }

        public void DoShoot(Vec3f start, Vec3f end, ProjInst proj)
        {
            proj.Destination = end;
            proj.Shooter = this;

            Angles rotation = Angles.FromAtVector(end - start);
            var p = rotation.Pitch; // rotate arrow correctly
            rotation.Pitch = rotation.Roll;
            rotation.Yaw -= Angles.PI / 2;
            rotation.Roll = p;

            proj.Spawn(this.World, start, rotation);


            var drawnWeapon = GetDrawnWeapon();
            ScriptAniJob job = (drawnWeapon != null && drawnWeapon.ItemType == ItemTypes.WepXBow) ? AniCatalog.FightXBow.Reload : AniCatalog.FightBow.Reload;
            if (job != null && ModelInst.TryGetAniFromJob(job, out ScriptAni ani))
            {
                this.ModelInst.StartAniJob(job);
            }
        }

        #endregion

        public void DropUnconscious(bool toFront = true)
        {
            var cat = AniCatalog.Unconscious;
            ScriptAniJob job = toFront ? cat.DropFront : cat.DropBack;
            if (job != null)
                this.ModelInst.StartAniJob(job);

            uncon = toFront ? Unconsciousness.Front : Unconsciousness.Back;

            var strm = this.BaseInst.GetScriptVobStream();
            strm.Write((byte)ScriptVobMessageIDs.Uncon);
            strm.Write((byte)uncon);
            this.BaseInst.SendScriptVobStream(strm);
        }

        public void LiftUnconsciousness()
        {
            if (!IsUnconscious)
                return;

            var cat = AniCatalog.Unconscious;
            ScriptAniJob job = uncon == Unconsciousness.Front ? cat.StandUpFront : cat.StandUpBack;
            if (job != null)
                this.ModelInst.StartAniJob(job, DoLiftUncon);
            else
                DoLiftUncon();
        }

        void DoLiftUncon()
        {
            uncon = Unconsciousness.None;
            var strm = this.BaseInst.GetScriptVobStream();
            strm.Write((byte)ScriptVobMessageIDs.Uncon);
            strm.Write((byte)uncon);
            this.BaseInst.SendScriptVobStream(strm);
        }
    }
}

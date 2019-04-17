﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Types;
using GUC.Network;
using GUC.WorldObjects.Definitions;
using GUC.Scripts.Sumpfkraut.EffectSystem.EffectHandlers;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Definitions
{
    public enum ItemMaterials : byte
    {
        Wood = 0,
        Stone = 1,
        Metal = 2,
        Leather = 3,
        Clay = 4,
        Glass = 5
    }

    // create an inherited class for each type ?
    public enum ItemTypes : byte
    {
        Misc,

        // Weapons

        // melee
        Wep1H,
        Wep2H,

        // ranged
        WepBow,
        WepXBow,

        MAXWEAPON,

        Armor,
        AmmoBow,
        AmmoXBow,
        Drinkable,
        SmallEatable,
        LargeEatable,
        Rice,
        Mutton,
        Readable,
        Torch,
        Joint,
        Slutable,

        Rune,
        Scroll,
    }

    public partial class ItemDef : NamedVobDef, GUCItemDef.IScriptItemInstance
    {
        #region Properties

        public override VobType VobType { get { return VobType.Item; } }

        new public ItemDefEffectHandler EffectHandler { get { return (ItemDefEffectHandler)base.EffectHandler; } }

        new public GUCItemDef BaseDef { get { return (GUCItemDef)base.BaseDef; } }

        /// <summary>The material of this item. Controls the dropping sound.</summary>
        public ItemMaterials Material = ItemMaterials.Wood;

        string effect = "";
        /// <summary>Magic effect when laying in the world. (case insensitive) See _work/Data/Scripts/System/VisualFX/VisualFxInst.d</summary>
        public string Effect
        {
            get { return this.effect; }
            set { this.effect = value == null ? "" : value.ToUpperInvariant(); }
        }

        string visualChange = "";
        /// <summary>For Armors</summary>
        public string VisualChange
        {
            get { return this.visualChange; }
            set { this.visualChange = value == null ? "" : value.ToUpperInvariant(); }
        }

        public ItemTypes ItemType = ItemTypes.Misc;

        public bool IsAmmo { get { return this.ItemType >= ItemTypes.AmmoBow && this.ItemType <= ItemTypes.AmmoXBow; } }
        public bool IsWeapon { get { return this.ItemType >= ItemTypes.Wep1H && this.ItemType <= ItemTypes.WepXBow; } }
        public bool IsWepRanged { get { return this.ItemType >= ItemTypes.WepBow && this.ItemType <= ItemTypes.WepXBow; } }
        public bool IsWepMelee { get { return this.ItemType >= ItemTypes.Wep1H && this.ItemType <= ItemTypes.Wep2H; } }

        public float Range = 0;
        public int Damage = 0;
        public int Protection = 0;

        public Vec3f InvOffset;
        public Angles InvRotation;

        #endregion

        #region Constructors

        partial void pConstruct();
        public ItemDef()
        {
            pConstruct();
        }

        protected override BaseEffectHandler CreateHandler()
        {
            return new ItemDefEffectHandler(null, null, this);
        }

        protected override GUCBaseVobDef CreateVobInstance()
        {
            return new GUCItemDef(this);
        }

        #endregion

        #region Read & Write

        public override void OnWriteProperties(PacketWriter stream)
        {
            base.OnWriteProperties(stream);
            stream.Write((byte)this.ItemType);
            stream.Write(this.name);
            stream.Write(this.visualChange);
            stream.Write((byte)this.Material);

            if (Range != 0)
            {
                stream.Write(true);
                stream.Write((ushort)Range);
            }
            else
            {
                stream.Write(false);
            }

            if (Damage != 0)
            {
                stream.Write(true);
                stream.Write((ushort)Damage);
            }
            else
            {
                stream.Write(false);
            }

            if (stream.Write(Protection != 0))
            {
                stream.Write((ushort)Protection);
            }

            if (stream.Write(!InvOffset.IsExactNull()))
            {
                stream.Write(InvOffset);
            }
            if (stream.Write(!InvRotation.IsExactNull()))
            {
                stream.WriteCompressedAngles(InvRotation);
            }
        }

        public override void OnReadProperties(PacketReader stream)
        {
            base.OnReadProperties(stream);
            this.ItemType = (ItemTypes)stream.ReadByte();
            this.name = stream.ReadString();
            this.visualChange = stream.ReadString();
            this.Material = (ItemMaterials)stream.ReadByte();

            if (stream.ReadBit())
                this.Range = stream.ReadUShort();
            if (stream.ReadBit())
                this.Damage = stream.ReadUShort();
            if (stream.ReadBit())
                this.Protection = stream.ReadUShort();
            if (stream.ReadBit())
                this.InvOffset = stream.ReadVec3f();
            if (stream.ReadBit())
                this.InvRotation = stream.ReadCompressedAngles();
        }

        #endregion

        public static void ForEach(Action<ItemDef> action)
        {
            GUCBaseVobDef.ForEachOfType(GUCVobTypes.Item, i => action((ItemDef)i.ScriptObject));
        }
    }
}
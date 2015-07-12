﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Server.WorldObjects;

namespace GUC.Server.Scripts.Items.Amulet
{
    /// <summary>
    /// Diese Klasse ist abstrakt, das heißt, sie wird nur für
    /// andere Food-Instanzen zum ableiten genutzt.
    /// </summary>
    public abstract class AbstractAmulet : ItemInstance
    {
        protected AbstractAmulet() : base()
        {
            Name = "Amulett";
            MainFlags = Enumeration.MainFlags.ITEM_KAT_MAGIC;
            Flags = Enumeration.Flags.ITEM_AMULET;

            Materials = Enumeration.MaterialType.MAT_METAL;
            Effect = "SPELLFX_ITEMGLIMMER";
            Wear = Enumeration.ArmorFlags.WEAR_EFFECT;

            Weight = 1;
        }
    }
}

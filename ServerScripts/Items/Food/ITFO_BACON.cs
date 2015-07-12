﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Server.Scripting.Objects;
using GUC.Server.Scripting.Objects.Character;

namespace GUC.Server.Scripts.Items.Food
{
    public class ITFO_BACON : AbstractFood
    {
        static ITFO_BACON ii;
        public static ITFO_BACON get()
        {
            if (ii == null)
                ii = new ITFO_BACON();
            return ii;
        }


        protected ITFO_BACON()
            : base("ITFO_BACON")
        {
            Name = "Schinken";
            Visual = "ItFo_Bacon.3ds";
            Description = Name;
            ScemeName = "FOODHUGE";

            
            OnUse += new Scripting.Events.UseItemEventHandler(useItem);

            CreateItemInstance();
        }

        protected void useItem(NPC npc, Item item, short state, short targetState)
        {
            if (!(state == -1 && targetState == 0))
                return;

            npc.HP += 20;
        }
    }
}

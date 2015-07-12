﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Server.Scripting.Objects;
using GUC.Server.Scripting.Objects.Character;

namespace GUC.Server.Scripts.Items.Belts
{
    public class ITBE_ADDON_PROT_FIRE : AbstractBelts
    {
        static ITBE_ADDON_PROT_FIRE ii;
        public static ITBE_ADDON_PROT_FIRE get()
        {
            if (ii == null)
                ii = new ITBE_ADDON_PROT_FIRE();
            return ii;
        }


        protected ITBE_ADDON_PROT_FIRE()
            : base("ITBE_ADDON_PROT_FIRE")
        {
            Description = "Gürtel des Feuerläufers";
            Visual = "ItMi_Belt_02.3ds";


            OnEquip += new Scripting.Events.NPCEquipEventHandler(equip);
            OnUnEquip += new Scripting.Events.NPCEquipEventHandler(unequip);

            CreateItemInstance();
        }

        protected void equip(NPC npc, Item item)
        {
            npc.ProtectionFire += 10;
        }

        protected void unequip(NPC npc, Item item)
        {
            npc.ProtectionFire -= 10;
        }
    }
}

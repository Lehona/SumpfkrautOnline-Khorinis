﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUC.Enumeration
{
    public enum ItemType : byte
    {
        Sword_1H, // one-handed swordlike weapons
        Sword_2H, // two-handed swordlike weapons
        Blunt_1H, // one-handed blunt weapons
        Blunt_2H, // two-handed blunt weapons
        Bow,   // bows
        XBow,  // crossbows
        Armor, // armor & clothing

        Ring,   // rings
        Amulet, // amulets
        Belt,   // belts

        Arrow, // ammunition
        XBolt, // ammunition

        Food_Huge,  // food in two hands (f.e. Bread)
        Food_Small, // food in one hand (f.e. Apples)
        Drink,      // drinks
        Potions,    // potions

        Document, // notes
        Book,     // books
        Rune,     // magic runes
        Scroll,   // magic scrolls 

        Misc_Usable, // interactive items (f.e. brooms, lutes)

        Misc // everything else
    }

    public enum ItemMaterial : byte
    {
        Wood = 0,
        Stone = 1,
        Metal = 2,
        Leather = 3,
        Clay = 4,
        Glass = 5
    }

    public enum Gender : byte
    {
        Masculine,
        Feminine,
        Neuter
    }
}

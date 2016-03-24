﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Server.WorldObjects;
using GUC.Types;
using GUC.Enumeration;

namespace GUC.Server.Scripts
{
    static class CmdHandler
    {
        public static void Init()
        {
            NPC.CmdOnMove += OnMovement;
            NPC.CmdOnTargetMove += OnTargetMovement;
            NPC.CmdOnUseMob += OnUseMob;
            NPC.CmdOnUnUseMob += OnUnUseMob;
            NPC.CmdOnUseItem += OnUseItem;
            NPC.CmdOnJump += OnJump;
            NPC.CmdOnDrawEquipment += OnDrawEquipment;
            NPC.CmdOnUndrawItem += OnUndrawItem;
        }

        static void OnDrawEquipment(NPC npc, Item item)
        {
            if (npc.UsedMob == null)
            {
                npc.DoDrawitem(item);
            }
        }

        static void OnUndrawItem(NPC npc, Item item)
        {
            npc.DoUndrawItem();
        }

        static void OnMovement(NPC npc, NPCState state)
        {
            switch (state)
            {
                case NPCState.Stand:
                    break;
                case NPCState.MoveForward:
                case NPCState.MoveBackward:
                    break;
            }

            npc.DoMoveState(state);
        }

        static void OnTargetMovement(NPC npc, NPC target, NPCState state)
        {
            switch (state)
            {
                case NPCState.MoveLeft:
                case NPCState.MoveRight:
                    //Strafing
                    break;

                case NPCState.AttackForward:
                    break;
                case NPCState.AttackLeft:
                    break;
                case NPCState.AttackRight:
                    break;
                case NPCState.AttackRun:
                    break;

                case NPCState.Parry:
                    break;
                case NPCState.DodgeBack:
                    break;
            }

            npc.DoMoveState(state, target);
        }

        static void OnJump(NPC npc)
        {
            npc.DoJump();
        }

        static void OnUseMob(MobInter mob, NPC npc)
        {
            if (npc.State == NPCState.Stand && npc.DrawnItem == null)
            {
                npc.DoUseMob(mob);
            }
        }

        static void OnUnUseMob(MobInter mob, NPC npc)
        {
            npc.DoUnUseMob();
        }

        static void OnUseItem(Item item, NPC npc)
        {
            if (npc.State == NPCState.Stand && npc.DrawnItem == null)
            {
                switch (item.Type)
                {
                    case ItemType.Sword_1H:
                    case ItemType.Sword_2H:
                    case ItemType.Blunt_1H:
                    case ItemType.Blunt_2H:
                        npc.EquipSlot(1, item);
                        break;
                    case ItemType.Bow:
                    case ItemType.XBow:
                        npc.EquipSlot(2, item);
                        break;
                    case ItemType.Armor:
                        npc.EquipSlot(10, item);
                        break;
                    case ItemType.Ring:
                        break;
                    case ItemType.Amulet:
                        break;
                    case ItemType.Belt:
                        break;
                    case ItemType.Food_Huge:
                    case ItemType.Food_Small:
                    case ItemType.Drink:
                    case ItemType.Potions:
                        break;
                    case ItemType.Document:
                    case ItemType.Book:
                        break;
                    case ItemType.Rune:
                    case ItemType.Scroll:
                        break;
                    case ItemType.Misc_Usable:
                        break;
                    case ItemType.Misc:
                        break;
                }
            }
        }
    }
}

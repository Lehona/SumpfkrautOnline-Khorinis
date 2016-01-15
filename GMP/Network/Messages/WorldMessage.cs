﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Client.WorldObjects;
using RakNet;
using GUC.Network;
using GUC.Types;
using Gothic.zClasses;
using GUC.Enumeration;

namespace GUC.Client.Network.Messages
{
    static class WorldMessage
    {

        public static void ReadVobSpawn(BitStream stream)
        {
            uint ID = stream.mReadUInt();
            ushort instID = stream.mReadUShort();
            Vec3f pos = stream.mReadVec();
            Vec3f dir = stream.mReadVec();
            bool drop = stream.mReadBool();

            MobInstance instance = MobInstance.Table.Get(instID);
            if (instance != null)
            {
                Vob vob = null;
                switch (instance.type)
                {
                    case MobType.Vob:
                        vob = new Vob(ID, instID);
                        break;
                    case MobType.Mob:
                        vob = new Mob(ID, instID);
                        break;
                    case MobType.MobInter:
                        vob = new MobInter(ID, instID);
                        break;
                    case MobType.MobFire:
                        vob = new MobFire(ID, instID);
                        break;
                    case MobType.MobLadder:
                        vob = new MobLadder(ID, instID);
                        break;
                    case MobType.MobSwitch:
                        vob = new MobSwitch(ID, instID);
                        break;
                    case MobType.MobWheel:
                        vob = new MobWheel(ID, instID);
                        break;
                    case MobType.MobContainer:
                        vob = new Mob(ID, instID);
                        break;
                    case MobType.MobDoor:
                        vob = new MobDoor(ID, instID);
                        break;
                }

                if (vob != null)
                {
                    vob.Spawn(pos, dir, drop);
                }
            }
        }

        public static void ReadVobDelete(BitStream stream)
        {
            uint id = stream.mReadUInt();
            AbstractVob vob = World.GetVobByID(id);
            if (vob != null)
            {
                vob.Despawn();
            }
        }

        public static void ReadNPCSpawn(BitStream stream)
        {
            uint ID = stream.mReadUInt();
            ushort instID = stream.mReadUShort();
            
            NPC npc;
            if (ID == Player.ID)
            {
                Player.Hero = new NPC(ID, instID, oCNpc.Player(Program.Process));
                npc = Player.Hero;
            }
            else
            {
                npc = new NPC(ID, instID);
            }
            
            npc.Position = stream.mReadVec();
            npc.Direction = stream.mReadVec();

            if (instID <= 2)
            {
                byte BodyTex = stream.mReadByte();
                byte HeadMesh = stream.mReadByte();
                byte HeadTex = stream.mReadByte();
                npc.Voice = stream.mReadByte();

                npc.SetBodyVisuals(BodyTex, ((HumHeadMesh)HeadMesh).ToString(), HeadTex);
            }

            npc.BodyHeight = (float)stream.mReadByte() / 100.0f;
            npc.BodyWidth = (float)stream.mReadByte() / 100.0f;
            npc.Fatness = (float)stream.mReadShort() / 100.0f;
            
            string customName = stream.mReadString();
            if (customName.Length > 0)
            {
                npc.Name = customName;
            }

            npc.HPMax = stream.mReadUShort();
            npc.HP = stream.mReadUShort();

            int slotCount = stream.mReadByte();
            byte slot; uint itemID; ushort itemInstanceID; ushort itemCondition; Item item;
            for (int i = 0; i < slotCount; i++)
            {
                slot = stream.mReadByte();
                itemID = stream.mReadUInt();
                itemInstanceID = stream.mReadUShort();
                itemCondition = stream.mReadUShort();

                item = new Item(itemID, itemInstanceID);
                item.Condition = itemCondition;
                npc.EquipSlot(slot, item);
            }

            if (stream.ReadBit())
            {
                npc.DrawnItem = NPCMessage.ReadStrmDrawItem(stream, npc);
            }

            npc.Spawn();
        }

        public static void ReadItemSpawn(BitStream stream)
        {
            uint ID = stream.mReadUInt();
            ushort instID = stream.mReadUShort();
            Item item = new Item(ID, instID);

            Vec3f pos = stream.mReadVec();
            Vec3f dir = stream.mReadVec();
            item.Amount = stream.mReadUShort();
            if (item.instance.type <= ItemType.Armor)
                item.Condition = stream.mReadUShort();
            bool drop = stream.ReadBit();
            item.Spawn(pos, dir, drop);
        }

        public static void ReadTimeChange (BitStream stream)
        {
            int day = stream.mReadInt();
            int hour = stream.mReadInt();
            int minute = stream.mReadInt();

            oCGame.Game(Program.Process).WorldTimer.SetTime(hour, minute);
            oCGame.Game(Program.Process).WorldTimer.SetDay(day);
        }

        public static void ReadWeatherChange (BitStream stream)
        {
            int weatherType = (int)stream.mReadByte();
            //int startDay = (int)stream.mReadByte();
            int startHour = (int)stream.mReadByte();
            int startMinute = (int)stream.mReadByte();
            //int endDay = (int)stream.mReadByte();
            int endHour = (int)stream.mReadByte();
            int endMinute = (int)stream.mReadByte();

            // if start- and endTime remain the same (done when there is no precipitation)...
            // they stop possible current rain/snow by overwriting the raintime with
            // something in the past, effectively ending the rain/snow (hopefully...)
            float startTime = 0f;
            float endTime = 0f;

            if (weatherType > 0)
            {
                // 12:00 == 0f; 24:00 == 1f
                startTime = (((((float)startHour + 12f) % 24f) * 60f) + ((float)startMinute)) / (24f * 60f);
                endTime = (((((float)endHour + 12f) % 24f) * 60f) + ((float)endMinute)) / (24f * 60f);
            }

            oCGame.Game(Program.Process).World.SkyControlerOutdoor.SetWeatherType(weatherType);
            oCGame.Game(Program.Process).World.SkyControlerOutdoor.StartRainTime = startTime;
            oCGame.Game(Program.Process).World.SkyControlerOutdoor.EndRainTime = endTime;
        }

    }

}

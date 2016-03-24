﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using GUC.Client.WorldObjects;
using GUC.Enumeration;
using GUC.Network;
using GUC.Types;
using Gothic.zTypes;
using Gothic.zStruct;
using Gothic.zClasses;

namespace GUC.Client.Network.Messages
{
    static class VobMessage
    {
        public static void ReadPosDir(BitStream stream)
        {
            uint id = stream.mReadUInt();

            AbstractVob vob = World.GetVobByID(id);
            if (vob != null)
            {
                Vec3f pos = stream.mReadVec();
                Vec3f dir = stream.mReadVec();

                vob.Position = pos;

                if (vob is NPC)
                {
                    NPC npc = (NPC)vob;
                    if (npc.State == NPCState.Stand)
                    {
                        Vec3f curDir = npc.turning ? npc.nextDir : npc.Direction;

                        float x = dir.X - curDir.X;
                        float z = dir.Z - curDir.Z;

                        if (x*x + z*z > 0.01f)
                        {
                            npc.StartTurnAni(npc.Direction.Z * dir.X - dir.Z * npc.Direction.X > 0);
                            npc.lastDir = npc.Direction;
                            npc.nextDir = dir;
                            npc.turning = true;
                            npc.lastDirTime = DateTime.UtcNow.Ticks;
                            return;
                        }
                        npc.StopTurnAnis();
                    }
                }

                vob.Direction = dir;
            }
        }



        public static void WritePosDir()
        {
            BitStream stream = Program.client.SetupSendStream(NetworkID.VobPosDirMessage);

            stream.mWrite(Player.Hero.Position);
            stream.mWrite(Player.Hero.Direction);

            AbstractVob vob;
            stream.mWrite(Player.VobControlledList.Count);
            for (int i = 0; i < Player.VobControlledList.Count; i++)
            {
                vob = Player.VobControlledList[i];
                stream.mWrite(vob.ID);
                stream.mWrite(vob.Position);
                stream.mWrite(vob.Direction);
            }
            Program.client.SendStream(stream, PacketPriority.LOW_PRIORITY, PacketReliability.UNRELIABLE);
        }
    }
}

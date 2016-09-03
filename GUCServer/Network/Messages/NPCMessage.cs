﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using GUC.Enumeration;
using GUC.WorldObjects;
using GUC.Animations;

namespace GUC.Network.Messages
{
    static class NPCMessage
    {
        #region States

        public static void ReadMoveState(PacketReader stream, GameClient client, NPC character, World world)
        {
            int id = stream.ReadUShort();
            NPC npc;
            if (world.TryGetVob(id, out npc))
            {
                MoveState state = (MoveState)stream.ReadByte();
                if (npc == character || (client.GuidedVobs.Contains(id) && state <= MoveState.Forward)) //is it a controlled NPC?
                {
                    if (npc.ScriptObject != null)
                        npc.ScriptObject.OnCmdMove(state);
                }
            }
        }

        public static void WriteMoveState(NPC npc, MoveState state)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCStateMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)state);

            npc.ForEachVisibleClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        #endregion

        #region Animation

        #region Overlays

        public static void ReadApplyOverlay(PacketReader stream, NPC npc)
        {
            Overlay ov;
            if (npc.Model.TryGetOverlay(stream.ReadByte(), out ov))
            {
                npc.ScriptObject.ApplyOverlay(ov);
            }
        }

        public static void ReadRemoveOverlay(PacketReader stream, NPC npc)
        {
            Overlay ov;
            if (npc.Model.TryGetOverlay(stream.ReadByte(), out ov))
            {
                npc.ScriptObject.RemoveOverlay(ov);
            }
        }

        public static void WriteApplyOverlayMessage(NPC npc, Overlay overlay)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCApplyOverlayMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)overlay.ID);

            npc.ForEachVisibleClient(c => c.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE, 'W'));
        }

        public static void WriteRemoveOverlayMessage(NPC npc, Overlay overlay)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCRemoveOverlayMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)overlay.ID);

            npc.ForEachVisibleClient(c => c.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE, 'W'));
        }

        #endregion

        public static void ReadAniStart(PacketReader stream, NPC character)
        {
            AniJob job;
            if (character.Model.TryGetAni(stream.ReadUShort(), out job))
            {
                Animation ani;
                if (character.TryGetAniFromJob(job, out ani))
                    character.ScriptObject.OnCmdAniStart(ani);
            }
        }

        public static void ReadAniStartWithArgs(PacketReader stream, NPC character)
        {
            AniJob job;
            if (character.Model.TryGetAni(stream.ReadUShort(), out job))
            {
                Animation ani;
                if (character.TryGetAniFromJob(job, out ani))
                {
                    object[] netArgs;
                    character.ScriptObject.OnReadAniStartArgs(stream, job, out netArgs);
                    character.ScriptObject.OnCmdAniStart(ani, netArgs);
                }
            }
        }

        public static void ReadAniStop(PacketReader stream, NPC character)
        {
            character.ScriptObject.OnCmdAniStop(stream.ReadBit());
        }


        public static void WriteAniStart(NPC npc, Animation ani)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCAniStartMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((ushort)ani.AniJob.ID);

            npc.ForEachVisibleClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        public static void WriteAniStart(NPC npc, Animation ani, object[] netArgs)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCAniStartWithArgsMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((ushort)ani.AniJob.ID);

            npc.ScriptObject.OnWriteAniStartArgs(stream, ani.AniJob, netArgs);

            npc.ForEachVisibleClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        public static void WriteAniStop(NPC npc, Animation ani, bool fadeout)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCAniStopMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)ani.LayerID);
            stream.Write(fadeout);

            npc.ForEachVisibleClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        #endregion

        #region Equipment

        public static void WriteEquipMessage(NPC npc, Item item)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCEquipMessage);

            stream.Write((ushort)npc.ID);
            stream.Write((byte)item.slot);
            item.WriteEquipProperties(stream);

            npc.ForEachVisibleClient(client =>
            {
                if (client != npc.client)
                    client.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W');
            });
        }

        public static void WriteEquipSwitchMessage(NPC npc, Item item)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCEquipSwitchMessage);

            stream.Write((ushort)npc.ID);
            stream.Write((byte)item.ID);
            stream.Write((byte)item.slot);

            npc.ForEachVisibleClient(client => client.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        public static void WriteUnequipMessage(NPC npc, Item item)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCUnequipMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)item.ID);

            npc.ForEachVisibleClient(client =>
            {
                if (client != npc.client)
                    client.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W');
            });
        }

        #endregion
        
        #region Health

        public static void WriteHealthMessage(NPC npc)
        {
            var stream = GameServer.SetupStream(NetworkIDs.NPCHealthMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((ushort)npc.HPMax);
            stream.Write((ushort)npc.HP);
            npc.ForEachVisibleClient(client => client.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        #endregion

        #region Fight Mode

        public static void WriteSetFightMode(NPC npc)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCSetFightModeMessage);
            stream.Write((ushort)npc.ID);
            npc.ForEachVisibleClient(client => client.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        public static void WriteUnsetFightMode(NPC npc)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCUnsetFightModeMessage);
            stream.Write((ushort)npc.ID);
            npc.ForEachVisibleClient(client => client.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        public static void ReadSetFightMode(PacketReader stream, NPC character)
        {
            character.ScriptObject.OnCmdSetFightMode(true);
        }

        public static void ReadUnsetFightMode(PacketReader stream, NPC character)
        {
            character.ScriptObject.OnCmdSetFightMode(false);
        }

        #endregion
    }
}

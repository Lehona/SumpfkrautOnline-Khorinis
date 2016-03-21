﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using GUC.Enumeration;
using GUC.WorldObjects;
using GUC.Network;
using GUC.Animations;

namespace GUC.Server.Network.Messages
{
    static class NPCMessage
    {
        #region States

        public static void ReadState(PacketReader stream, GameClient client, NPC character, World world)
        {
            int id = stream.ReadUShort();
            NPC npc;
            if (world.TryGetVob(id, out npc))
            {
                NPCStates state = (NPCStates)stream.ReadByte();
                if (npc == character /*|| (client.VobControlledList.Contains(npc) && state <= NPCStates.MoveBackward)*/) //is it a controlled NPC?
                {
                    if (npc.ScriptObject != null)
                        npc.ScriptObject.OnCmdMove(state);
                }
            }
        }

        public static void WriteState(NPC npc, NPCStates state)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCStateMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)state);

            npc.Cell.ForEachSurroundingClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        #endregion

        #region Jumping

        public static void ReadJump(PacketReader stream, GameClient client, NPC character, World world)
        {
            int id = stream.ReadUShort();
            NPC npc;
            if (world.TryGetVob(id, out npc))
            {
                if (npc == character /*|| (client.VobControlledList.Contains(npc) && state <= NPCStates.MoveBackward)*/) //is it a controlled NPC?
                {
                    if (npc.ScriptObject != null)
                        npc.ScriptObject.OnCmdJump();
                }
            }
        }

        public static void WriteJump(NPC npc)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCJumpMessage);
            stream.Write((ushort)npc.ID);

            npc.Cell.ForEachSurroundingClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W'));
        }

        #endregion

        public static void WriteDrawItem(IEnumerable<GameClient> list, NPC npc, Item item, bool fast)
        {
            PacketWriter stream = Network.GameServer.SetupStream(NetworkIDs.NPCDrawItemMessage);
            stream.Write(npc.ID);
            stream.Write(fast);
            //item.WriteEquipped(stream);

            foreach (GameClient client in list)
                client.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W');
        }

        public static void WriteUndrawItem(IEnumerable<GameClient> list, NPC npc, bool fast, bool altRemove)
        {
            if (npc == null)
                return;

            /*PacketWriter stream = Network.GameServer.SetupStream(NetworkIDs.NPCUndrawItemMessage);
            stream.Write(npc.ID);
            stream.Write(fast);
            stream.Write(altRemove);

            foreach (Client client in list)
                client.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W');*/
        }

        #region Animation

        #region Overlays

        public static void ReadApplyOverlay(PacketReader stream, NPC npc)
        {
            Overlay ov;
            if (npc.Model.TryGetOverlay(stream.ReadByte(), out ov))
            {
                npc.ScriptObject.OnCmdApplyOverlay(ov);
            }
        }

        public static void ReadRemoveOverlay(PacketReader stream, NPC npc)
        {
            Overlay ov;
            if (npc.Model.TryGetOverlay(stream.ReadByte(), out ov))
            {
                npc.ScriptObject.OnCmdRemoveOverlay(ov);
            }
        }

        public static void WriteApplyOverlayMessage(NPC npc, Overlay overlay)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCApplyOverlayMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)overlay.ID);

            npc.Cell.ForEachSurroundingClient(c => c.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE, 'W'));
        }

        public static void WriteRemoveOverlayMessage(NPC npc, Overlay overlay)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCRemoveOverlayMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)overlay.ID);

            npc.Cell.ForEachSurroundingClient(c => c.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE, 'W'));
        }

        #endregion

        public static void ReadAniStart(PacketReader stream, NPC character)
        {
            AniJob job;
            if (character.Model.TryGetAni(stream.ReadUShort(), out job))
            {
                character.ScriptObject.OnCmdStartAni(job);
            }
        }

        public static void ReadAniStop(PacketReader stream, NPC character)
        {
            character.ScriptObject.OnCmdStopAni(stream.ReadBit());
        }


        public static void WriteAniStart(NPC npc, Animation ani)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCAniStartMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((ushort)ani.AniJob.ID);

            npc.Cell.ForEachSurroundingClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, 'W'));
        }

        public static void WriteAniStop(NPC npc, bool fadeout)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCAniStartMessage);
            stream.Write((ushort)npc.ID);
            stream.Write(fadeout);

            npc.Cell.ForEachSurroundingClient(c => c.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, 'W'));
        }

        #endregion

        public static void WriteEquipMessage(NPC npc, Item item)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCEquipMessage);

            stream.Write((ushort)npc.ID);
            stream.Write((byte)item.Slot);
            item.WriteEquipProperties(stream);

            npc.Cell.ForEachSurroundingClient(client =>
            {
                if (client != npc.client)
                    client.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W');
            });
        }

        public static void WriteUnequipMessage(NPC npc, int slot)
        {
            PacketWriter stream = GameServer.SetupStream(NetworkIDs.NPCUnequipMessage);
            stream.Write((ushort)npc.ID);
            stream.Write((byte)slot);

            npc.Cell.ForEachSurroundingClient(client =>
            {
                if (client != npc.client)
                    client.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W');
            });
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using GUC.WorldObjects;
using GUC.Log;
using GUC.GameObjects.Collections;
using GUC.WorldObjects.Cells;
using GUC.Scripting;
using GUC.Types;
using GUC.WorldObjects.VobGuiding;
using GUC.Models;
using GUC.WorldObjects.Definitions;

namespace GUC.Network
{
    /* FIXME:
     * Give every vob its own ID as RakNet channel char => (char)ID.
     */
    public partial class GameClient
    {
        #region Network Messages

        internal static class Messages
        {
            public static bool ReadConnection(PacketReader stream, RakNetGUID guid, SystemAddress ip, out GameClient client)
            {
                client = ScriptManager.Interface.CreateClient();
                client.driveHash = stream.ReadBytes(16);
                client.macHash = stream.ReadBytes(16);

                if (client.ScriptObject.IsAllowedToConnect())
                {
                    client.guid = new RakNetGUID(guid.g);
                    client.systemAddress = new SystemAddress(ip.ToString(), ip.GetPort());
                    return true;
                }
                else
                {
                    client = null;
                    return false;
                }
            }

            static byte[] dynamics = null;
            public static void WriteDynamics(GameClient client)
            {
                if (dynamics == null)
                {
                    if (GUCBaseVobDef.GetCountDynamics() > 0 && ModelInstance.CountDynamics > 0)
                    {
                        PacketWriter strm = new PacketWriter();

                        // MODELS
                        if (strm.Write(ModelInstance.CountDynamics > 0))
                        {
                            strm.Write((ushort)ModelInstance.CountDynamics);
                            ModelInstance.ForEachDynamic(model =>
                            {
                                model.WriteStream(strm);
                            });
                        }

                        // INSTANCES
                        if (strm.Write(GUCBaseVobDef.GetCountDynamics() > 0))
                        {
                            strm.Write((ushort)GUCBaseVobDef.GetCountDynamics());
                            GUCBaseVobDef.ForEachDynamic(v =>
                            {
                                strm.Write((byte)v.ScriptObject.GetVobType());
                                v.WriteStream(strm);
                            });
                        }

                        byte[] arr = strm.CopyData();
                        int decomp = arr.Length;
                        using (var ms = new System.IO.MemoryStream(decomp))
                        {
                            using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress))
                            {
                                ds.Write(arr, 0, decomp);
                            }
                            arr = ms.ToArray();
                        }

                        strm.Reset();
                        strm.Write((byte)ServerMessages.DynamicsMessage);
                        strm.Write(decomp);
                        strm.Write(arr, 0, arr.Length);
                        dynamics = strm.CopyData();
                    }
                    else
                    {
                        return;
                    }
                }
                client.Send(dynamics, dynamics.Length, NetPriority.Low, NetReliability.Reliable, '\0');

            }

            #region Spectator

            public static void ReadSpectatorPosition(PacketReader stream, GameClient client)
            {
                if (!client.IsSpectating)
                    return;

                client.SetPosition(stream.ReadCompressedPosition(), false);
            }

            public static void WriteSpectatorMessage(GameClient client, Vec3f pos, Angles ang)
            {
                var stream = GameServer.SetupStream(ServerMessages.SpectatorMessage);
                stream.Write(pos);
                stream.Write(ang);
                client.Send(stream, NetPriority.Low, NetReliability.Reliable, '\0');
            }

            #endregion

            #region NPC Control

            public static void WritePlayerControl(GameClient client, GUCNPCInst npc)
            {
                PacketWriter stream = GameServer.SetupStream(ServerMessages.PlayerControlMessage);
                stream.Write((ushort)npc.ID);
                npc.WriteTakeControl(stream);
                client.Send(stream, NetPriority.Low, NetReliability.ReliableOrdered, '\0');
            }

            #endregion

            public static void ReadLoadWorldMessage(PacketReader stream, GameClient client)
            {
                client.loading = false;
                if (client.character != null)
                {
                    client.character.client = client;
                    client.JoinWorld(client.character.World, client.character.Position);
                    client.character.SpawnPlayer();
                }
                else if (client.specWorld != null)
                {
                    client.isSpectating = true;
                    WriteSpectatorMessage(client, client.specPos, client.specAng); // tell the client that he's spectating
                    client.specWorld.AddClient(client);
                    client.specWorld.AddSpectatorToCells(client);
                    client.JoinWorld(client.specWorld, client.specPos);
                }
                else
                {
                    //throw new Exception("Unallowed LoadWorldMessage");
                }
            }

            public static void ReadScriptCommandMessage(PacketReader stream, GameClient client, World world, bool hero)
            {
                GuidedVob vob;
                if (hero)
                {
                    vob = client.character;
                }
                else
                {
                    world.TryGetVob(stream.ReadUShort(), out vob);
                }

                if (vob == null)
                    return;

                client.ScriptObject.ReadScriptRequestMessage(stream, vob);
            }
        }

        #endregion

        #region ScriptObject

        /// <summary>
        /// The ScriptObject interface
        /// </summary>
        public partial interface IScriptClient : IScriptGameObject
        {
            bool IsAllowedToConnect();

            void ReadScriptMessage(PacketReader stream);
            void ReadScriptRequestMessage(PacketReader stream, GuidedVob vob);
        }

        #endregion

        #region Collection

        public bool IsConnected { get { return this.isCreated; } }

        static StaticCollection<GameClient> idColl = new StaticCollection<GameClient>(200); // slots
        static DynamicCollection<GameClient> clients = new DynamicCollection<GameClient>();

        internal void Create()
        {
            if (this.isCreated)
                throw new Exception("Client is already in the collection!");

            idColl.Add(this);
            clients.Add(this, ref this.collID);

            this.isCreated = true;

            this.ScriptObject.OnConnection();

            Messages.WriteDynamics(this);
        }

        internal void Delete()
        {
            if (!this.isCreated)
                throw new Exception("Client is not in the collection!");

            if (this.character != null)
            {
                if (this.character.IsSpawned && !loading)
                {
                    this.character.World.RemoveClient(this);
                    this.Character.Cell.RemoveClient(this);
                }
                this.character.client = null;
            }
            else if (this.isSpectating)
            {
                if (!loading) // still loading
                {
                    this.specWorld.RemoveClient(this);
                    this.specWorld.RemoveSpectatorFromCells(this);
                }
                this.specWorld = null;
            }

            visibleVobs.ForEach(vob => vob.RemoveVisibleClient(this));
            visibleVobs.Clear();

            this.isCreated = false;

            int id = this.ID; // preserve the id
            idColl.Remove(this);
            clients.Remove(ref this.collID);

            this.ScriptObject.OnDisconnection(id);

            this.character = null;

            this.systemAddress.Dispose();
            this.guid.Dispose();
        }

        /// <summary>
        /// return FALSE to break the loop.
        /// </summary>
        public static void ForEachPredicate(Predicate<GameClient> predicate)
        {
            clients.ForEachPredicate(predicate);
        }

        public static void ForEach(Action<GameClient> action)
        {
            clients.ForEach(action);
        }

        public static int Count { get { return clients.Count; } }

        public static bool TryGetClient(int id, out GameClient client)
        {
            return idColl.TryGet(id, out client);
        }

        #endregion

        #region Vob guiding

        internal GODictionary<GuidedVob> GuidedVobs = new GODictionary<GuidedVob>(20);

        class IntBox { public int Count = 1; }
        Dictionary<int, IntBox> guideTargets = new Dictionary<int, IntBox>(5);
        internal void AddGuideTarget(GUCBaseVobInst vob)
        {
            if (guideTargets.TryGetValue(vob.ID, out IntBox box))
            {
                box.Count++;
            }
            else
            {
                guideTargets.Add(vob.ID, new IntBox());
                if (!visibleVobs.Contains(vob.ID))
                {
                    vob.targetOf.Add(this);
                }
            }
        }

        internal void RemoveGuideTarget(GUCBaseVobInst vob)
        {
            if (guideTargets.TryGetValue(vob.ID, out IntBox box))
            {
                box.Count--;
                if (box.Count == 0)
                {
                    guideTargets.Remove(vob.ID);
                    vob.targetOf.Remove(this);
                }
            }
        }

        #endregion

        #region Properties

        internal int cellID = -1;

        //Networking
        RakNetGUID guid;
        public RakNetGUID Guid { get { return this.guid; } }

        SystemAddress systemAddress;
        public String SystemAddress { get { return systemAddress.ToString(); } }

        byte[] driveHash;
        public byte[] GetDriveHash()
        {
            byte[] arr = new byte[driveHash.Length];
            Array.Copy(driveHash, arr, driveHash.Length);
            return arr;
        }

        byte[] macHash;
        public byte[] GetMacHash()
        {
            byte[] arr = new byte[macHash.Length];
            Array.Copy(macHash, arr, macHash.Length);
            return arr;
        }

        #endregion

        #region Vob visibility

        GODictionary<GUCBaseVobInst> visibleVobs = new GODictionary<GUCBaseVobInst>();

        internal void AddVisibleVob(GUCBaseVobInst vob)
        {
            visibleVobs.Add(vob);
            vob.targetOf.Remove(this);
        }

        internal void RemoveVisibleVob(GUCBaseVobInst vob)
        {
            visibleVobs.Remove(vob.ID);
            if (guideTargets.ContainsKey(vob.ID))
            {
                vob.targetOf.Add(this);
            }
        }

        internal void UpdateVobList(World world, Vec3f pos)
        {
            int removeCount = 0, addCount = 0;
            PacketWriter stream = GameServer.SetupStream(ServerMessages.WorldCellMessage);

            // first, clean all vobs which are out of range
            if (visibleVobs.Count > 0)
            {
                // save the position where the count is written
                int removeCountBytePos = stream.Position;
                stream.Write(ushort.MinValue);

                visibleVobs.ForEach(vob =>
                {
                    if (vob.Position.GetDistancePlanar(pos) > World.SpawnRemoveRange)
                    {
                        stream.Write((ushort)vob.ID);

                        vob.RemoveVisibleClient(this);
                        visibleVobs.Remove(vob.ID);
                        removeCount++;
                    }
                });

                // vobs were removed, write the count at the start
                if (removeCount > 0)
                {
                    int currentByte = stream.Position;
                    stream.Position = removeCountBytePos;
                    stream.Write((ushort)removeCount);
                    stream.Position = currentByte;
                }
            }
            else
            {
                stream.Write(ushort.MinValue);
            }

            // save the position where we wrote the count of new vobs
            int countBytePos = stream.Position;
            stream.Write(ushort.MinValue);

            // then look for new vobs
            if (visibleVobs.Count > 0) // we have to check whether we know the vob already
            {
                world.ForEachDynVobRougher(pos, World.SpawnInsertRange, vob =>
                {
                    if (!visibleVobs.Contains(vob.ID))
                    {
                        if (pos.GetDistancePlanar(vob.Position) < World.SpawnInsertRange)
                        {
                            AddVisibleVob(vob);
                            vob.AddVisibleClient(this);

                            stream.Write((byte)vob.ScriptObject.GetVobType());
                            vob.WriteStream(stream);
                            addCount++;
                        }
                    }
                });
            }
            else // just add everything
            {
                world.ForEachDynVobRougher(pos, World.SpawnInsertRange, vob =>
                {
                    if (pos.GetDistancePlanar(vob.Position) < World.SpawnInsertRange)
                    {
                        AddVisibleVob(vob);
                        vob.AddVisibleClient(this);

                        stream.Write((byte)vob.ScriptObject.GetVobType());
                        vob.WriteStream(stream);
                        addCount++;
                    }
                });
            }

            // vobs were added, write the correct count at the start
            if (addCount > 0)
            {
                int currentByte = stream.Position;
                stream.Position = countBytePos;
                stream.Write((ushort)addCount);
                stream.Position = currentByte;
            }
            else if (removeCount == 0) // nothing changed
            {
                return;
            }

            this.Send(stream, NetPriority.Low, NetReliability.ReliableOrdered, 'W');
        }

        void JoinWorld(World world, Vec3f pos)
        {
            PacketWriter stream = GameServer.SetupStream(ServerMessages.WorldJoinMessage);

            // save vob count position for later
            int countBytePos = stream.Position;
            stream.Write(ushort.MinValue);

            // check for vobs
            world.ForEachDynVobRougher(pos, World.SpawnInsertRange, vob =>
            {
                if (vob.Position.GetDistance(pos) < World.SpawnInsertRange)
                {
                    AddVisibleVob(vob);
                    vob.AddVisibleClient(this);

                    stream.Write((byte)vob.ScriptObject.GetVobType());
                    vob.WriteStream(stream);
                }
            });

            if (visibleVobs.Count > 0)
            {
                // write correct vob count to the front
                int currentByte = stream.Position;
                stream.Position = countBytePos;
                stream.Write((ushort)visibleVobs.Count);
                stream.Position = currentByte;
                this.Send(stream, NetPriority.Low, NetReliability.ReliableOrdered, 'W');
            }
        }

        void LeaveWorld()
        {
            World.Messages.WriteLeaveWorld(this);

            visibleVobs.ForEach(vob => vob.RemoveVisibleClient(this));
            visibleVobs.Clear();
        }

        #endregion

        #region Spectating

        internal BigCell SpecCell;

        partial void pSpecSetPos()
        {
            this.SetPosition(this.specPos, true);
        }

        Vec3f lastPos = Vec3f.Null;
        const int UpdateDistance = 100;
        internal void SetPosition(Vec3f pos, bool sendToClient)
        {
            this.specPos = pos.ClampToWorldLimits();
            this.specWorld.UpdateSpectatorCell(this, this.specPos);

            if (specPos.GetDistancePlanar(lastPos) > UpdateDistance)
            {
                lastPos = specPos;
                UpdateVobList(this.specWorld, specPos);
            }
        }

        partial void pSetToSpectate(World world, Vec3f pos, Angles ang)
        {
            if (this.isSpectating) // is spectating, but in a different world
            {
                this.isSpectating = false;
                if (!this.loading)
                {
                    this.specWorld.RemoveClient(this);
                    this.specWorld.RemoveSpectatorFromCells(this);

                    this.visibleVobs.ForEach(v => v.RemoveVisibleClient(this));
                    this.visibleVobs.Clear();
                }
                World.Messages.WriteLoadWorld(this, world);
            }
            else
            {
                if (this.character == null)
                {
                    World.Messages.WriteLoadWorld(this, world);
                }
                else
                {
                    // set old character to npc
                    this.character.client = null;

                    if (this.character.IsSpawned)
                    {
                        if (!this.loading)
                        {
                            this.character.World.RemoveClient(this);
                            this.character.Cell.RemoveClient(this);
                        }

                        if (this.character.World != world)
                        {
                            if (!this.loading)
                            {
                                this.visibleVobs.ForEach(v => v.RemoveVisibleClient(this));
                                this.visibleVobs.Clear();
                            }
                            World.Messages.WriteLoadWorld(this, world);
                        }
                        else
                        {
                            // same world, just update
                            this.isSpectating = true;
                            Messages.WriteSpectatorMessage(this, pos, ang);
                            world.AddClient(this);
                            world.AddSpectatorToCells(this);
                            UpdateVobList(world, pos);
                        }
                    }
                    else
                    {
                        if (!this.loading)
                        {
                            this.visibleVobs.ForEach(v => v.RemoveVisibleClient(this));
                            this.visibleVobs.Clear();
                        }
                        World.Messages.WriteLoadWorld(this, world);
                    }
                    this.character = null;
                }
            }

            this.specPos = pos;
            this.specAng = ang;
            this.specWorld = world;
        }

        #endregion

        #region Player control

        partial void pSetControl(GUCNPCInst npc)
        {
            if (npc == null) // take control of nothing
            {
                if (this.isSpectating)
                {
                    this.specWorld.RemoveClient(this);
                    this.specWorld.RemoveSpectatorFromCells(this);
                }
                else if (this.character != null)
                {
                    if (this.character.IsSpawned && !this.loading)
                    {
                        this.character.World.RemoveClient(this);
                        this.character.Cell.RemoveClient(this);
                    }
                    this.character.client = null;
                }
                this.LeaveWorld();
                this.character = null;
                return;
            }

            if (npc.IsPlayer)
            {
                Logger.LogWarning("Rejected SetControl of Player {0} by Client {1}!", npc.ID, this.ID);
                return;
            }

            // npc is already in the world, set to player
            if (npc.IsSpawned)
            {
                if (this.isSpectating)
                {
                    if (this.specWorld != npc.World)
                    {
                        if (!this.loading)
                        {
                            this.specWorld.RemoveClient(this);
                            this.specWorld.RemoveSpectatorFromCells(this);

                            this.visibleVobs.ForEach(v => v.RemoveVisibleClient(this));
                            this.visibleVobs.Clear();
                        }

                        World.Messages.WriteLoadWorld(this, npc.World);
                    }
                    else // same world
                    {
                        if (npc.Cell != this.SpecCell)
                        {
                            this.specWorld.RemoveSpectatorFromCells(this);
                            npc.Cell.AddClient(this);
                        }

                        UpdateVobList(npc.World, npc.Position);
                        Messages.WritePlayerControl(this, npc);
                    }

                    this.specWorld = null;
                    this.isSpectating = false;
                }
                else // not spectating
                {
                    if (this.character == null) // has been in the main menu probably
                    {
                        World.Messages.WriteLoadWorld(this, npc.World);
                    }
                    else if (this.character.World != npc.World) // different world
                    {
                        if (this.character.IsSpawned && !this.loading)
                        {
                            this.character.World.RemoveClient(this);
                            this.character.Cell.RemoveClient(this);

                            this.visibleVobs.ForEach(v => v.RemoveVisibleClient(this));
                            this.visibleVobs.Clear();
                        }

                        this.character.client = null;
                        World.Messages.WriteLoadWorld(this, npc.World);
                    }
                    else // same world
                    {
                        if (this.character.Cell != npc.Cell)
                        {
                            this.character.Cell.RemoveClient(this);
                            npc.Cell.AddClient(this);
                        }

                        this.character.client = null;
                        UpdateVobList(npc.World, npc.Position);

                        Messages.WritePlayerControl(this, npc);
                    }
                }
            }
            else // npc is not spawned remove all old vobs
            {
                if (this.isSpectating)
                {
                    this.specWorld.RemoveClient(this);
                    this.specWorld.RemoveSpectatorFromCells(this);
                    this.specWorld = null;
                    this.isSpectating = false;
                    LeaveWorld();
                }
                else if (this.character != null)
                {
                    if (this.character.IsSpawned && !this.loading)
                    {
                        this.character.Cell.RemoveClient(this);
                        this.character.World.RemoveClient(this);
                    }
                    this.character.client = null;
                    LeaveWorld();
                }
            }

            this.character = npc;
            npc.client = this;
        }

        #endregion

        #region Networking

        internal void Send(byte[] data, int length, NetPriority pp, NetReliability pr, char orderingChannel)
        {
            if (!this.isCreated)
                throw new Exception("Client has disconnected.");

            GameServer.ServerInterface.Send(data, length, (PacketPriority)pp, (PacketReliability)pr, (char)this.ID/*'\0'/*orderingChannel*/, this.guid, false);
        }

        internal void Send(PacketWriter stream, NetPriority pp, NetReliability pr, char orderingChannel)
        {
            this.Send(stream.GetData(), stream.GetLength(), pp, pr, orderingChannel);
        }

        public int GetLastPing()
        {
            return GameServer.ServerInterface.GetLastPing(this.guid);
        }

        public void Disconnect()
        {
            GameServer.DisconnectClient(this);
        }

        public void Kick(string message = "")
        {
            Logger.Log("Client kicked: {0} IP:{1}", this.ID, this.SystemAddress);
            GameServer.DisconnectClient(this);
        }

        public void Ban(string message = "")
        {
            Kick(message);
            GameServer.AddToBanList(this.SystemAddress);
        }

        public static PacketWriter GetScriptMessageStream()
        {
            return GameServer.SetupStream(ServerMessages.ScriptMessage);
        }

        public void SendScriptMessage(PacketWriter stream, NetPriority pr, NetReliability rl)
        {
            this.Send(stream, pr, rl, 'M');
        }

        /// <summary> Only use if you know what you're doing. </summary>
        public void SendScriptMessage(byte[] data, int length, NetPriority pr, NetReliability rl)
        {
            this.Send(data, length, pr, rl, 'M');
        }

        #endregion
    }
}

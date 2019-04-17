﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using GUC.Types;
using GUC.Log;
using GUC.GUI;
using GUC.Scripting;
using GUC.WorldObjects;
using Gothic.Objects;
using GUC.WorldObjects.VobGuiding;
using GUC.Models;
using GUC.WorldObjects.Instances;
using GUC.Utilities;
using GUC.GameObjects;

namespace GUC.Network
{
    public partial class GameClient : IDObject
    {
        #region Network Messages

        internal static class Messages
        {
            public static void ReadDynamics(PacketReader stream)
            {
                Logger.Log("Read dynamic instances: {0} Bytes.", stream.Length);

                int decomp = stream.ReadInt();
                byte[] data = new byte[decomp];
                using (var ms = new System.IO.MemoryStream(stream.GetRemainingData()))
                using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))
                {
                    ds.Read(data, 0, decomp);
                }
                stream.Load(data, decomp);

                // Read models
                if (stream.ReadBit())
                {
                    int count = stream.ReadUShort();
                    for (int i = 0; i < count; i++)
                    {
                        ModelInstance model = ScriptManager.Interface.CreateModelInstance();
                        model.ReadStream(stream);
                        model.ScriptObject.Create();
                    }
                }

                // Read vob instances
                if (stream.ReadBit())
                {
                    int count = stream.ReadUShort();
                    for (int i = 0; i < count; i++)
                    {
                        byte type = stream.ReadByte();
                        GUCBaseVobDef inst = ScriptManager.Interface.CreateInstance(type);
                        inst.ReadStream(stream);
                        inst.ScriptObject.Create();
                    }
                }
            }

            public static void WriteConnection()
            {
                string signature = ""; // Program.GetSignature(0);
                string mac = ""; // Program.GetMacAddress();

                Logger.Log("Signature: " + signature);
                Logger.Log("MAC: " + mac);

                PacketWriter stream = GameClient.SetupStream(ClientMessages.ConnectionMessage);
                stream.Write(signature.GetMD5Hash(), 0, 16);
                stream.Write(signature.GetMD5Hash(), 0, 16);
                GameClient.Send(stream, NetPriority.Immediate, NetReliability.Reliable);
            }

            public static void ReadScriptVob(PacketReader stream)
            {
                int id = stream.ReadUShort();

                if (World.Current.TryGetVob(id, out GUCBaseVobInst vob))
                {
                    Client.ScriptObject.ReadScriptVobMessage(stream, vob);
                }
            }
        }

        #endregion

        public static readonly GameClient Client;

        #region RakNet

        static readonly RakPeerInterface clientInterface;
        static readonly SocketDescriptor socketDescriptor;

        static readonly GUCVisual abortInfo;
        static readonly GUCVisual devInfo;
        public static bool ShowInfo
        {
            get { return devInfo.Shown; }
            set
            {
                if (value) devInfo.Show();
                else devInfo.Hide();
            }
        }


        #region Connection

        static GameClient()
        {
            Client = ScriptManager.Interface.CreateClient();

            // Init RakNet objects
            clientInterface = RakPeerInterface.GetInstance();
            clientInterface.SetOccasionalPing(true);

            socketDescriptor = new SocketDescriptor();
            socketDescriptor.port = 0;

            if (clientInterface.Startup(1, socketDescriptor, 1) != StartupResult.RAKNET_STARTED)
            {
                Logger.LogError("RakNet failed to start!");
            }

            // Init debug info on screen
            var screenSize = GUCView.GetScreenSize();
            abortInfo = new GUCVisual((screenSize.Y - 300) / 2, 150, 300, 40);
            abortInfo.SetBackTexture("Menu_Choice_Back.tga");
            GUCVisualText visText = abortInfo.CreateText("Verbindung unterbrochen!");
            visText.SetColor(ColorRGBA.Red);

            devInfo = new GUCVisual();
            for (int pos = 0; pos < 0x2000; pos += devInfo.zView.FontY() + 5)
            {
                var t = devInfo.CreateText("", 0x2000, pos, true);
                t.Format = GUCVisualText.TextFormat.Right;
            }
            devInfo.Show();
        }


        static bool isConnecting = false;
        public static bool IsConnecting { get { return isConnecting; } }

        static int connectionAttempts = 0;
        public static int ConnectionAttempts { get { return connectionAttempts; } }

        static bool isConnected = false;
        public static bool IsConnected { get { return isConnected; } }

        static bool isDisconnected = false;
        /// <summary> Is true when we've received a disconnect message (f.e. from a kick). No further connection attempts are then started. </summary>
        public static bool IsDisconnected { get { return isDisconnected; } }

        internal static void Connect()
        {
            try
            {
                if (isConnected || isConnecting)
                    return;

                isConnecting = true;

                ConnectionAttemptResult res = clientInterface.Connect(Program.ServerIP, Program.ServerPort, Program.Password, Program.Password == null ? 0 : Program.Password.Length);
                if (res != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED)
                {
                    throw new Exception("Connection couldn't be established: " + res);
                }

                Logger.Log("Connection attempt {0} to '{1}:{2}' started.", ++connectionAttempts, Program.ServerIP, Program.ServerPort);
            }
            catch (Exception e)
            {
                Logger.LogError("Connection failed! " + e);
            }
        }

        #endregion

        #region Update

        static long receivedBytes = 0;
        static long sentBytes = 0;
        static LockTimer infoTimer = new LockTimer(1000);

        static readonly PacketWriter packetWriter = new PacketWriter(128000);
        static readonly PacketReader packetReader = new PacketReader();

        internal static void Update()
        {
            int counter = 0;
            ServerMessages msgType;
            Packet packet;

            // Receive packets
            while ((packet = clientInterface.Receive()) != null)
            {
                try
                {
                    receivedBytes += packet.length;

                    packetReader.Load(packet.data, (int)packet.length);
                    msgType = (ServerMessages)packetReader.ReadByte();
                    ReadMessage(msgType, packetReader);

                    counter++;
                    if (counter >= 1000)
                    {
                        counter = 0;
                        Logger.Log("1000 Pakete hintereinander");
                    }
                }
                catch (Exception e)
                {
                    if (packet.length >= 1)
                        Logger.LogError("{0}: {1}: {2}\n{3}", (ServerMessages)packet.data[0], e.Source, e.Message, e.StackTrace);
                    else
                        Logger.LogError("{0}: {1}\n{2}", e.Source, e.Message, e.StackTrace);
                }
                finally
                {
                    clientInterface.DeallocatePacket(packet);
                }
            }

            #region Debug Info

            // update only every second
            if (infoTimer.IsReady)
            {
                int ping = clientInterface.GetLastPing(clientInterface.GetSystemAddressFromIndex(0));

                if (isDisconnected)
                {
                    abortInfo.Texts[0].Text = "Verbindung geschlossen!";
                    abortInfo.Show();
                }
                else if (isConnected)
                {
                    if (ping > 300 || ping < 0)
                    {
                        abortInfo.Show();
                    }
                    else
                    {
                        abortInfo.Hide();
                    }
                }

                // update ping text on screen
                int devIndex = 0;
                GUCVisualText pingText = devInfo.Texts[devIndex++];
                pingText.Text = string.Format("Ping: {0}ms", ping);
                ColorRGBA color;
                if (ping <= 120)
                {
                    color = new ColorRGBA((byte)(40 + 180 * ping / 120), 220, 40);
                }
                else if (ping <= 220)
                {
                    color = new ColorRGBA(220, (byte)(220 - 180 * (ping - 100) / 120), 40);
                }
                else
                {
                    color = new ColorRGBA(220, 40, 40);
                }
                pingText.SetColor(color);

                long fps = Hooks.hGame.LastElapsedTicks > 0 ? TimeSpan.TicksPerSecond / Hooks.hGame.LastElapsedTicks : 999;
                GUCVisualText fpsText = devInfo.Texts[devIndex++];
                fpsText.Text = "FPS: " + fps;
                if (fps < 10)
                {
                    color = new ColorRGBA(220, 40, 40);
                }
                else if (fps < 40)
                {
                    color = new ColorRGBA(220, (byte)(40 + 180 * (fps - 10) / 30), 40);
                }
                else if (fps < 90)
                {
                    color = new ColorRGBA((byte)(220 - 180 * (fps - 40) / 50), 220, 40);
                }
                else
                {
                    color = new ColorRGBA(40, 220, 40);
                }
                fpsText.SetColor(color);

                devInfo.Texts[devIndex++].Text = "Spike: " + Hooks.hGame.SpikeLongest / TimeSpan.TicksPerMillisecond + "ms";

                // update kB/s text on screen
                int kbs = (int)(receivedBytes);
                devInfo.Texts[devIndex++].Text = ("Net received: " + kbs + "B/s");
                kbs = (int)(sentBytes);
                devInfo.Texts[devIndex++].Text = ("Net Sent: " + kbs + "B/s");
                receivedBytes = 0;
                sentBytes = 0;

                if(GUCNPCInst.Hero != null)
                    devInfo.Texts[devIndex++].Text = ("Position: " + GUCNPCInst.Hero.Position);

                if (World.Current != null)
                {
                    devIndex = 8;
                    devInfo.Texts[devIndex++].Text = World.Current.VobCount + " Vobs";
                    devInfo.Texts[devIndex++].Text = Client.guidedIDs.Count + " guided";

                    devInfo.Texts[devIndex++].Text = "Weather: " + World.Current.WeatherCtrl.CurrentWeight + " " + World.Current.Clock.Time.ToString(false);
                    devInfo.Texts[devIndex++].Text = "Barrier: " + World.Current.BarrierCtrl.CurrentWeight + " " + World.Current.BarrierCtrl.EndWeight;

                    devInfo.Texts[devIndex++].Text = "VobSounds: " + SoundHandler.VobSoundCount;
                    devInfo.Texts[devIndex++].Text = "PosSounds: " + SoundHandler.PosSoundCount;

                    //if (NPC.Hero != null)
                    //    devInfo.Texts[devIndex++].Text =  NPC.Hero.Movement.ToString();

                }
            }

            #endregion
        }

        #endregion

        #region Read Message

        static void ReadMessage(ServerMessages id, PacketReader stream)
        {
            if (id != ServerMessages.VobPosDirMessage && id != ServerMessages.NPCPosAngMessage)
                Logger.Log(id);

            switch (id)
            {
                #region RakNet

                case ServerMessages.RakNet_ConnectionRequestAccepted:
                    isConnected = true; // we are connected
                    isConnecting = false;
                    connectionAttempts = 0; // reset attempt counter
                    Client.ScriptObject.OnConnection(); // let the scripts know too
                    Messages.WriteConnection(); // send mac & drive hash
                    break;

                case ServerMessages.RakNet_ConnectionAttemptFailed:
                    isConnected = false;
                    isConnecting = false;
                    break;

                case ServerMessages.RakNet_AlreadyConnected:
                    Logger.LogWarning("Already Connected!");
                    break;

                case ServerMessages.RakNet_ConnectionBanned:
                    Logger.LogError("Client banned!");
                    break;

                case ServerMessages.RakNet_InvalidPassword:
                    Logger.LogError("Wrong password");
                    break;

                case ServerMessages.RakNet_IncompatibleProtocolVersion:
                    Logger.LogError("ID_INCOMPATIBLE_PROTOCOL_VERSION");
                    break;

                case ServerMessages.RakNet_NoFreeIncomingConnections:
                    Logger.LogError("ID_NO_FREE_INCOMING_CONNECTIONS");
                    break;

                case ServerMessages.RakNet_DisconnectionNotification:
                case ServerMessages.RakNet_ConnectionLost:
                    isConnected = false;
                    isConnecting = false;
                    isDisconnected = true;
                    Logger.Log("Disconnected from server.");
                    Client.ScriptObject.OnDisconnection(-1);
                    break;

                #endregion

                /* 
                *   USER MESSAGES
                */

                // receiving dynamic instances
                case ServerMessages.DynamicsMessage:
                    Messages.ReadDynamics(stream);
                    break;

                // load a gothic world
                case ServerMessages.LoadWorldMessage:
                    World.Messages.ReadLoadWorld(stream);
                    break;

                case ServerMessages.WorldLeaveMessage:
                    throw new NotImplementedException();
                    break;

                // client becomes a spectator
                case ServerMessages.SpectatorMessage:
                    Client.ReadSpectatorMessage(stream);
                    break;

                case ServerMessages.PlayerControlMessage:
                    Client.ReadPlayerControlMessage(stream);
                    break;

                // World Messages
                case ServerMessages.WorldJoinMessage:
                    World.Messages.ReadJoinWorld(stream);
                    break;

                case ServerMessages.WorldCellMessage:
                    World.Messages.ReadCellMessage(stream);
                    break;

                case ServerMessages.WorldBarrierMessage:
                    WorldObjects.WorldGlobals.BarrierController.Messages.ReadBarrier(stream);
                    break;
                case ServerMessages.WorldWeatherMessage:
                    WorldObjects.WorldGlobals.WeatherController.Messages.ReadWeather(stream);
                    break;
                case ServerMessages.WorldWeatherTypeMessage:
                    WorldObjects.WorldGlobals.WeatherController.Messages.ReadWeatherType(stream);
                    break;
                case ServerMessages.WorldTimeMessage:
                    WorldObjects.WorldGlobals.WorldClock.Messages.ReadTime(stream);
                    break;
                case ServerMessages.WorldTimeStartMessage:
                    WorldObjects.WorldGlobals.WorldClock.Messages.ReadTimeStart(stream, true);
                    break;
                case ServerMessages.WorldTimeStopMessage:
                    WorldObjects.WorldGlobals.WorldClock.Messages.ReadTimeStart(stream, false);
                    break;

                // Vob Messages
                case ServerMessages.VobSpawnMessage:
                    World.Messages.ReadVobSpawn(stream);
                    break;
                case ServerMessages.VobDespawnMessage:
                    World.Messages.ReadVobDespawnMessage(stream);
                    break;
                case ServerMessages.VobPosDirMessage:
                    GUCBaseVobInst.Messages.ReadPosDirMessage(stream);
                    break;
                case ServerMessages.VobThrowMessage:
                    GUCVobInst.Messages.ReadThrow(stream);
                    break;

                case ServerMessages.ScriptMessage:
                    Client.ScriptObject.ReadScriptMessage(stream);
                    break;

                // ScriptVobMessages
                case ServerMessages.ScriptVobMessage:
                    Messages.ReadScriptVob(stream);
                    break;

                // NPC Messages
                case ServerMessages.NPCPosAngMessage:
                    GUCNPCInst.Messages.ReadPosAngMessage(stream);
                    break;
                case ServerMessages.NPCEquipAddMessage:
                    GUCNPCInst.Messages.ReadEquipMessage(stream);
                    break;
                case ServerMessages.NPCEquipRemoveMessage:
                    GUCNPCInst.Messages.ReadUnequipMessage(stream);
                    break;
                case ServerMessages.NPCEquipSwitchMessage:
                    GUCNPCInst.Messages.ReadEquipSwitchMessage(stream);
                    break;
                case ServerMessages.NPCFightModeSetMessage:
                    GUCNPCInst.Messages.ReadFightMode(stream, true);
                    break;
                case ServerMessages.NPCFightModeUnsetMessage:
                    GUCNPCInst.Messages.ReadFightMode(stream, false);
                    break;
                case ServerMessages.NPCHealthMessage:
                    GUCNPCInst.Messages.ReadHealth(stream);
                    break;

                // Player Messages
                case ServerMessages.PlayerInvAddItemMessage:
                    WorldObjects.ItemContainers.NPCInventory.Messages.ReadAddItem(stream);
                    break;
                case ServerMessages.PlayerInvRemoveItemMessage:
                    WorldObjects.ItemContainers.NPCInventory.Messages.ReadRemoveItem(stream);
                    break;
                case ServerMessages.PlayerItemAmountChangedMessage:
                    WorldObjects.GUCItemInst.Messages.ReadItemAmountChangedMessage(stream);
                    break;

                // Model Messages
                case ServerMessages.ModelAniUncontrolledMessage:
                    Model.Messages.ReadAniStartUncontrolled(stream);
                    break;
                case ServerMessages.ModelAniStartMessage:
                    Model.Messages.ReadAniStart(stream);
                    break;
                case ServerMessages.ModelAniStartFPSMessage:
                    Model.Messages.ReadAniStartFPS(stream);
                    break;
                case ServerMessages.ModelAniStopMessage:
                    Model.Messages.ReadAniStop(stream, false);
                    break;
                case ServerMessages.ModelAniFadeMessage:
                    Model.Messages.ReadAniStop(stream, true);
                    break;
                case ServerMessages.ModelOverlayAddMessage:
                    Model.Messages.ReadOverlay(stream, true);
                    break;
                case ServerMessages.ModelOverlayRemoveMessage:
                    Model.Messages.ReadOverlay(stream, false);
                    break;
                case ServerMessages.ModelInstanceCreateMessage:
                    // TODO: whatever has to be done
                    break;

                // vob guiding
                case ServerMessages.GuideAddCmdMessage:
                    GuidedVob.Messages.ReadGuideAddCmdMessage(stream);
                    break;
                case ServerMessages.GuideAddMessage:
                    GuidedVob.Messages.ReadGuideAddMessage(stream);
                    break;
                case ServerMessages.GuideRemoveCmdMessage:
                    GuidedVob.Messages.ReadGuideRemoveCmdMessage(stream);
                    break;
                case ServerMessages.GuideRemoveMessage:
                    GuidedVob.Messages.ReadGuideRemoveMessage(stream);
                    break;
                case ServerMessages.GuideSetCmdMessage:
                    GuidedVob.Messages.ReadGuideSetCmdMessage(stream);
                    break;
                case ServerMessages.VobInstanceCreateMessage:
                    Log.Logger.Log("Created Vob instance message >>");
                    // TODO: whatever has to be done
                    break;

                default:
                    Enum undefinedID;
                    if (Enum.IsDefined(typeof(ServerMessages), id))
                    {
                        undefinedID = id;
                    }
                    else
                    {
                        undefinedID = (DefaultMessageIDTypes)id;
                    }
                    Logger.LogWarning("Received message with invalid NetworkID! " + undefinedID);
                    break;
            }
        }


        #endregion

        #endregion

        #region ScriptObject

        public partial interface IScriptClient : IScriptGameObject
        {
            void ReadScriptMessage(PacketReader stream);
            void ReadScriptVobMessage(PacketReader stream, GUCBaseVobInst vob);
        }

        #endregion

        #region Guided vobs

        internal Dictionary<int, GuideCmd> guidedIDs = new Dictionary<int, GuideCmd>(100);

        #endregion

        #region Spectator

        internal static void UpdateSpectator(long now)
        {
            if (!Client.isSpectating)
                return;

            Client.UpdateSpectatorPos(now);
        }

        partial void pSetToSpectate(World world, Vec3f pos, Angles ang)
        {
            var cam = GothicGlobals.Game.GetCameraVob();
            cam.SetAI(specCam);

            cam.SetPositionWorld(pos.X, pos.Y, pos.Z);
            ang.SetMatrix(cam);

            this.isSpectating = true;
            this.character = null;
            GothicGlobals.Game.SetShowPlayerStatus(false);
        }

        oCAICamera specCam = oCAICamera.Create();
        void ReadSpectatorMessage(PacketReader stream)
        {
            Vec3f pos = stream.ReadVec3f();
            Angles ang = stream.ReadAngles();

            this.ScriptObject.SetToSpectator(World.Current, pos, ang);
        }

        Vec3f lastSpecPos;
        static long specNextUpdate = 0;
        const long SpecPosUpdateInterval = 2000000;
        void UpdateSpectatorPos(long now)
        {
            if (now < specNextUpdate)
                return;

            var cam = GothicGlobals.Game.GetCameraVob();
            var pos = new Vec3f(cam.Position).ClampToWorldLimits();
            cam.SetPositionWorld(pos.X, pos.Y, pos.Z);

            if (now - specNextUpdate < TimeSpan.TicksPerSecond && pos.GetDistance(lastSpecPos) < 10)
                return;

            lastSpecPos = pos;

            PacketWriter stream = SetupStream(ClientMessages.SpecatorPosMessage);
            stream.WriteCompressedPosition(pos);
            Send(stream, NetPriority.Low, NetReliability.Unreliable);

            specNextUpdate = now + SpecPosUpdateInterval;
        }

        #endregion

        #region Hero

        void ReadPlayerControlMessage(PacketReader stream)
        {
            int characterID = stream.ReadUShort();
            if (!World.Current.TryGetVob(characterID, out GUCNPCInst npc))
            {
                throw new Exception("Hero not found!");
            }
            npc.ReadTakeControl(stream);

            Logger.Log("Taking control of npc " + npc.ID);
            this.ScriptObject.SetControl(npc);
        }

        partial void pSetControl(GUCNPCInst npc)
        {
            this.character = npc;
            Character.gVob.SetAsPlayer();
            GothicGlobals.Game.GetCameraVob().SetAI(GothicGlobals.Game.GetCameraAI());
            this.isSpectating = false;
            GothicGlobals.Game.SetShowPlayerStatus(true);
        }

        #endregion

        internal static void Disconnect()
        {
            clientInterface.CloseConnection(clientInterface.GetSystemAddressFromIndex(0), true, 0);
            isConnected = false;
            isConnecting = false;
            isDisconnected = true;
            Client.ScriptObject.OnDisconnection(-1);
        }

        #region Script Message

        public static PacketWriter GetScriptMessageStream()
        {
            return SetupStream(ClientMessages.ScriptMessage);
        }

        public static void SendScriptMessage(PacketWriter stream, NetPriority priority, NetReliability reliability)
        {
            Send(stream, priority, reliability, 'M');
        }

        #endregion

        internal static PacketWriter SetupStream(ClientMessages id)
        {
            packetWriter.Reset();
            packetWriter.Write((byte)id);
            return packetWriter;
        }

        internal static void Send(PacketWriter stream, NetPriority pp, NetReliability pr, char orderingChannel = '\0')
        {
            clientInterface.Send(stream.GetData(), stream.GetLength(), (PacketPriority)pp, (PacketReliability)pr, orderingChannel, clientInterface.GetSystemAddressFromIndex(0), false);
            sentBytes += stream.GetLength();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;
using GUC.Enumeration;
using GUC.Server.WorldObjects;
using GUC.Server.Network.Messages;
using GUC.Network;

namespace GUC.Server.Network
{
    class Client : IDisposable
    {
        //Networking
        public RakNetGUID guid;
        public SystemAddress systemAddress;
        public String DriveString = "";
        public String MacString = "";
        public bool isValid = false;

        public bool instanceNPCNeeded;
        public bool instanceItemNeeded;
        public bool instanceMobNeeded;

        //Account
        public int accountID = -1;
        public bool isLoggedIn { get { return accountID != -1; } set { } }        

        //Ingame
        public NPC mainChar = null; //Main
        public NPC character = null; //current npc

        public List<AbstractCtrlVob> VobControlledList = new List<AbstractCtrlVob>();
        
        public Client(RakNetGUID guid, SystemAddress systemAddress)
        {
            this.guid = new RakNetGUID(guid.g);
            this.systemAddress = systemAddress;
        }

        public void SetControl(NPC npc)
        {
            //set old character to NPC
            if (character != null)
            {
                sWorld.PlayerDict.Remove(character.ID);
                sWorld.NPCDict.Add(character.ID, character);
                if (character.World != null)
                {
                    character.World.PlayerDict.Remove(npc.ID);
                    character.World.NPCDict.Add(character.ID, character);
                }
                if (character.cell != null)
                {
                    character.cell.PlayerList.Remove(npc);
                    character.cell.NPCList.Add(character);
                }
                character.client = null;
            }

            //npc is already in the world, set to player
            if (npc.Spawned)
            {
                npc.World.NPCDict.Remove(npc.ID);
                npc.World.PlayerDict.Add(npc.ID, npc);
                if (npc.cell != null)
                {
                    npc.cell.NPCList.Remove(npc);
                    npc.cell.PlayerList.Add(npc);
                }

                if (npc.VobController != null)
                    npc.VobController.RemoveControlledVob(npc);
            }

            sWorld.NPCDict.Remove(npc.ID);
            sWorld.PlayerDict.Add(npc.ID, npc);

            npc.client = this;
            character = npc;
            NPC.WriteControl(this, character);

            // additional adjustments to synchronize surroundings

            // update ingame-time
            character.World.ChangeIgTime(character.World.GetIgTime(), character.World.GetIgTimeRate(),
                new List<NPC> { character });
            // update ingame-weather
            character.World.ChangeIgWeather(character.World.GetWeatherType(), 
                character.World.GetWeatherStartTime(), character.World.GetWeatherEndTime(), 
                new List<NPC> { character });
        }

        public void CheckValidity(String driveString, String macString, byte[] npcTableHash, byte[] itemTableHash, byte[] mobTableHash)
        {
            //FIXME: Check for banned strings
            this.DriveString = driveString;
            this.MacString = macString;

            instanceNPCNeeded = !npcTableHash.SequenceEqual(NPCInstance.Table.hash);
            instanceItemNeeded = !itemTableHash.SequenceEqual(ItemInstance.Table.hash);
            instanceMobNeeded = !mobTableHash.SequenceEqual(MobInstance.Table.hash);

            isValid = true;
        }

        public void Disconnect()
        {
            Disconnect(false);
        }

        public void Disconnect(bool connectionLost)
        {
            if (mainChar != null)
            {
                mainChar.Despawn();
                sWorld.RemoveVob(mainChar);
                mainChar = null;
                //Messages.Connection.DisconnectMessage.Write(mainChar, connectionLost);
            }

            if (character != null)
            { //client was controlling someone else
                character.client = null;
            }

            for (int i = 0; i < VobControlledList.Count; i++)
            {
                VobControlledList[i].VobController = null;
                VobControlledList[i].FindNewController();
            }
        }

        public void AddControlledVob(AbstractCtrlVob vob)
        {
            VobControlledList.Add(vob);
            vob.VobController = this;
            BitStream stream = Program.server.SetupStream(NetworkID.ControlAddVobMessage); stream.mWrite(vob.ID);
            Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'G', guid, false);
            Log.Logger.log("AddCtrl: " + character.ID + " " + vob.ID + ": " + vob.GetType().Name);

            if (vob is NPC)
            {
                ((NPC)vob).GoTo(this.character, 500);
            }
        }

        public void RemoveControlledVob(AbstractCtrlVob vob)
        {
            VobControlledList.Remove(vob);
            vob.VobController = null;
            BitStream stream = Program.server.SetupStream(NetworkID.ControlRemoveVobMessage); stream.mWrite(vob.ID);
            Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'G', guid, false);
            Log.Logger.log("RemoveCtrl: " + character.ID + " " + vob.ID + ": " + vob.GetType().Name);
        }

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                guid.Dispose();
                disposed = true;
            }
        }
    }
}

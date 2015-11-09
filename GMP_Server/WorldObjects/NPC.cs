﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Enumeration;
using GUC.Server.Network;
using RakNet;
using GUC.Network;
using GUC.Server.Network.Messages;
using GUC.Types;
using GUC.Server.Scripting;

namespace GUC.Server.WorldObjects
{
    public class NPC : AbstractCtrlVob
    {
        /// <summary>
        /// Gets the NPCInstance of this NPC.
        /// </summary>
        public NPCInstance Instance { get; protected set; }

        #region Constructors

        /// <summary>
        /// Creates and returns an NPC object from the given NPCInstance-ID.
        /// Returns NULL when the ID is not found!
        /// </summary>
        public static NPC Create(ushort instanceID)
        {
            NPCInstance inst = NPCInstance.Table.Get(instanceID);
            if (inst == null)
            {
                Log.Logger.logError("NPC creation failed! Instance ID not found: " + instanceID);
                return null;
            }
            return Create(inst);
        }

        /// <summary>
        /// Creates and returns an NPC object from the given NPCInstance-Name.
        /// Returns NULL when the name is not found!
        /// </summary>
        public static NPC Create(string instanceName)
        {
            NPCInstance inst = NPCInstance.Table.Get(instanceName);
            if (inst == null)
            {
                Log.Logger.logError("NPC creation failed! Instance name not found: " + instanceName);
                return null;
            }
            return Create(inst);
        }

        /// <summary>
        /// Creates and returns an NPC object from the given NPCInstance.
        /// Returns NULL when the NPCInstance is NULL!
        /// </summary>
        public static NPC Create(NPCInstance instance)
        {
            if (instance != null)
            {
                NPC npc = new NPC();
                npc.Instance = instance;
                npc.BodyHeight = instance.BodyHeight;
                npc.BodyHeight = instance.BodyHeight;
                npc.BodyWidth = instance.BodyWidth;
                npc.BodyFatness = instance.Fatness;

                npc.AttrHealthMax = instance.AttrHealthMax;
                npc.AttrHealth = instance.AttrHealthMax;

                npc.AttrManaMax = instance.AttrManaMax;
                npc.AttrMana = instance.AttrManaMax;
                npc.AttrStaminaMax = instance.AttrStaminaMax;
                npc.AttrStamina = instance.AttrStaminaMax;

                npc.AttrStrength = instance.AttrStrength;
                npc.AttrDexterity = instance.AttrDexterity;

                npc.AttrCapacity = 100;

                npc.State = NPCState.Stand;
                npc.WeaponState = NPCWeaponState.None;
                return npc;
            }
            else
            {
                Log.Logger.logError("NPC creation failed! Instance can't be NULL!");
                return null;
            }
        }

        protected NPC()
        {
        }

        #endregion

        #region Appearance

        protected string customName = "";
        /// <summary>Set this for a different name from the instance-name.</summary>
        public string CustomName
        {
            get { return customName; }
            set
            {
                if (value == null || value == Instance.Name)
                {
                    customName = "";
                }
                else
                {
                    customName = value;
                }
            }
        }

        /// <summary>This field will be only used with the "_Male"- or "_Female"-Instance.</summary>
        public HumBodyTex HumanBodyTex = HumBodyTex.G1Hero;
        /// <summary>This field will be only used with the "_Male"- or "_Female"-Instance.</summary>
        public HumHeadMesh HumanHeadMesh = HumHeadMesh.HUM_HEAD_PONY;
        /// <summary>This field will be only used with the "_Male"- or "_Female"-Instance.</summary>
        public HumHeadTex HumanHeadTex = HumHeadTex.Face_N_Player;
        /// <summary>This field will be only used with the "_Male"- or "_Female"-Instance.</summary>
        public HumVoice HumanVoice = HumVoice.Hero;

        /// <summary>Body height in percent. (0% ... 255%)</summary>
        public byte BodyHeight;
        /// <summary>Body width in percent. (0% ... 255%)</summary>
        public byte BodyWidth;
        /// <summary>Fatness in percent. (-32768% ... +32767%)</summary>
        public short BodyFatness;

        #endregion

        //Things only the playing client should know
        #region Player stats

        public ushort AttrHealth;
        public ushort AttrHealthMax;

        public ushort AttrMana;
        public ushort AttrManaMax;
        public ushort AttrStamina;
        public ushort AttrStaminaMax;

        public ushort AttrStrength;
        public ushort AttrDexterity;

        public ushort AttrCapacity;

        #region Health

        public void AttrHealthUpdate()
        {
            BitStream stream = Program.server.SetupStream(NetworkID.NPCHitMessage);
            stream.mWrite(ID);
            stream.mWrite(AttrHealthMax);
            stream.mWrite(AttrHealth);

            foreach (Client cl in cell.SurroundingClients())
                Program.server.ServerInterface.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', cl.guid, false);
        }

        public void AttrManaStaminaUpdate()
        {
            if (isPlayer)
            {
                BitStream stream = Program.server.SetupStream(NetworkID.PlayerAttributeMSMessage);
                stream.mWrite(AttrManaMax);
                stream.mWrite(AttrMana);
                stream.mWrite(AttrStaminaMax);
                stream.mWrite(AttrStamina);
                Program.server.ServerInterface.Send(stream, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', client.guid, false);
            }
        }

        public void AttrUpdate()
        {
            BitStream stream = Program.server.SetupStream(NetworkID.NPCHitMessage);
            stream.mWrite(ID);
            stream.mWrite(AttrHealthMax);
            stream.mWrite(AttrHealth);

            foreach (Client cl in cell.SurroundingClients(client))
                Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', cl.guid, false);

            if (isPlayer)
            {
                // Update all the stats!
                stream = Program.server.SetupStream(NetworkID.PlayerAttributeMessage);
                stream.mWrite(AttrHealthMax);
                stream.mWrite(AttrHealth);
                stream.mWrite(AttrManaMax);
                stream.mWrite(AttrMana);
                stream.mWrite(AttrStaminaMax);
                stream.mWrite(AttrStamina);
                stream.mWrite(AttrCapacity);
                Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', client.guid, false);
            }
        }

        #endregion

        #endregion

        #region States

        public NPCState State { get; protected set; }
        public NPCWeaponState WeaponState { get; protected set; }

        #endregion

        public MobInter UsedMob { get; protected set; }

        #region Networking

        internal Client client;
        public bool isPlayer { get { return client != null; } }

        #region Client commands
        public delegate void CmdOnUseMobHandler(MobInter mob, NPC user);
        public static event CmdOnUseMobHandler CmdOnUseMob;
        public static event CmdOnUseMobHandler CmdOnUnUseMob;

        internal static void CmdReadUseMob(BitStream stream, Client client)
        {
            uint ID = stream.mReadUInt();

            Vob mob;
            if (client.character.World.VobDict.TryGetValue(ID, out mob) && mob is MobInter)
            {
                if (CmdOnUseMob != null)
                {
                    CmdOnUseMob((MobInter)mob, client.character);
                }
            }
        }

        internal static void CmdReadUnUseMob(BitStream stream, Client client)
        {
            if (client.character.UsedMob != null && CmdOnUnUseMob != null)
            {
                CmdOnUnUseMob(client.character.UsedMob, client.character);
            }
        }

        public delegate void CmdOnUseItemHandler(Item item, NPC user);
        public static event CmdOnUseItemHandler CmdOnUseItem;

        internal static void CmdReadUseItem(BitStream stream, Client client)
        {
            uint ID = stream.mReadUInt();
            Item item;
            if (sWorld.ItemDict.TryGetValue(ID, out item) && item.Owner == client.character && CmdOnUseItem != null)
            {
                CmdOnUseItem(item, client.character);
            }
        }

        #endregion

        internal override void WriteSpawn(IEnumerable<Client> list)
        {
            BitStream stream = Program.server.SetupStream(NetworkID.WorldNPCSpawnMessage);
            stream.mWrite(ID);
            stream.mWrite(Instance.ID);
            stream.mWrite(pos);
            stream.mWrite(dir);
            if (Instance.ID <= 2)
            {
                stream.mWrite((byte)HumanBodyTex);
                stream.mWrite((byte)HumanHeadMesh);
                stream.mWrite((byte)HumanHeadTex);
                stream.mWrite((byte)HumanVoice);
            }
            stream.mWrite(BodyHeight);
            stream.mWrite(BodyWidth);
            stream.mWrite(BodyFatness);

            stream.mWrite(CustomName);

            stream.mWrite(AttrHealthMax);
            stream.mWrite(AttrHealth);

            stream.mWrite((byte)equippedSlots.Count);
            foreach (KeyValuePair<byte,Item> slot in equippedSlots)
            {
                stream.mWrite(slot.Key);
                stream.mWrite(slot.Value.ID);
                stream.mWrite(slot.Value.Instance.ID);
                stream.mWrite(slot.Value.Condition);
            }

            foreach (Client cl in list)
                Program.server.ServerInterface.Send(stream, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', cl.guid, false);
        }

        public delegate void OnEnterWorldHandler(NPC player);
        public static event OnEnterWorldHandler OnEnterWorld;

        internal static void WriteControl(Client client, NPC npc)
        {
            BitStream stream = Program.server.SetupStream(NetworkID.PlayerControlMessage);
            stream.mWrite(npc.ID);
            stream.mWrite(npc.World.MapName);
            //write stats & inventory
            Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE, 'G', client.guid, false);
        }

        internal static void ReadControl(BitStream stream, Client client)
        {
            if (client.mainChar == null) // coming from the log-in menus, first spawn
            {
                client.mainChar = client.character;
                Network.Messages.ConnectionMessage.WriteInstanceTables(client);

                if (OnEnterWorld != null)
                {
                    OnEnterWorld(client.mainChar);
                }

                Item item = Item.Create("itfo_apple");
                item.Amount = 3;
                item.Spawn(client.character.World);

                item = Item.Create("itmw_wolfszahn");
                item.Condition = 200;
                item.SpecialLine = "Geschmiedet von Malak Akbar.";
                item.Spawn(client.character.World, new Types.Vec3f(200, 0, 200), new Types.Vec3f(0, 0, 1));

                //NPC scav = NPC.Create("scavenger");
                //scav.Spawn(client.character.World);
            }

            if (!client.character.Spawned)
            {
                client.character.Spawn(client.character.World);
                client.character.WriteSpawn(new Client[1] { client });
            }
        }

        internal static void ReadPickUpItem(BitStream stream, Client client)
        {
            Item item;
            client.character.World.ItemDict.TryGetValue(stream.mReadUInt(), out item);
            if (item == null) return;

            client.character.AddItem(item);
        }

        public delegate void MovementHandler(NPC npc, NPCState state, Vec3f position, Vec3f direction);
        public static event MovementHandler sOnMovement;

        internal static void ReadState(BitStream stream, Client client)
        {
            uint id = stream.mReadUInt();

            NPC npc = null;

            if (id == client.character.ID)
            {
                npc = client.character;
            }
            else
            {
                npc = (NPC)client.VobControlledList.Find(v => v.ID == id && v is NPC);
            }

            if (npc != null)
            {
                NPCState state = (NPCState)stream.mReadByte();
                Vec3f pos = stream.mReadVec();
                Vec3f dir = stream.mReadVec();

                if (sOnMovement != null)
                {
                    sOnMovement(client.character, state, pos, dir);
                }
            }
        }

        public void DoMovement(NPCState state, Vec3f position, Vec3f direction)
        {
            this.State = state;
            this.pos = position; //FIXME: Position -> cell update
            this.dir = direction;
            NPCMessage.WriteState(this.client.character.cell.SurroundingClients(), this);
        }

        public delegate void TargetMovementHandler(NPC npc, NPC target, NPCState state, Vec3f position, Vec3f direction);
        public static TargetMovementHandler sOnTargetMovement;
        public TargetMovementHandler OnTargetMovement;

        internal static void ReadTargetState(BitStream stream, Client client)
        {
            NPCState state = (NPCState)stream.mReadByte();
            Vec3f pos = stream.mReadVec();
            Vec3f dir = stream.mReadVec();
            NPC target = client.character.World.GetNpcOrPlayer(stream.mReadUInt());

            if (sOnTargetMovement != null)
            {
                sOnTargetMovement(client.character, target, state, pos, dir);
            }
            if (client.character.OnTargetMovement != null)
            {
                client.character.OnTargetMovement(client.character, target, state, pos, dir);
            }
        }

        public void DoTargetMovement(NPCState state, Vec3f position, Vec3f direction, NPC target)
        {
            this.State = state;
            this.pos = position;
            this.dir = direction;
            NPCMessage.WriteTargetState(this.client.character.cell.SurroundingClients(), this, target);
        }

        #endregion

        #region Equipment

        Dictionary<byte, Item> equippedSlots = new Dictionary<byte, Item>();

        /// <summary>
        /// Equip an Item.
        /// </summary>
        public void EquipSlot(byte slot, Item item)
        {
            if (item != null && item.Owner == this)
            {
                Item oldItem;
                if (equippedSlots.TryGetValue(slot, out oldItem))
                {
                    oldItem.Slot = -1;
                    equippedSlots[slot] = item;
                }
                else
                {
                    equippedSlots.Add(slot, item);
                }
                item.Slot = slot;
                NPCMessage.WriteEquipMessage(cell.SurroundingClients(), this, item, slot);
            }
        }

        /// <summary>
        /// Unequip an Item slot.
        /// </summary>
        public void UnequipSlot(byte slot)
        {
            Item item;
            if (equippedSlots.TryGetValue(slot, out item))
            {
                item.Slot = -1;
                equippedSlots.Remove(slot);
                NPCMessage.WriteUnequipMessage(cell.SurroundingClients(), this, slot);
            }
        }

        /// <summary>
        /// Unequip an Item.
        /// </summary>
        public void UnequipItem(Item item)
        {
            if (item.Owner == this && item.Slot != -1)
            {
                UnequipSlot((byte)item.Slot);
            }
        }

        /// <summary>
        /// Get the equipped Item of a slot.
        /// </summary>
        public Item GetEquipment(byte slot)
        {
            Item item = null;
            equippedSlots.TryGetValue(slot, out item);
            return item;
        }

        #endregion

        #region Itemcontainer

        protected Dictionary<ItemInstance, List<Item>> inventory = new Dictionary<ItemInstance, List<Item>>();

        /// <summary>
        /// Gets a list of the items this NPC is carrying.
        /// </summary>
        public List<Item> ItemList
        {
            get
            {
                List<Item> itemList = new List<Item>();
                foreach (List<Item> list in inventory.Values)
                {
                    itemList.AddRange(list);
                }
                return itemList;
            }
        }

        /// <summary>
        /// Gets the total weight of the items this NPC is carrying.
        /// </summary>
        public ushort carryWeight { get; protected set; }

        /// <summary>
        /// Checks whether this NPC has the Item with the given ID.
        /// </summary>
        public bool HasItem(uint itemID)
        {
            Item item = null;
            if (sWorld.ItemDict.TryGetValue(itemID, out item))
            {
                return HasItem(item);
            }
            return false;
        }

        /// <summary>
        /// Checks whether this NPC has the given Item.
        /// </summary>
        public bool HasItem(Item item)
        {
            List<Item> list;
            if (inventory.TryGetValue(item.Instance, out list))
            {
                return list.Contains(item);
            }
            return false;
        }

        /// <summary>
        /// Checks whether this NPC an Item of the given ItemInstance.
        /// Only stackable Items are considered, i.e. no unique items like user-written scrolls, worn weapons etc.
        /// </summary>
        public bool HasItem(ItemInstance instance)
        {
            return HasItem(instance, 1);
        }

        /// <summary>
        /// Checks whether this NPC has the amount of Items of the given ItemInstance.
        /// Only stackable Items are considered, i.e. no unique items like user-written scrolls, worn weapons etc.
        /// </summary>
        public bool HasItem(ItemInstance instance, ushort amount)
        {
            List<Item> list;
            if (inventory.TryGetValue(instance, out list))
            {
                Item item = list.Find(i => i.Stackable == true);
                if (item != null)
                {
                    if (item.amount >= amount)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Tries to add the given Item to the NPC's inventory, depending on his carry weight limit.
        /// Stackable items may be deleted by this method!
        /// </summary>
        public bool AddItem(Item item)
        {
            int newWeight = carryWeight + item.amount * item.Instance.Weight;
            if (newWeight <= AttrCapacity)
            {
                carryWeight = (ushort)newWeight;
                if (item.Spawned)
                {
                    item.Despawn(); //Fixme?: Send despawn + additem msg in one msg to the new owner
                }
                //else
                if (item.Owner != null)
                {
                    item.Owner.RemoveItem(item);
                }

                List<Item> list;
                if (inventory.TryGetValue(item.Instance, out list))
                {
                    if (item.Stackable)
                    {
                        Item other = list.Find(i => i.Stackable == true);
                        if (other != null) //merge the items
                        {
                            other.amount += item.amount;
                            item.RemoveFromServer();
                            InventoryMessage.WriteAmountUpdate(this.client, other);
                            item.Owner = this;
                            return true;
                        }
                    }
                }
                else
                {
                    list = new List<Item>(1);
                    inventory.Add(item.Instance, list);
                }
                list.Add(item);
                InventoryMessage.WriteAddItem(this.client, item);
                item.Owner = this;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the given Item from the NPC's inventory if he owns it.
        /// If you want to delete the Item call "item.RemoveFromServer()" instead.
        /// </summary>
        public void RemoveItem(Item item)
        {
            if (item.Owner == this)
            {
                item.Owner = null;
                List<Item> list;
                if (inventory.TryGetValue(item.Instance, out list))
                {
                    list.Remove(item);
                    if (list.Count == 0)
                    {
                        inventory.Remove(item.Instance);
                    }
                }

                InventoryMessage.WriteAmountUpdate(this.client, item, 0);
            }
        }

        /// <summary>
        /// Removes one Item of the given ItemInstance from the NPC's inventory.
        /// Only stackable Items are considered, i.e. no unique items like user-written scrolls, worn weapons etc.
        /// </summary>
        public void RemoveItem(ItemInstance instance)
        {
            RemoveItem(instance, 1);
        }

        /// <summary>
        /// Removes the amount of Items of the given ItemInstance from the NPC's inventory.
        /// Only stackable Items are considered, i.e. no unique items like user-written scrolls, worn weapons etc.
        /// </summary>
        public void RemoveItem(ItemInstance instance, ushort amount)
        {
            List<Item> list;
            if (inventory.TryGetValue(instance, out list))
            {
                Item item = list.Find(i => i.Stackable == true);
                if (item != null)
                {
                    int newAmount = item.amount - amount;
                    if (newAmount > 0)
                    {
                        item.amount = (ushort)newAmount;
                        carryWeight = (ushort)(carryWeight - item.amount * item.Instance.Weight);

                        InventoryMessage.WriteAmountUpdate(this.client, item);
                    }
                    else
                    {
                        item.RemoveFromServer();
                    }
                }
            }
        }

        #endregion

        #region Events
        public delegate void OnHitHandler(NPC attacker, NPC target);
        public OnHitHandler OnHit;
        public static OnHitHandler sOnHit;

        internal void DoHit(NPC attacker)
        {
            if (sOnHit != null)
            {
                sOnHit(attacker, this);
            }
            if (OnHit != null)
            {
                OnHit(attacker, this);
            }
        }
        #endregion

        public void DoUseMob(MobInter mob)
        {
            if (mob != null)
            {
                UsedMob = mob;

                BitStream stream = Program.server.SetupStream(NetworkID.MobUseMessage);
                stream.mWrite(this.ID);
                stream.mWrite(mob.ID);

                foreach (Client cl in this.cell.SurroundingClients())
                {
                    Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', cl.guid, false);
                }
            }
        }

        public void DoUnUseMob()
        {
            if (UsedMob != null)
            {
                UsedMob = null;

                BitStream stream = Program.server.SetupStream(NetworkID.MobUnUseMessage);
                stream.mWrite(this.ID);

                foreach (Client cl in this.cell.SurroundingClients())
                {
                    Program.server.ServerInterface.Send(stream, PacketPriority.LOW_PRIORITY, PacketReliability.RELIABLE_ORDERED, 'W', cl.guid, false);
                }
            }
        }

    }
}

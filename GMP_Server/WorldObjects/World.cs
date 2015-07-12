﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Enumeration;
using GUC.Server.Network;
using GUC.Server.Network.Messages;

namespace GUC.Server.WorldObjects
{
    public class World
    {
        //Worlds, hardcoded but whatever
        private static World newworld = new World("NEWWORLD\\NEWWORLD.ZEN");
        public static World NewWorld { get { return newworld; } }

        public string MapName { get; protected set; }

        internal Dictionary<int, Dictionary<int, WorldCell>> Cells = new Dictionary<int, Dictionary<int, WorldCell>>();

        Dictionary<uint, NPC> playerDict = new Dictionary<uint, NPC>();
        Dictionary<uint, NPC> npcDict = new Dictionary<uint, NPC>();
        Dictionary<uint, Item> itemDict = new Dictionary<uint, Item>();
        Dictionary<uint, Vob> vobDict = new Dictionary<uint, Vob>();

        public Dictionary<uint, NPC> PlayerDict { get { return playerDict; } }
        public Dictionary<uint, NPC> NPCDict { get { return npcDict; } }
        public Dictionary<uint, Item> ItemDict { get { return itemDict; } }
        public Dictionary<uint, Vob> VobDict { get { return vobDict; } }

        public World(string mapname)
        {
            MapName = mapname;
            sWorld.WorldList.Add(this);
        }

        internal void AddVob(AbstractVob vob)
        {
            if (vob is NPC)
            {
                if (((NPC)vob).isPlayer)
                    playerDict.Add(vob.ID, (NPC)vob);
                else
                    npcDict.Add(vob.ID, (NPC)vob);
            }
            else if (vob is Item)
            {
                itemDict.Add(vob.ID, (Item)vob);
            }
            else if (vob is Vob)
            {
                vobDict.Add(vob.ID,(Vob)vob);
            }
            vob.World = this;
        }

        internal void RemoveVob(AbstractVob vob)
        {
            if (vob is NPC)
            {
                if (((NPC)vob).isPlayer)
                    playerDict.Remove(vob.ID);
                else
                    npcDict.Remove(vob.ID);
            }
            else if (vob is Item)
            {
                itemDict.Remove(vob.ID);
            }
            else
            {
                vobDict.Remove(vob.ID);
            }
            vob.World = null;
        }

        #region Spawn
        public void SpawnVob(AbstractVob vob)
        {
            if (vob.World != null)
            {
                vob.World.DespawnVob(vob);
            }
            AddVob(vob);
            UpdatePosition(vob, vob.ClientOrNull);
        }

        public void DespawnVob(AbstractVob vob)
        {
            if (vob.World != this)
            {
                return;
            }

            RemoveVob(vob);
            if (vob.cell != null)
            {
                vob.WriteDespawn(vob.cell.SurroundingClients());
                vob.cell.RemoveVob(vob);
            }
        }

        #endregion

        #region WorldCells
        internal void UpdatePosition(AbstractVob vob, Client exclude)
        {
            float unroundedX = vob.pos.X / WorldCell.cellSize;
            float unroundedZ = vob.pos.Z / WorldCell.cellSize;

            //calculate new cell indices
            int x = (int)(vob.pos.X >= 0 ? unroundedX + 0.5f : unroundedX - 0.5f);
            int z = (int)(vob.pos.Z >= 0 ? unroundedZ + 0.5f : unroundedZ - 0.5f);

            if (vob.cell == null)
            { //Vob has not been in the world yet
                ChangeCells(vob, x, z, exclude);
            }
            else
            {
                //vob moved to a new cell
                if (vob.cell.x != x || vob.cell.z != z)
                {
                    //check whether we're at least > 20% inside: 0.5f == between 2 cells
                    float xdiff = unroundedX - vob.cell.x;
                    float zdiff = unroundedZ - vob.cell.z;
                    if ((xdiff > 0.65f || xdiff < -0.65f) || (zdiff > 0.65f || zdiff < -0.65f))
                    {
                        ChangeCells(vob, x, z, exclude);
                        return;
                    }
                }

                //still in the old cell, updates for everyone!
                VobMessage.WritePosition(vob.cell.SurroundingClients(exclude), vob);
            }
        }

        void ChangeCells(AbstractVob vob, int x, int z, Client exclude)
        {
            //create the new cell
            if (!Cells.ContainsKey(x))
            {
                Cells.Add(x, new Dictionary<int, WorldCell>());
            }

            if (!Cells[x].ContainsKey(z))
            {
                Cells[x].Add(z, new WorldCell(this, x, z));
            }

            WorldCell newCell = Cells[x][z];

            if (vob.cell == null)
            {
                if (exclude != null)
                {
                    VobMessage.WritePosition(new Client[1] {exclude}, vob);
                }
                //Send creation message to all players in surrounding cells
                vob.WriteSpawn(newCell.SurroundingClients(exclude));

                //send all vobs in surrounding cells to the player
                if (vob is NPC && ((NPC)vob).isPlayer)
                {
                    foreach (WorldCell c in newCell.SurroundingCells())
                    {
                        foreach (AbstractVob v in c.AllVobs())
                        {
                            v.WriteSpawn(new Client[1] { ((NPC)vob).client });
                        }
                    }
                }
            }
            else
            {
                VobChangeDiffCells(vob, vob.cell, newCell, exclude);
                vob.cell.RemoveVob(vob);
            }
            newCell.AddVob(vob);
        }

        void VobChangeDiffCells(AbstractVob vob, WorldCell from, WorldCell to, Client exclude)
        {
            int i, j;
            WorldCell cell;
            Dictionary<int, WorldCell> row;

            for (i = from.x - 1; i <= from.x + 1; i++)
            {
                row = null;
                Cells.TryGetValue(i, out row);
                if (row == null) continue;

                for (j = from.z - 1; j <= from.z + 1; j++)
                {
                    cell = null;
                    row.TryGetValue(j, out cell);
                    if (cell == null) continue;

                    if (i <= to.x + 1 && i >= to.x - 1 && j <= to.z + 1 && j >= to.z - 1)
                    {
                         //Position updates in shared cells
                         VobMessage.WritePosition(cell.GetClients(exclude), vob);
                    }
                    else
                    {
                        //deletion updates in old cells
                        vob.WriteDespawn(cell.GetClients());

                        //deletion updates for the player
                        if (vob is NPC && ((NPC)vob).isPlayer)
                        {
                            foreach (AbstractVob v in cell.AllVobs())
                            {
                                v.WriteDespawn(new Client[1] {((NPC)vob).client});
                            }
                        }
                    }
                }
            }

            for (i = to.x - 1; i <= to.x + 1; i++)
            {
                row = null;
                Cells.TryGetValue(i, out row);
                if (row == null) continue;

                for (j = to.z - 1; j <= to.z + 1; j++)
                {
                    cell = null;
                    row.TryGetValue(j, out cell);
                    if (cell == null) continue;

                    if (i <= from.x + 1 && i >= from.x - 1 && j <= from.z + 1 && j >= from.z - 1)
                        continue;

                    if (exclude != null)
                    {
                        VobMessage.WritePosition(new Client[1] { exclude }, vob);
                    }

                    //creation updates in the new cells
                    vob.WriteSpawn(cell.GetClients(exclude));

                    //creation updates for the player
                    if (vob is NPC && ((NPC)vob).isPlayer)
                    {
                        foreach (AbstractVob v in cell.AllVobs())
                        {
                            v.WriteSpawn(new Client[1] {((NPC)vob).client});
                        }
                    }
                }
            }
        }

        #endregion
    }
}

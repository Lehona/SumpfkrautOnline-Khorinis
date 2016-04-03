﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Network;

namespace GUC.Scripts.Sumpfkraut.WorldSystem
{
    public partial class WorldInst : ScriptObject, WorldObjects.World.IScriptWorld
    {
        WorldObjects.World baseWorld;
        public WorldObjects.World BaseWorld { get { return baseWorld; } }

        WorldDef definition = null;
        public WorldDef Definition { get { return definition; } }

        ScriptClock clock;
        public ScriptClock Clock { get { return this.clock; } }

        ScriptSkyCtrl skyCtrl;
        public ScriptSkyCtrl SkyCtrl { get { return this.skyCtrl; } }

        public WorldInst()
        {
            this.baseWorld = new WorldObjects.World();
            this.baseWorld.ScriptObject = this;

            this.clock = new ScriptClock(this);
            this.skyCtrl = new ScriptSkyCtrl(this);
        }

        public void OnWriteProperties(PacketWriter stream)
        {
            // write definition id
        }

        public void OnReadProperties(PacketReader stream)
        {
            // read definition id
        }

        partial void pCreate();
        public void Create()
        {
            this.baseWorld.Create();
            pCreate();
        }

        partial void pDelete();
        public void Delete()
        {
            this.baseWorld.Delete();
            pDelete();
        }
    }
}
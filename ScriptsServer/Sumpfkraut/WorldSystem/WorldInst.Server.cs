﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUC.Scripts.Sumpfkraut.WorldSystem
{
    public partial class WorldInst
    {
        public static WorldInst Current;



        public WorldInst (WorldDef def)
            : this("WorldInst (default)")
        { }

        public WorldInst (WorldDef def, string objName) 
            : this(objName)
        {
            this.definition = def;
        }



        partial void pCreate()
        {
            skyCtrl.StartRainTimer();
        }

        partial void pDelete()
        {
            skyCtrl.StopRainTimer();
        }
    }
}

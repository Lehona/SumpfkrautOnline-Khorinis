﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Scripts.Sumpfkraut.VobSystem.Definitions;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Instances
{
    public partial class ItemInst
    {
        public ItemInst(ItemDef def) : base(def, new WorldObjects.Item())
        {
        }
    }
}
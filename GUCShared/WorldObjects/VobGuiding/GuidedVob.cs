﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Network;
using GUC.Types;
using GUC.WorldObjects.Instances;

namespace GUC.WorldObjects.VobGuiding
{
    public abstract partial class GuidedVob : GUCBaseVobInst
    {
        #region Constructors

        public GuidedVob(IScriptBaseVob scriptObject) : base(scriptObject)
        {
        }

        #endregion

        #region Properties

        GuideCmd currentCmd;
        public GuideCmd CurrentCommand { get { return this.currentCmd; } }

        internal GameClient guide;
        public GameClient Guide { get { return this.guide; } }

        #endregion

        #region Spawn & Despawn

        partial void pSpawn(World world, Vec3f position, Angles angles);
        public override void Spawn(World world, Vec3f position, Angles angles)
        {
            base.Spawn(world, position, angles);
            pSpawn(world, position, angles);
        }

        partial void pDespawn();
        public override void Despawn()
        {
            pDespawn();
            base.Despawn();
        }

        #endregion
    }
}

﻿using System;
using GUC.WorldObjects.Instances;

using GUC.Scripts.Sumpfkraut.VobSystem.Definitions;
using GUC.Scripts.Sumpfkraut.Visuals;
using GUC.Types;
using GUC.Scripts.Sumpfkraut.WorldSystem;
using GUC.Scripts.Sumpfkraut.EffectSystem.EffectHandlers;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Instances
{
    public partial class ProjInst : BaseVobInst, GUCProjectileInst.IScriptProjectile
    {
        #region Properties

        public override VobType VobType { get { return VobType.Projectile; } }

        new public GUCProjectileInst BaseInst { get { return (GUCProjectileInst)base.BaseInst; } }
        new public ProjDef Definition { get { return (ProjDef)base.Definition; } set { base.Definition = value; } }

        public ModelDef Model { get { return (ModelDef)BaseInst.Model?.ScriptObject; } set { BaseInst.Model = value?.BaseDef; } }
        public float Velocity { get { return BaseInst.Velocity; } set { BaseInst.Velocity = value; } }
        public Vec3f Destination { get { return this.BaseInst.Destination; } set { this.BaseInst.Destination = value; } }

        #endregion

        #region Constructors

        partial void pConstruct();
        public ProjInst()
        {
            pConstruct();
        }

        protected override GUCBaseVobInst CreateVob()
        {
            return new GUCProjectileInst(this);
        }

        protected override BaseEffectHandler CreateHandler()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Spawn / Despawn

        partial void pSpawn(WorldInst world, Vec3f pos, Angles ang);
        public override void Spawn(WorldInst world, Vec3f pos, Angles ang)
        {
            base.Spawn(world, pos, ang);
            pSpawn(world, pos, ang);
        }

        partial void pDespawn();
        public override void Despawn()
        {
            base.Despawn();
            pDespawn();
        }

        #endregion

        partial void pUpdatePos();
        public void UpdatePos()
        {
            pUpdatePos();
        }

        partial void pOnEndPos();
        public void OnEndPos()
        {
            pOnEndPos();
        }
    }
}

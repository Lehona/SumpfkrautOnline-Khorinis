﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.WorldObjects.Instances;
using GUC.Scripts.Sumpfkraut.Visuals;
using GUC.Scripts.Sumpfkraut.EffectSystem.EffectHandlers;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Definitions
{
    public partial class VobDef : BaseVobDef, GUCVobDef.IScriptVobInstance
    {
        #region Constructors

        partial void pConstruct();
        public VobDef()
        {
            pConstruct();
        }

        protected override BaseEffectHandler CreateHandler()
        {
            return new VobDefEffectHandler(null, null, this);
        }

        protected override GUCBaseVobDef CreateVobInstance()
        {
            return new GUCVobDef(this);
        }

        #endregion

        #region Properties

        public override VobType VobType { get { return VobType.Vob; } }

        new public VobDefEffectHandler EffectHandler { get { return (VobDefEffectHandler)base.EffectHandler; } }
        new public GUCVobDef BaseDef { get { return (GUCVobDef)base.BaseDef; } }
        
        public ModelDef Model
        {
            get { return (ModelDef)this.BaseDef.ModelInstance.ScriptObject; }
            set { this.BaseDef.ModelInstance = value.BaseDef; }
        }

        public bool CDDyn { get { return BaseDef.CDDyn; } set { BaseDef.CDDyn = value; } }
        public bool CDStatic { get { return BaseDef.CDStatic; } set { BaseDef.CDStatic = value; } }

        #endregion

    }
}

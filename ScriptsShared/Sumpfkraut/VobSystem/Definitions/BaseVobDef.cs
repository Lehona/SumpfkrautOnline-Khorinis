﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GUC.WorldObjects.Instances;
using GUC.Network;
using GUC.Enumeration;
using GUC.WorldObjects.Collections;

namespace GUC.Scripts.Sumpfkraut.VobSystem.Definitions
{
    public abstract partial class BaseVobDef : ScriptObject, BaseVobInstance.IScriptBaseVobInstance
    {
        #region Properties

        BaseVobInstance baseDef;
        public BaseVobInstance BaseDef { get { return baseDef; } }

        public int ID { get { return baseDef.ID; } }
        public VobTypes VobType { get { return baseDef.VobType; } }
        public bool IsStatic { get { return baseDef.IsStatic; } }

        #endregion

        protected BaseVobDef(BaseVobInstance baseDef)
        {
            if (baseDef == null)
                throw new ArgumentNullException("BaseDef is null!");

            this.baseDef = baseDef;
            this.baseDef.ScriptObject = this;
        }

        partial void pCreate();
        public void Create()
        {
            this.baseDef.Create();
            pCreate();
        }

        partial void pDelete();
        public void Delete()
        {
            this.baseDef.Delete();
            pDelete();
        }

        public virtual void OnWriteProperties(PacketWriter stream) { }
        public virtual void OnReadProperties(PacketReader stream) { }
    }
}
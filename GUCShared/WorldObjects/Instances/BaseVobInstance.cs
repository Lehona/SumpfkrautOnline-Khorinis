﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.GameObjects;
using GUC.WorldObjects.Collections;
using GUC.GameObjects.Collections;
using GUC.Types;

namespace GUC.WorldObjects.Instances
{
    /// <summary>
    /// A VobInstance is used to define a Vob's default settings. 
    /// </summary>
    public abstract partial class BaseVobInstance : IDObject, VobTypeObject
    {
        public abstract VobTypes VobType { get; }

        #region ScriptObject

        public partial interface IScriptBaseVobInstance : IScriptGameObject
        {
            void Create();
            void Delete();
        }
        
        /// <summary> The ScriptObject of this object. </summary>
        public new IScriptBaseVobInstance ScriptObject { get { return (IScriptBaseVobInstance)base.ScriptObject; } }

        #endregion

        #region Constructors

        public BaseVobInstance(IScriptBaseVobInstance scriptObject) : base(scriptObject)
        {
        }

        #endregion

        #region Properties

        /// <summary> Checks whether this Instance is added to the static Instance collection. </summary>
        public bool IsCreated { get { return this.isCreated; } }

        #endregion

        #region Static Collection

        internal int collTypeID = -1;
        internal int dynTypeID = -1;

        static StaticCollection<BaseVobInstance> idColl = new StaticCollection<BaseVobInstance>();

        static VobTypeCollection<BaseVobInstance> instances = new VobTypeCollection<BaseVobInstance>();
        static VobTypeCollection<BaseVobInstance> dynInstances = new VobTypeCollection<BaseVobInstance>();

        #region Create & Delete

        partial void pAfterCreate();
        /// <summary> Adds this Instance to the static Instance collection. </summary>
        public virtual void Create()
        {
            if (this.isCreated)
                throw new ArgumentException("Instance is already in the collection!");
            
            idColl.Add(this);
            
            instances.Add(this, ref this.collID, ref this.collTypeID);

            if (!this.IsStatic)
            {
                dynInstances.Add(this, ref this.dynID, ref this.dynTypeID);
            }

            this.isCreated = true;

            pAfterCreate();
        }

        partial void pBeforeDelete();
        /// <summary> Removes this Instance from the static Instance collection. </summary>
        public virtual void Delete()
        {
            if (!this.isCreated)
                throw new ArgumentException("Instance is not in the collection!");
            
            pBeforeDelete();

            this.isCreated = false;

            idColl.Remove(this);
            instances.Remove(this, ref this.collID, ref this.collTypeID);

            if (!this.IsStatic)
            {
                dynInstances.Remove(this, ref this.dynID, ref this.dynTypeID);
            }
        }

        #endregion

        #region Access

        /// <summary> Gets any Instance with the given ID or null from the static Instance collection. </summary>
        public static bool TryGet(int id, out BaseVobInstance instance)
        {
            return idColl.TryGet(id, out instance);
        }

        /// <summary> Gets an Instance of a specific type with the given ID or null from the static Instance collection. </summary>
        public static bool TryGet<T>(int id, out T instance) where T : BaseVobInstance
        {
            return idColl.TryGet(id, out instance);
        }

        /// <summary> Loops through all Instances in the static Instance collection. </summary>
        public static void ForEach(Action<BaseVobInstance> action)
        {
            instances.ForEach(action);
        }

        /// <summary> Gets the count of all Instances in the static Instance collection. </summary>
        public static int GetCount()
        {
            return instances.GetCount();
        }

        /// <summary> Loops through all Instances of the given type in the static Instance collection. </summary>
        public static void ForEachOfType(VobTypes type, Action<BaseVobInstance> action)
        {
            instances.ForEachOfType(type, action);
        }

        /// <summary> Gets the count of all Instances of the given type in the static Instance collection. </summary>
        public static int GetCountOfType(VobTypes type)
        {
            return instances.GetCountOfType(type);
        }

        public static void ForEachDynamic(Action<BaseVobInstance> action)
        {
            dynInstances.ForEach(action);
        }

        public static void ForEachDynamicOfType(VobTypes type, Action<BaseVobInstance> action)
        {
            dynInstances.ForEachOfType(type, action);
        }

        public static int GetCountDynamics()
        {
            return dynInstances.GetCount();
        }

        public static int GetCountDynamicsOfType(VobTypes type)
        {
            return dynInstances.GetCountOfType(type);
        }

        #endregion

        #endregion
    }
}

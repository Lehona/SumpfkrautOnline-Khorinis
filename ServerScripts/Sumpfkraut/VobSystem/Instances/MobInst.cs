﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Server.Scripting.Objects;
using GUC.Server.Scripting.Objects.Mob;
using GUC.Server.Scripts.Sumpfkraut.VobSystem.Definitions;
using GUC.Types;

namespace GUC.Server.Scripts.Sumpfkraut.VobSystem.Instances
{

    /**
     *   Class which handles mob creation.
     */
    class MobInst : VobInst
    {

        // definition on which basis the item was created
        private MobDef vobDef;
        public MobDef getVobDef () { return this.vobDef; }
        public void setVobDef (MobDef vobDef) { this.vobDef = vobDef; }

        // the ingame-item created by using itemDef
        private MobInter vob;
        public MobInter getVob () { return this.vob; }
        public void setVob (MobInter vob) { this.vob = vob; }
       
        // current world where the instance is in
        // (always set inWorld and position at the same time)
        private WorldInst inWorld;
        public WorldInst getInWorld () { return inWorld; }
        public void setInWorld (WorldInst inWorld) 
        { 
            this.inWorld = inWorld;
            // despawn and spawn again to swtich worlds ingame
            this.DespawnVob();
            this.SpawnVob();
        }

        // cartesian position in current world 
        // (always set inWorld and position at the same time)
        private Vec3f position;
        public Vec3f getPosition () { return this.position; }
        public void setPosition (Vec3f position) 
        { 
            this.position = position;
            if (this.vob != null)
            {
                Vob vob = this.vob;
                vob.setPosition(position);
            }          
        }

        private Vec3f direction;
        public Vec3f getDirection () { return this.direction; }
        public void setDirection (Vec3f direction) 
        {
            this.direction = direction;
            if (this.vob != null)
            {
                Vob vob = this.vob;
                vob.setDirection(direction);
            }   
        }

        private List<ItemInst> inventoryItems = new List<ItemInst>();
        public List<ItemInst> getInventoryItems () { return this.inventoryItems; }
        public void setInventoryItems (List<ItemInst> inventoryItems) 
        { 
            this.inventoryItems = inventoryItems; 
        }

        // TO DO: MobInst (MobDef def) --> requires a default world and, thus, the world management beforehand

        public MobInst (MobDef def, WorldInst inWorld)
            : this(def, inWorld, new Vec3f(0, 0, 0))
        { }

        public MobInst (MobDef def, WorldInst inWorld, Vec3f position)
            : this(def, inWorld, position, new Vec3f(0, 0, 0))
        { }

        public MobInst (MobDef def, WorldInst inWorld, Vec3f position, Vec3f direction)
        {
            this.setVobDef(def);
            MobInter newVob = null;
            MobInterType mobType = def.getMobInterType();

            // need to despawn newly created vobs because, maybe, they shouldn not be spawned at
            // this point, however, the GUC does not allow so at the moment
            switch (mobType)
            {
                case MobInterType.MobBed:
                    newVob = new MobBed(def.getVisual(), def.getFocusName(), def.getUseWithItem(), 
                        def.getTriggerTarget(), def.getCDDyn(), def.getCDStatic());
                    newVob.Despawn();
                    break;
                case MobInterType.MobContainer:
                    newVob = new MobContainer(def.getVisual(), def.getFocusName(), new ItemInstance[0], 
                        new int[0], def.getIsLocked(), def.getKeyInstance(), def.getPicklockString(), 
                        def.getUseWithItem(), def.getTriggerTarget(), def.getCDDyn(), def.getCDStatic());
                    newVob.Despawn();
                    break;
                case MobInterType.MobDoor:
                    newVob = new MobDoor(def.getVisual(), def.getFocusName(), def.getIsLocked(), 
                        def.getKeyInstance(), def.getPicklockString(), def.getUseWithItem(), 
                        def.getTriggerTarget(), def.getCDDyn(), def.getCDStatic());
                    newVob.Despawn();
                    break;
                case MobInterType.MobInter:
                    newVob = new MobInter(def.getVisual(), def.getFocusName(), def.getUseWithItem(), 
                        def.getTriggerTarget(), def.getCDDyn(), def.getCDStatic());
                    newVob.Despawn();
                    break;
                case MobInterType.MobSwitch:
                    newVob = new MobSwitch(def.getVisual(), def.getFocusName(), def.getUseWithItem(), 
                        def.getTriggerTarget(), def.getCDDyn(), def.getCDStatic());
                    newVob.Despawn();
                    break;
                case MobInterType.None:
                    break;
                default:
                    Log.Logger.logWarning("MobInst (constr): No valid MobInterType was provided on "
                        + "Mob-instantiation.");
                    break;
            }

            if (newVob != null)
            {
                this.setVob(newVob);
            }

            this.setInWorld(inWorld);
            this.setPosition(position);
            this.setDirection(direction);
        }


        public void CreateVob ()
        {
            //if (this.vob != null)
            //{
            //    this.DeleteVob();
            //}
            if (this.getVobDef() != null)
            {
                //this.vob = new Item(this.getVobDef(), this.getAmount());

                // TO DO

            }
        }

        // MobInter does not seem to support Delete-method
        //public void DeleteVob ()
        //{
        //    if (this.vob != null)
        //    {
        //        this.vob.Delete();
        //    } 
        //}

        public void SpawnVob ()
        {
            if (this.vob != null)
            {
                this.vob.Spawn(this.getInWorld().getWorldName(), this.getPosition(), this.getDirection());
                setIsSpawned(true);
            }  
        }

        public void DespawnVob ()
        {
            if (this.vob != null)
            {
                this.vob.Despawn();
                setIsSpawned(false);
            }  
        }

    }
}

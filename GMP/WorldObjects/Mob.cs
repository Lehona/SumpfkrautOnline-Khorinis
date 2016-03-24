﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gothic.zClasses;
using Gothic.zTypes;

namespace GUC.Client.WorldObjects
{
    public class Vob : AbstractVob
    {
        public MobInstance Instance { get; protected set; }

        public string Visual { get { return Instance.visual; } }

        public bool CDDyn { get { return Instance.cdDyn; } }
        public bool CDStatic { get { return Instance.cdStatic; } }

        public Vob(uint id, ushort instanceID)
            : base(id)
        {
            Instance = MobInstance.Table.Get(instanceID);
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = zCVob.Create(Program.Process);
            }
            SetProperties();
        }

        protected virtual void SetProperties()
        {
            gVob.BitField1 |= (int)zCVob.BitFlag0.staticVob;
            gVob.SetVisual(Visual);

            if (CDDyn) gVob.BitField1 |= (int)zCVob.BitFlag0.collDetectionDynamic;
            else gVob.BitField1 &= ~(int)zCVob.BitFlag0.collDetectionDynamic;

            if (CDStatic) gVob.BitField1 |= (int)zCVob.BitFlag0.collDetectionStatic;
            else gVob.BitField1 &= ~(int)zCVob.BitFlag0.collDetectionStatic;
        }
    }

    public class Mob : Vob
    {
        public oCMob gMob { get { return new oCMob(Program.Process, gVob.Address); } }

        public string FocusName { get { return Instance.focusName; } }

        public Mob(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMob.Create(Program.Process);
            }
            SetProperties();
        }
    }

    public class MobInter : Mob
    {
        public oCMobInter gMobInter { get { return new oCMobInter(Program.Process, gVob.Address); } }

        public string onTriggerClientFunc { get { return Instance.onTriggerClientFunc; } }

        public MobInter(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobInter.Create(Program.Process);
            }
            SetProperties();
        }
    }

    public class MobFire : MobInter
    {
        string fireVobTreeName { get { return Instance.fireVobTreeName; } }

        public MobFire(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobFire.Create(Program.Process);
            }
            SetProperties();
        }

        protected override void SetProperties()
        {
            base.SetProperties();

            //set fireVobTreeName
        }
    }

    public class MobLadder : MobInter
    {
        public MobLadder(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobLadder.Create(Program.Process);
            }
            SetProperties();
        }
    }

    public class MobSwitch : MobInter
    {
        public MobSwitch(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobSwitch.Create(Program.Process);
            }
            SetProperties();
        }
    }

    public class MobWheel : MobInter
    {
        public MobWheel(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobWheel.Create(Program.Process);
            }
            SetProperties();
        }
    }

    public abstract class MobLockable : MobInter
    {
        string OnTryOpenClientFunc { get { return Instance.onTryOpenClientFunc; } }

        public MobLockable(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }
    }

    public class MobContainer : MobLockable
    {
        public MobContainer(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobContainer.Create(Program.Process);
            }
            SetProperties();
        }
    }

    public class MobDoor : MobLockable
    {
        public MobDoor(uint id, ushort instanceID)
            : base(id, instanceID)
        {
        }

        protected override void CreateVob(bool createNew)
        {
            if (createNew)
            {
                gVob = oCMobDoor.Create(Program.Process);
            }
            SetProperties();
        }
    }
}

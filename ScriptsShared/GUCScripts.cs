﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GUC.Scripting;
using GUC.Scripts.Sumpfkraut.Networking;
using GUC.Scripts.Sumpfkraut.Visuals;
using GUC.Enumeration;
using GUC.Scripts.Sumpfkraut.VobSystem.Instances;
using GUC.Scripts.Sumpfkraut.VobSystem.Definitions;
using GUC.Scripts.Sumpfkraut.WorldSystem;

namespace GUC.Scripts
{
    public partial class GUCScripts : ScriptInterface
    {
        public Animations.AniJob CreateAniJob()
        {
            return new ScriptAniJob().BaseAniJob;
        }

        public Animations.Animation CreateAnimation()
        {
            return new ScriptAni().BaseAni;
        }

        public Animations.Overlay CreateOverlay()
        {
            return new ScriptOverlay().BaseOverlay;
        }

        public Models.Model CreateModel()
        {
            return new ModelDef().BaseDef;
        }

        public bool OnClientConnection(Network.GameClient client)
        {
            ScriptClient sc = new ScriptClient(client);
            return true;
        }

        public WorldObjects.World CreateWorld()
        {
            return new WorldInst().BaseWorld;
        }

        public WorldObjects.BaseVob CreateVob(VobTypes type)
        {
            BaseVobInst vob;
            switch (type)
            {
                case VobTypes.Vob:
                    vob = new VobInst();
                    break;
                case VobTypes.Item:
                    vob = new ItemInst();
                    break;
                case VobTypes.NPC:
                    vob = new NPCInst();
                    break;
                default:
                    throw new Exception("Unsupported VobType: " + type);
            }
            return vob.BaseInst;
        }


        public WorldObjects.Instances.BaseVobInstance CreateInstance(VobTypes type)
        {
            BaseVobDef def;
            switch (type)
            {
                case VobTypes.Vob:
                    def = new VobDef();
                    break;
                case VobTypes.NPC:
                    def = new NPCDef();
                    break;
                case VobTypes.Item:
                    def = new ItemDef();
                    break;
                default:
                    throw new Exception("Unsupported VobType: " + type);
            }

            return def.BaseDef;
        }
    }
}
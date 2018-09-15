﻿using System;
using System.Collections.Generic;
using System.Text;
using WinApi;
using Gothic.Types;
using Gothic.Objects.Sky;
using Gothic.View;
using Gothic.Objects.Meshes;

namespace Gothic.Objects
{
    public class zCWorld : zCObject
    {
        public zCWorld(int address)
            : base(address)
        {
        }

        public zCWorld()
        {
        }

        public abstract class FuncAddresses
        {
            public const int RemoveVob = 0x00624B70,
            GetProgressBar = 0x006269C0,
            SetSkyControlerOutdoor = 0x00620410,
            AddVob = 0x00624810,
            TraceRayNearestHit = 0x00621B80,
            TraceRayFirstHit = 0x00621960,
            MergeVobSubtree = 0x006273E0;
        }

        /*public enum HookSize
        {
            RemoveVob = 7,
            GetProgressBar = 6
        }*/

        new public abstract class VarOffsets : zCObject.VarOffsets
        {
            public const int LevelName = 0x0068,
            ActiveVobs = 0x0240,
            WorldFileName = 25176,
            WorldName = 25196,
            globalVobTree = 0x24,
            waynet = 144,
            activeSkyControler = 0xE8,
            skyControlerOutdoor = 0xE4,
            VobList = 0x6280,

            raytrace_foundHit = 0x0038,
            raytrace_foundVob = 0x003C,
            raytrace_foundPoly = 0x0040,
            raytrace_foundIntersection = 0x0044, //zVec[]
            raytrace_foundPolyNormal = 0x0050, // zVec[]
            raytrace_foundVertex = 0x005C,
            m_bIsInventoryWorld = 136;
        }

        [Flags]
        public enum zTraceRay
        {
            Ignore_Vob_No_Collision = 1 << 0,
            Ignore_Vob_All = 1 << 1,
            Test_Vob_BBox = 1 << 2, //Testet nur auf die Boundingbox
            Ignore_Static = 1 << 4, //Nur Vobs
            Return_POLY = 1 << 5, //Gibt Pointer im Return pointer auf Schnittpolygon
            Test_Portals = 1 << 6, //Schnitte mit Portalen ebenfalls werten
            Return_Normal = 1 << 7,
            Ignore_Alpha = 1 << 8,
            Test_Water = 1 << 9,
            Test_2Sided = 1 << 10,
            Ignore_NPC = 1 << 11,
            FirstHit = 1 << 12, //nicht unbedingt der nächste sondern Irgendein Schnittpunkt
            Test_HelperVisuals = 1 << 13,
            Ignore_Projectiles = 1 << 14
        }

        public Dictionary<zCVob.gVobTypes, List<zCVob>> getVobLists(params zCVob.gVobTypes[] vobTypes)
        {
            Dictionary<zCVob.gVobTypes, List<zCVob>> rDic = new Dictionary<zCVob.gVobTypes, List<zCVob>>();

            getAllVobObjects(ref rDic, this.GlobalVobTree, vobTypes);

            return rDic;
        }

        public List<zCVob> getVobList(zCVob.gVobTypes vobType)
        {
            Dictionary<zCVob.gVobTypes, List<zCVob>> rDic = new Dictionary<zCVob.gVobTypes, List<zCVob>>();

            getAllVobObjects(ref rDic, this.GlobalVobTree, vobType);

            if (rDic.ContainsKey(vobType))
                return rDic[vobType];

            return new List<zCVob>();
        }

        public List<zCVob> getItemList(zCVob.gVobTypes vobType)
        {
            List<zCVob> vobs = new List<zCVob>();

            zCListSort<zCVob> vobList = this.VobList;

            do
            {
                zCVob vob = vobList.Data;

                //Check if vob is item
                if (vob == null || vob.Address == 0 || vob.VTBL != vobType)
                    continue;

                //add the vob to the itemlist
                vobs.Add(vob);

            } while ((vobList = vobList.Next).Address != 0);

            return vobs;
        }




        private void getAllVobObjects(ref Dictionary<zCVob.gVobTypes, List<zCVob>> list, zCTree<zCVob> tree, params zCVob.gVobTypes[] types)
        {
            do
            {

                if (tree.Data != null && tree.Data.Address != 0)
                {

                    zCVob.gVobTypes type = tree.Data.VTBL;
                    bool isInList = false;
                    foreach (zCVob.gVobTypes vt in types)
                    {
                        if (vt == type)
                        {
                            isInList = true;
                            break;
                        }
                    }

                    if (isInList)
                    {
                        if (!list.ContainsKey(type))
                            list.Add(type, new List<zCVob>());
                        list[type].Add(tree.Data);
                    }
                }

                if (tree.FirstChild != null && tree.FirstChild.Address != 0)
                {
                    getAllVobObjects(ref list, tree.FirstChild, types);
                }


            } while ((tree = tree.Next).Address != 0);
        }


        public List<zCVob> getVobList()
        {
            List<zCVob> rDic = new List<zCVob>();

            getAllVobObjects(ref rDic, this.GlobalVobTree);

            return rDic;
        }
        private void getAllVobObjects(ref List<zCVob> list, zCTree<zCVob> tree)
        {
            do
            {

                if (tree.Data != null && tree.Data.Address != 0)
                {
                    list.Add(tree.Data);
                }

                if (tree.FirstChild != null && tree.FirstChild.Address != 0)
                {
                    getAllVobObjects(ref list, tree.FirstChild);
                }


            } while ((tree = tree.Next).Address != 0);
        }

        public bool IsInventoryWorld
        {
            get { return Process.ReadBool(Address + VarOffsets.m_bIsInventoryWorld); }
            set { Process.Write(Address + VarOffsets.m_bIsInventoryWorld, value); }
        }

        public bool DrawVobBBox3D
        {
            get { return Process.ReadBool(Address + 0x224); }
            set { Process.Write(Address + 0x224, value); }
        }

        public int BspTreeMode
        {
            get { return Process.ReadInt(Address + 0x204); }
            set { Process.Write(Address + 0x204, value); }
        }

        public bool Raytrace_FoundHit
        {
            get { return Process.ReadBool(Address + VarOffsets.raytrace_foundHit); }
        }

        public zCPolygon Raytrace_FoundPoly
        {
            get { return new zCPolygon(Process.ReadInt(Address + VarOffsets.raytrace_foundPoly)); }
        }

        public zCVob Raytrace_FoundVob
        {
            get { return new zCVob(Process.ReadInt(Address + VarOffsets.raytrace_foundVob)); }
        }

        public zVec3 Raytrace_FoundIntersection
        {
            get { return new zVec3(Address + VarOffsets.raytrace_foundIntersection); }
        }

        public zVec3 Raytrace_FoundNormal
        {
            get { return new zVec3(Address + VarOffsets.raytrace_foundPolyNormal); }
        }

        public zCSkyControler ActiveSkyControler
        {
            get { return new zCSkyControler(Process.ReadInt(Address + VarOffsets.activeSkyControler)); }
            set { Process.Write(Address + VarOffsets.activeSkyControler, value.Address); }
        }

        public zCSkyControler_Outdoor SkyControlerOutdoor
        {
            get { return new zCSkyControler_Outdoor(Process.ReadInt(Address + VarOffsets.skyControlerOutdoor)); }
            set { Process.Write(Address + VarOffsets.skyControlerOutdoor, value.Address); }
        }

        public zCTree<zCVob> GlobalVobTree
        {
            get { return new zCTree<zCVob>(Address + VarOffsets.globalVobTree); }
        }

        public zString LevelName
        {
            get { return new zString(Address + VarOffsets.LevelName); }
        }

        public zString WorldFileName
        {
            get { return new zString(Address + VarOffsets.WorldFileName); }
        }

        public zString WorldName
        {
            get { return new zString(Address + VarOffsets.WorldName); }
        }

        /*public zCWayNet WayNet
        {
            get { return new zCWayNet(Process.ReadInt(Address + VarOffsets.waynet)); }
        }*/

        public zCListSort<oCNpc> NPCList
        {
            get
            {
                return new zCListSort<oCNpc>(Address + 25220);
            }
        }

        public zCListSort<oCItem> ItemList
        {
            get
            {
                return new zCListSort<oCItem>(Address + 25224);
            }
        }

        /// <summary>
        /// This function does return a Voblist. The items are included, MobDoors and Containers aren't.
        /// </summary>
        public zCListSort<zCVob> VobList
        {
            get
            {
                return new zCListSort<zCVob>(Address + 0x6280);
            }
        }

        public zCArray<zCVob> ActiveVobList
        {
            get
            {
                return new zCArray<zCVob>(Address + VarOffsets.ActiveVobs);
            }
        }

        public void Render(zCCamera camera)
        {
            Process.THISCALL<NullReturnCall>(Address, 0x621700, camera);
        }

        public zCTree<zCVob> AddVob(zCVob vob)
        {
            return Process.THISCALL<zCTree<zCVob>>(Address, FuncAddresses.AddVob, vob);
        }

        public zCTree<zCVob> AddVobAsChild(zCVob child, zCVob parent)
        {
            return Process.THISCALL<zCTree<zCVob>>(Address, 0x6247F0, child, parent);
        }


        public virtual void RemoveVob(zCVob vob)
        {
            Process.THISCALL<NullReturnCall>(Address, FuncAddresses.RemoveVob, vob);
        }

        public void RemoveVobSubTree(zCVob vob)
        {
            Process.THISCALL<NullReturnCall>(Address, 0x624D60, vob);
        }

        public zCViewProgressBar GetProgressBar()
        {
            return Process.THISCALL<zCViewProgressBar>(Address, FuncAddresses.GetProgressBar);
        }

        public void SetSkyControlerOutdoor(zCSkyControler vob)
        {
            Process.THISCALL<NullReturnCall>(Address, FuncAddresses.SetSkyControlerOutdoor, vob);
        }

        public bool TraceRayNearestHit(zVec3 start, zVec3 ray, zTraceRay rayType)
        {
            return Process.FASTCALL<BoolArg>(Address, start.Address, FuncAddresses.TraceRayNearestHit, ray, new IntArg(0), new IntArg((int)rayType));
        }

        public bool TraceRayFirstHit(zVec3 start, zVec3 ray, zTraceRay rayType)
        {
            return Process.FASTCALL<BoolArg>(Address, start.Address, FuncAddresses.TraceRayFirstHit, ray, new IntArg(0), new IntArg((int)rayType));
        }
        
        public int GetBspTree()
        {
            return Process.THISCALL<IntArg>(Address, 0x6D0020);
        }

        public const int ByteSize = 0x6258;

        public static zCWorld Create()
        {
            int address = Process.CDECLCALL<IntArg>(0x61F9B0); //_CreateInstance()
            //Process.THISCALL<NullReturnCall>(address, 0x61FA40); //Konstruktor...
            return new zCWorld(address);
        }

        public override void Dispose()
        {
            Process.THISCALL<NullReturnCall>(Address, 0x620080, (BoolArg)true);
        }

        public zCVob SearchVobByName(string name)
        {
            zCVob result;
            using (var z = zString.Create(name))
                result = SearchVobByName(z);
            return result;
        }

        public zCVob SearchVobByName(zString name)
        {
            return Process.THISCALL<zCVob>(Address, 0x623FB0, name);
        }

        public zCVob MergeVobSubTree(string visualName)
        {
            zCVob result;
            using (zString zStr = zString.Create(visualName))
                result = MergeVobSubTree(zStr, zCVob.NullVob, 0);
            return result;
        }

        public zCVob MergeVobSubTree(zString visualName, zCVob parent, int mergeType = 0)
        {
            return Process.THISCALL<zCVob>(Address, FuncAddresses.MergeVobSubtree, visualName, parent, new IntArg(mergeType));
        }
    }
}

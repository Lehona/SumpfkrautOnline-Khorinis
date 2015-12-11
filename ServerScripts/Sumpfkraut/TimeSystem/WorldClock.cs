﻿using GUC.Server.Scripts.Sumpfkraut.Utilities.Threading;
using GUC.Server.WorldObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUC.Server.Scripts.Sumpfkraut.TimeSystem
{
    public class WorldClock : Runnable
    {

        new public static readonly String _staticName = "WorldClock (static)";

        protected List<World> affectedWorlds;

        public static readonly TimeSpan defaultTickTimeout = new TimeSpan(0, 0, 10);
        protected TimeSpan tickTimeout;
        public TimeSpan GetTickTimeout () { return this.timeout; }
        public void SetTickTimeout (TimeSpan tickTimeout)
        {
            this.timeout = tickTimeout;
        }

        // how fast goes ingametime goes by relative to realtime
        // note that the actual IGTime does not allow for high precision,
        // although the DateTime- and rate-calculations are performed with doubles
        protected double igTimeRate;
        public double GetIgTimeRate () { return this.igTimeRate; }
        public void SetIgTimeRate (double igTimeRate) { this.igTimeRate = igTimeRate; }

        protected DateTime lastTimeUpdate;
        public DateTime GetLastTimeUpdate () { return this.lastTimeUpdate; }

        protected IGTime igTime;
        public IGTime GetIgTime () { return this.igTime; }
        public void SetIgTime (IGTime igTime)
        {
            this.igTime = igTime;
            UpdateWorldTime();
        }

        protected object igTimeLock = new object();

        public delegate void OnTimeChangeEventHandler (IGTime igTime);
        public event OnTimeChangeEventHandler OnTimeChange; 



        public WorldClock (List<World> affectedWorlds, IGTime startIGTime, 
            double igTimeRate, bool startOnCreate, TimeSpan tickTimeout)
            : base (false, tickTimeout, false)
        {
            SetObjName("WorldClock (default)");
            this.affectedWorlds = affectedWorlds;
            this.igTime = startIGTime;
            this.igTimeRate = igTimeRate;

            if (startOnCreate)
            {
                Start();
            }
        }



        public override void Start ()
        {
            lastTimeUpdate = DateTime.Now;
            base.Start();
        }



        public void UpdateWorldTime ()
        {
            if (affectedWorlds == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            double rlDiff, igDiff;
            long newTotalMinutes;
            IGTime newIgTime;

            lock (igTimeLock)
            {
                try
                {
                    rlDiff = (now - lastTimeUpdate).TotalMinutes;
                    igDiff = rlDiff * igTimeRate;
                    newTotalMinutes = IGTime.ToMinutes(igTime) + (long) igDiff;
                    newIgTime = new IGTime(newTotalMinutes);
                }
                catch (Exception ex)
                {
                    // forcefully reset to day 0 to reduce number sizes in following calculations
                    newIgTime = igTime;
                    newIgTime.day = 0;
                    rlDiff = (now - lastTimeUpdate).TotalMinutes;
                    igDiff = rlDiff * igTimeRate;
                    newTotalMinutes = IGTime.ToMinutes(newIgTime) + (long) igDiff;
                    MakeLogError("Forcefully reset igTime to 0 days, preserving the daytime"
                        + " due to unhandleble calculations: " + ex);
                }

                //MakeLog(IGTime.ToMinutes(igTime));
                //MakeLog(rlDiff);
                //MakeLog(igDiff);
                //MakeLog(newTotalMinutes);
                //MakeLog(new IGTime(newTotalMinutes));

                if ((long) igDiff == 0)
                {
                    // no signifcant difference made
                    return;
                }

                igTime = new IGTime(newTotalMinutes);
                lastTimeUpdate = now;

                if (OnTimeChange != null)
                {
                    OnTimeChange.Invoke(igTime);
                }
            
                for (int w = 0; w < affectedWorlds.Count; w++)
                {
                    affectedWorlds[w].ChangeTime(newIgTime);
                }
            }
        }



        public override void Run ()
        {
            base.Run();

            UpdateWorldTime();
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using GUC.Scripting;
using GUC.Log;
using GUC.Network;
using GUC.Options;

namespace GUC
{
    public static class Program
    {

        private static long updateRate = 0L;
        public static long UpdateRate { get { return updateRate; } }

        private static long timeTillNextUpdate = 0L;
        public static long TimeTillNextUpdate { get { return timeTillNextUpdate; } }

        private static TimeStat timeAll = new TimeStat();
        public static long CurrentElapsedTicks { get { return timeAll.Ticks; } }



        class TimeStat
        {
            long tickCount;
            long tickMax;
            long counter;
            Stopwatch watch;

            public long Ticks { get { return this.tickCount; } }
            public double Average { get { return (double)tickCount / (double)counter / (double)TimeSpan.TicksPerMillisecond; } }
            public double Maximum { get { return (double)tickMax / (double)TimeSpan.TicksPerMillisecond; } }

            public TimeStat()
            {
                watch = new Stopwatch();
                this.Reset();
            }

            public void Start()
            {
                watch.Restart();
            }

            public long Stop()
            {
                watch.Stop();
                long ticks = watch.Elapsed.Ticks;

                this.tickCount += ticks;
                counter++;

                if (ticks > tickMax)
                {
                    tickMax = ticks;
                }
                return ticks;
            }

            public void Reset()
            {
                tickCount = 0;
                counter = 0;
                tickMax = 0;
            }
        }



        static Thread game;
        static Thread tcpListener;
        static void Main(string[] args)
        {
            try
            {
                ServerOptions.Load();
                Console.Title = ServerOptions.ServerName;

                GameServer.Start();

                ScriptManager.StartScripts("Scripts\\ServerScripts.dll");

                game = new Thread(RunServer);
                game.Start();

                tcpListener = new Thread(TCPListener.Run);
                tcpListener.Start();

                Logger.RunLog();
            }
            catch (Exception e)
            {
                Logger.LogError(e.Source + "<br>" + e.Message + "<br>" + e.StackTrace);
                Logger.LogError("InnerException: " + e.InnerException.Source + "<br>" + e.InnerException.Message);
            }
            Console.ReadLine();
        }

        public delegate void OnTickEventHandler();
        public static event OnTickEventHandler OnTick;
        static void RunServer()
        {
            try
            {
                updateRate = 15 * TimeSpan.TicksPerMillisecond; //min time between server ticks

                const long nextInfoUpdateInterval = 1 * TimeSpan.TicksPerMinute;
                long nextInfoUpdateTime = GameTime.Ticks + nextInfoUpdateInterval;

                while (true)
                {
                    timeAll.Start();

                    GameTime.Update();
                    if (OnTick != null) { OnTick(); }

                    WorldObjects.World.ForEach(w => w.OnTick(GameTime.Ticks));
                    GUCTimer.Update(GameTime.Ticks); // move to new thread?
                    GameServer.Update(); //process received packets

                    if (nextInfoUpdateTime < GameTime.Ticks)
                    {
                        Logger.Log("Performance info: {0:0.00}ms average, {1:0.00}ms max. Allocated RAM: {2:0.0}MB", timeAll.Average, timeAll.Maximum, Process.GetCurrentProcess().PrivateMemorySize64 / 1048576d);
                        timeAll.Reset();
                        nextInfoUpdateTime = GameTime.Ticks + nextInfoUpdateInterval;
                    }

                    timeTillNextUpdate = (updateRate - timeAll.Stop()) / TimeSpan.TicksPerMillisecond;
                    if (timeTillNextUpdate > 0)
                    {
                        Thread.Sleep((int)timeTillNextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.Source + "<br>" + e.Message + "<br>" + e.StackTrace);
            }
        }
    }
}
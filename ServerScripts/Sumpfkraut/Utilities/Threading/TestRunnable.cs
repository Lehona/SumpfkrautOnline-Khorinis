﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Server.WorldObjects;
using GUC.Types;

namespace GUC.Server.Scripts.Sumpfkraut.Utilities.Threading
{
    public class TestRunnable : Runnable
    {

        new public static readonly string _staticName = "TestRunnable (static)";

        public delegate void TestEventHandler (DateTime dt);
        public event TestEventHandler TestEvent;

        int day = 0;
        int hour = 0;
        int minute = 0;

        WeatherType weatherType = WeatherType.rain;
        IgTime weatherStartTime = new IgTime();
        IgTime weatherEndTime = new IgTime();

        public TestRunnable (bool startOnCreate, TimeSpan timeout, bool runOnce)
            : base(startOnCreate, timeout, runOnce)
        {
            SetObjName("TestRunnable (default)");
        }

        public override void Run ()
        {
            if (OnRun != null)
            { 
                OnRun.Invoke(this);
            }

            //if (hour >= 2)
            //{
            //    Suspend();
            //}

            //Console.WriteLine("TestRunnable: " + DateTime.Now + " " + this.runOnce);
            Console.WriteLine("TestRunnable: " + day + " " + hour + " " + minute);
            World.NewWorld.ChangeIgTime(day, hour, minute);

            //weatherStartTime.day = 0;
            //weatherStartTime.hour = 8;
            //weatherStartTime.minute = 0;
            //weatherEndTime.day = 0;
            //weatherEndTime.hour = 10;
            //weatherEndTime.minute = 0;
            //World.NewWorld.ChangeWeather(weatherType, weatherStartTime, weatherEndTime);

            if (TestEvent != null)
            {
                TestEvent.Invoke(DateTime.Now);
            }
            
            if (waitHandle != null)
            {
                waitHandle.Set();
            }

            minute++;
        }

    }
}

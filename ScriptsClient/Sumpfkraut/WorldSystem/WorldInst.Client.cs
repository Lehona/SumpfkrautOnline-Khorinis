﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gothic.Types;
using Gothic.View;
using GUC.Scripts.Sumpfkraut.Menus;

namespace GUC.Scripts.Sumpfkraut.WorldSystem
{
    public partial class WorldInst
    {
        public static WorldInst Current { get { return (WorldInst)WorldObjects.World.Current?.ScriptObject; } }

        public void Load()
        {
            GUCMenu.CloseActiveMenus();

            var ogame = GothicGlobals.Game;

            ogame.OpenLoadscreen(!GUCScripts.Ingame, zString.Create(Path));

            zCViewProgressBar progBar = ogame.ProgressBar;
            if (progBar.Address != 0) progBar.SetPercent(0);
            ogame.ClearGameState();

            progBar = ogame.ProgressBar;
            if (progBar.Address != 0) progBar.SetRange(0, 92);

            ogame.LoadWorld(true, Path);

            progBar = ogame.ProgressBar;
            if (progBar.Address != 0) progBar.ResetRange();

            progBar = ogame.ProgressBar;
            if (progBar.Address != 0) progBar.SetRange(92, 100);

            ogame.EnterWorld();

            progBar = ogame.ProgressBar;
            if (progBar.Address != 0) progBar.ResetRange();

            progBar = ogame.ProgressBar;
            if (progBar.Address != 0) progBar.SetPercent(100);

            ogame.SetTime(Clock.Time.GetDay(), Clock.Time.GetHour(), Clock.Time.GetMinute());
            
            GothicGlobals.Game.CloseLoadscreen();
            //Gothic.CGameManager.InitScreen_Close();
        }
    }
}

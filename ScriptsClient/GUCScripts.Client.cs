﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GUC.Log;
using GUC.Scripting;
using System.Reflection;
using System.IO;
using GUC.Client.Scripts.Sumpfkraut.Menus;

namespace GUC.Scripts
{
    public partial class GUCScripts : ScriptInterface
    {
        public static bool Ingame = false;

        public GUCScripts()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            Logger.Log("SumpfkrautOnline ClientScripts loaded!");
        }

        static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            string name = args.Name.Substring(0, args.Name.IndexOf(','));

            Assembly asm = Assembly.LoadFrom(Path.GetFullPath("System\\Multiplayer\\UntoldChapters\\SumpfkrautOnline\\" + name + ".dll"));
            if (asm == null)
            {
                asm = Assembly.LoadFrom(Path.GetFullPath("Multiplayer\\UntoldChapters\\SumpfkrautOnline\\" + name + ".dll"));
            }
            return asm;
        }

        public void Update(long ticks)
        {
            GUCMenu.UpdateMenus(ticks);
            Client.Scripts.TFFA.InputControl.Update(ticks);
        }

        public void StartOutgame()
        {
            WinApi.Process.Write(new byte[] { 0xE9, 0x99, 0x04, 0x00, 0x00 }, 0x0067836C); // always do T_GOTHIT instead of T_STUMBLE/B when getting hit

            Client.Scripts.TFFA.InputControl.Init();
            //Client.Scripts.TFFA.MainMenu.Menu.Open();
            Logger.Log("Outgame started.");
        }

        public void StartIngame()
        {
            Client.Scripts.TFFA.InputControl.Init();
            //GUCMenu.CloseActiveMenus();
            Gothic.Objects.oCNpcFocus.SetFocusMode(1);
            Ingame = true;
            Logger.Log("Ingame started.");
        }
    }
}

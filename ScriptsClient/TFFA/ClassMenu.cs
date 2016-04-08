﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Client.Scripts.Sumpfkraut.GUI.MainMenu;
using GUC.Client.Scripts.Sumpfkraut.Menus.MainMenus;
using GUC.Scripts.TFFA;
using GUC.Network;

namespace GUC.Client.Scripts.TFFA
{
    class ClassMenu : GUCMainMenu
    {
        public static readonly ClassMenu Menu = new ClassMenu();

        MainMenuButton bLight, bHeavy;

        public void SetCounts(int tLight, int tHeavy)
        {
            SetTeam(TFFAClient.Client.Team);
            bLight.Text += ": " + tLight;
            bHeavy.Text += ": " + tHeavy;
        }

        void SetTeam(Team team)
        {
            if (team == Team.AL)
            {
                bLight.Text = "Schatten";
                bHeavy.Text = "Gardist";
            }
            else
            {
                bLight.Text = "Bandit";
                bHeavy.Text = "Söldner";
            }
        }

        protected override void OnCreate()
        {
            Back.CreateTextCenterX("Klasse wählen", 70);
            OnEscape = TeamMenu.Menu.Open;

            const int offset = 200;
            const int dist = 38;
            bLight = AddButton("", "", offset + dist * 0, () => SelectClass(PlayerClass.Light));
            bHeavy = AddButton("", "", offset + dist * 1, () => SelectClass(PlayerClass.Heavy));
        }

        public override void Open()
        {
            if (TFFAClient.Client.Team == Team.Spec)
                return;

            PacketWriter stream = GameClient.Client.GetMenuMsgStream();
            stream.Write((byte)MenuMsgID.OpenClassMenu);
            GameClient.Client.SendMenuMsg(stream);
            base.Open();
            SetTeam(TFFAClient.Client.Team);
        }

        public override void Close()
        {
            PacketWriter stream = GameClient.Client.GetMenuMsgStream();
            stream.Write((byte)MenuMsgID.CloseClassMenu);
            GameClient.Client.SendMenuMsg(stream);
            base.Close();
        }

        void SelectClass(PlayerClass c)
        {
            PacketWriter stream = GameClient.Client.GetMenuMsgStream();
            stream.Write((byte)MenuMsgID.SelectClass);
            stream.Write((byte)c);
            GameClient.Client.SendMenuMsg(stream);
            Close();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Client.GUI.MainMenu;
using GUC.Client.States;

namespace GUC.Client.Menus.MainMenus
{
    class LoginMenu : GUCMainMenu
    {
        MainMenuTextBox name;
        MainMenuTextBox pw;

        protected override void OnCreate()
        {
            Back.CreateTextCenterX("Anmeldung", 100);

            name = AddTextBox("Accountname:", "Name deines Accounts eingeben.", 200, 200, Login);
            pw = AddTextBox("Passwort:", "Passwort deines Accounts eingeben.", 250, 200, Login);
            AddButton("Einloggen", "In den Account einloggen.", 300, Login);
            AddButton("Zurück", "Zurück zum Hauptmenü.", 400, GUCMenus.Main.Open);
            OnEscape = GUCMenus.Main.Open;
        }

        void Login()
        {
            if (name.Input.Length == 0)
            {
                SetCursor(0);
                return;
            }
            else if (pw.Input.Length == 0)
            {
                SetCursor(1);
                return;
            }
            else
            {
                StartupState.clientOptions.name = name.Input;
                StartupState.clientOptions.password = pw.Input;
                StartupState.clientOptions.Save(StartupState.getConfigPath() + "gmp.xml");
                Network.Messages.AccountMessage.Login();
            }
        }
    }
}

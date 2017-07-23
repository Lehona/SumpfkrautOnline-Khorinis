﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUC.Scripts.Sumpfkraut.CommandConsole.InfoObjects
{
    public class PlayerInfo : NPCInfo
    {

        public PlayerInfo ()
        {
            this._objName = "PlayerInfo (default)";
        }



        public int ClientID = -1;
        public byte[] DriveHash;
        public byte[] MacHash;
        public int Ping = -1;

    }
}

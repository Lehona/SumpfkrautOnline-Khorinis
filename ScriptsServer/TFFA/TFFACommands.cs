﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.WorldObjects;
using GUC.Scripts.Sumpfkraut.WorldSystem;
using GUC.Enumeration;
using GUC.Scripts.Sumpfkraut.CommandConsole.InfoObjects;
using GUC.Scripts.Sumpfkraut.Web.WS.Protocols;
using GUC.Server.WorldObjects;
using GUC.Types;

namespace GUC.Scripts.TFFA
{
    public class TFFACommands
    {

        private static List<TFFA.TFFAClient> GetAllClientsTFFA ()
        {
            List<TFFA.TFFAClient> clients = new List<TFFA.TFFAClient>();
            TFFA.TFFAClient.ForEach(c => clients.Add(c));
            return clients;
        }

        protected static List<TFFA.TFFAClient> PrepareClientListTFFA (string[] param)
        {
            List<int> ids = new List<int>(); int id;
            List<string> charNames = new List<string>();
            List<TFFA.TFFAClient> clients = new List<TFFA.TFFAClient>();
            bool getAll = false;

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "all") { getAll = true; }
                if (int.TryParse(param[i], out id)) { ids.Add(id); }
                else { charNames.Add(param[i]); }
            }

            int j = 0;
            TFFA.TFFAClient.ForEach(client =>
            {
                if (j >= param.Length) { return; }
                if (getAll) { clients.Add(client); }
                else
                {
                    if (ids.Contains(client.ID)) { clients.Add(client); }
                    else if (charNames.Contains(client.Name)) { clients.Add(client); }
                }
                j++;
            });

            return clients;
        }

        public static void GetPlayerListTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", "Failed to retrieve player list!" },
            };
            
            StringBuilder infoSB = new StringBuilder();
            infoSB.Append("List of players: ");
            bool first = true;
            TFFA.TFFAClient.ForEach(client =>
            {
                if (!first) { infoSB.Append(", "); }
                infoSB.AppendFormat("[clientID: {0}, clientName: {1}]", client.ID,
                    client.Name);
            });

            returnVal = new Dictionary<string, object>
            {
                { "type", WSProtocolType.chatData },
                { "sender", "SERVER" },
                { "rawText", infoSB.ToString() },
            };
        }

        public static void BanPlayersTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            StringBuilder successMsgSB = new StringBuilder();
            List<TFFA.TFFAClient> clients = PrepareClientListTFFA(param);
            int clientID = -1;
            string charName = "";

            if ((clients == null) || (clients.Count < 1))
            {
                successMsgSB.AppendFormat("Didn't use {0} on any players.", cmd);
            }
            else
            {
                bool first = true;
                successMsgSB.AppendFormat("Used {0} on players:", cmd);
                for (var i = 0; i < clients.Count; i++)
                {
                    clientID = clients[i].ID;
                    charName = clients[i].Name;
                    clients[i].BaseClient.Ban();
                    if (!first) { successMsgSB.Append(","); }
                    else { first = false; }
                    successMsgSB.AppendFormat("[{0}, {1}]", clientID, charName);
                }
            }

            returnVal = new Dictionary<string, object>
            {
                { "type", WSProtocolType.chatData },
                { "sender", "SERVER" },
                { "rawText", successMsgSB.ToString() },
            };
        }

        public static void KickPlayersTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            StringBuilder successMsgSB = new StringBuilder();
            List<TFFA.TFFAClient> clients = PrepareClientListTFFA(param);
            int clientID = -1;
            string charName = "";

            if ((clients == null) || (clients.Count < 1))
            {
                successMsgSB.AppendFormat("Didn't use {0} on any players.", cmd);
            }
            else
            {
                bool first = true;
                successMsgSB.AppendFormat("Used {0} on players:", cmd);
                for (var i = 0; i < clients.Count; i++)
                {
                    clientID = clients[i].ID;
                    charName = clients[i].Name;
                    clients[i].BaseClient.Kick();
                    if (!first) { successMsgSB.Append(","); }
                    else { first = false; }
                    successMsgSB.AppendFormat("[{0}, {1}]", clientID, charName);
                }
            }

            returnVal = new Dictionary<string, object>
            {
                { "type", WSProtocolType.chatData },
                { "sender", "SERVER" },
                { "rawText", successMsgSB.ToString() },
            };
        }

        public static void KillPlayersTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            StringBuilder successMsgSB = new StringBuilder();
            List<TFFA.TFFAClient> clients = PrepareClientListTFFA(param);

            if ((clients == null) || (clients.Count < 1))
            {
                successMsgSB.AppendFormat("Didn't use {0} on any players.", cmd);
            }
            else
            {
                bool first = true;
                successMsgSB.AppendFormat("Used {0} on players:", cmd);
                for (var i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Character != null)
                    {
                        //clients[i].Character.SetHealth(0);
                        TFFA.TFFAGame.Kill(clients[i], false);
                        if (!first) { successMsgSB.Append(","); }
                        else { first = false; }
                        successMsgSB.AppendFormat("[{0}, {1}]", clients[i].ID, clients[i].Name);
                    }
                }
            }

            returnVal = new Dictionary<string, object>
            {
                { "type", WSProtocolType.chatData },
                { "sender", "SERVER" },
                { "rawText", successMsgSB.ToString() },
            };
        }



        public static void SwitchTeamTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", "Wasn't able to switch teams" },
            };

            if (param.Length < 2)
            {
                returnVal["rawText"] += ": Provide at least 2 command arguments!";
                return;
            }

            List<TFFA.TFFAClient> clients = PrepareClientListTFFA(new string[] { param[0] });
            if (clients.Count < 1)
            {
                returnVal["rawText"] += string.Format(": {0} is no valid client!", param[0]);
            }

            TFFA.Team newTeam = TFFA.Team.Spec;
            bool changeTeam = true;
            switch (param[1].ToUpper())
            {
                case "AL":
                    newTeam = TFFA.Team.AL;
                    break;
                case "NL":
                    newTeam = TFFA.Team.NL;
                    break;
                case "SPEC":
                    newTeam = TFFA.Team.Spec;
                    break;
                default:
                    changeTeam = false;
                    break;
            }

            if (changeTeam)
            {
                TFFA.TFFAGame.AddToTeam(clients[0], newTeam);
            }
        }

        public static void SetPhaseTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", "Wasn't able to change gamephase" },
            };

            if (param.Length < 1)
            {
                returnVal["rawText"] += ": Provide at least 1 command argument!";
                return;
            }

            switch (param[0].ToUpper())
            {
                case "WAIT":
                    TFFA.TFFAGame.PhaseWait();
                    returnVal["rawText"] = "Changed gamephase to waiting phase";
                    break;
                case "FIGHT":
                    TFFA.TFFAGame.PhaseFight();
                    returnVal["rawText"] = "Changed gamephase to fighting phase";
                    break;
                case "END":
                    TFFA.TFFAGame.PhaseEnd();
                    returnVal["rawText"] = "Changed gamephase to ending phase";
                    break;
                default:
                    returnVal["rawText"] += string.Format(":{0} is an invalid command argument!", 
                        param[0]);
                    break;
            }
        }



        public static void SetIGTimeTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", "Failed to set ig-time!" },
            };

            if (param.Length < 1)
            {
                returnVal["rawText"] += " Provide at least 1 command argument.";
                return;
            }

            WorldTime time = WorldInst.Current.Clock.Time;
            float rate = WorldInst.Current.Clock.Rate;
            bool timeReady = false, rateReady = false;
            for (int i = 0; i < param.Length; i++)
            {
                if ((!rateReady) && (float.TryParse(param[i], out rate))) { rateReady = true; }
                if ((!timeReady) && (WorldTime.TryParseDayHourMin(param[i], out time))) { timeReady = true; }
                if (timeReady && rateReady) { break; }
            }

            if (!(timeReady || rateReady)) { return; }
            WorldInst.Current.Clock.SetTime(time, rate);
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", string.Format("Set ig-time to {0} and ig-time-rate to {1}", time, rate) },
            };
        }

        public static void SetIGWeatherTypeTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", "Failed to set ig-weathertype!" },
            };

            if (param.Length < 1)
            {
                returnVal["rawText"] += " Provide at least 1 command argument.";
                return;
            }

            WeatherTypes weatherType = default(WeatherTypes);
            int weatherInt = -1;
            bool success = false;
            if (int.TryParse(param[0], out weatherInt))
            {
                if (Enum.IsDefined(typeof(WeatherTypes), weatherInt))
                {
                    weatherType = (WeatherTypes) weatherInt;
                    success = true;
                }
            }
            else if (Enum.TryParse(param[0], out weatherType))
            {
                success = true;
            }

            if (success)
            {
                WorldInst.Current.SkyCtrl.SetWeatherType(weatherType);
                returnVal = new Dictionary<string, object>()
                {
                    { "rawText", string.Format("Set ig-weathertype to {0}.", weatherType) },
                };
            }
        }

        public static void SetIGRainTimeTFFA (object sender, string cmd, string[] param,
            out Dictionary<string, object> returnVal)
        {
            returnVal = new Dictionary<string, object>()
            {
                { "rawText", "Failed to set ig-raintime!" },
            };

            if (param.Length < 1)
            {
                returnVal["rawText"] += " Provide at least 2 command arguments.";
                return;
            }

            WorldTime rainTime = default(WorldTime);
            float weight = 0f;
            bool rainTimeReady = false, weightReady = false;

            for (int i = 0; i < param.Length; i++)
            {
                if ((!weightReady) && float.TryParse(param[i], out weight)) { weightReady = true; }
                if ((!rainTimeReady) && WorldTime.TryParseDayHourMin(param[i], out rainTime)) { rainTimeReady = true; }
            }

            if (rainTimeReady && weightReady)
            {
                WorldInst.Current.SkyCtrl.SetRainTime(rainTime, weight);
                returnVal = new Dictionary<string, object>()
                {
                    { "rawText", string.Format("Set rain at time {0} to weight {1}.", rainTime, weight) },
                };
            }
        }

    }
}
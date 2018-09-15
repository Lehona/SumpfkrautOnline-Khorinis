﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.Scripts.Sumpfkraut.Menus;
using GUC.Scripting;
using GUC.Network;


namespace GUC.Scripts.Arena.GameModes.Horde
{
    partial class HordeMode
    {

        public override Action OpenJoinMenu { get { return MenuClassSelect.Instance.Open; } }

        public override ScoreBoardScreen ScoreBoard { get { return HordeScoreBoard.Instance; } }

        public static void End(bool win)
        {
            if (!HordeMode.IsActive)
                return;

            // HordeScoreBoard.Instance.Open();
            DoVictoryStuff(win);
        }

        HordeScenario.Stand ActiveStand;
        SoundInstance StandSFXLoop;
        int messageIndex = 0;
        GUCTimer messageTimer = new GUCTimer();
        void StartStand(HordeScenario.Stand stand)
        {
            if (!HordeMode.IsActive)
                return;

            ActiveStand = stand;

            if (!string.IsNullOrWhiteSpace(stand.SFXStart))
            {
                var def = new SoundDefinition(stand.SFXStart);
                if (stand.GlobalSFX)
                    SoundHandler.PlaySound(def, 1.0f);
                else
                    SoundHandler.PlaySound3D(def, stand.Position, 5000 + stand.Range, 1.0f);
            }

            if (!string.IsNullOrWhiteSpace(stand.SFXLoop))
            {
                var def = new SoundDefinition(stand.SFXLoop);
                if (stand.GlobalSFX)
                    SoundHandler.PlaySound(def, 1.0f);
                else
                    SoundHandler.PlaySound3D(def, stand.Position, 5000 + stand.Range, 0.5f, true);
            }

            if (stand.Messages != null && stand.Messages.Length > 0)
            {
                messageIndex = 0;
                NextMessage();
                if (stand.Messages.Length > 1 && stand.Duration > 0)
                {
                    messageTimer.SetInterval(stand.Duration / (stand.Messages.Length - 1));
                    messageTimer.SetCallback(NextMessage);
                    messageTimer.Start();
                }
            }
        }

        void NextMessage()
        {
            if (ActiveStand == null || messageIndex >= ActiveStand.Messages.Length)
            {
                messageTimer.Stop();
                return;
            }

            Log.Logger.Log(ActiveStand.Messages[messageIndex]);
            ChatMenu.Menu.AddMessage(ChatMode.Private, ActiveStand.Messages[messageIndex++]);
        }

        void Endstand()
        {
            if (ActiveStand == null)
                return;

            if (StandSFXLoop != null)
                SoundHandler.StopSound(StandSFXLoop);

            var stand = ActiveStand;
            if (HordeMode.IsActive)
            {
                if (!string.IsNullOrWhiteSpace(stand.SFXStop))
                {
                    var def = new SoundDefinition(stand.SFXStop);
                    if (stand.GlobalSFX)
                        SoundHandler.PlaySound(def, 1.0f);
                    else
                        SoundHandler.PlaySound3D(def, stand.Position, 5000 + stand.Range, 1.0f);
                }
            }
            ActiveStand = null;
            messageTimer.Stop();
        }

        protected override void End()
        {
            Endstand();
            base.End();
        }

        protected override void SetPhase(GamePhase phase)
        {
            if (phase > GamePhase.Fight)
            {
                StartStand(Scenario.Stands[phase - GamePhase.Fight - 1]);
            }
            else
            {
                Endstand();
            }

            base.SetPhase(phase);
        }
    }
}

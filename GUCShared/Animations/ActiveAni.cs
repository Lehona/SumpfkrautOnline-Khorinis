﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GUC.WorldObjects;
using GUC.Scripting;
using GUC.Models;

namespace GUC.Animations
{
    /// <summary>
    /// Handle for an active animation (for progress etc.).
    /// </summary>
    public partial class ActiveAni
    {
        struct TimeActionPair
        {
            public float Frame;
            public long Time;
            public Action Callback;
        }

        GUCModelInst model;
        /// <summary> The animated model which is playing this animation. </summary>
        public GUCModelInst Model { get { return this.model; } }

        float fpsMult;
        /// <summary> The value with which the frame speed is multiplied. </summary>
        public float FrameSpeedMultiplier { get { return this.fpsMult; } }
        /// <summary> The frame speed with which the active animation is being played. </summary>
        public float FPS { get { return this.ani.FPS * this.fpsMult; } }

        Animation ani;
        /// <summary> The Animation which is being active. </summary>
        public Animation Ani { get { return this.ani; } }

        /// <summary> The AniJob of the active animation. </summary>
        public AniJob AniJob { get { return this.ani.AniJob; } }

        long startTime;
        long endTime;

        public bool IsIdleAni { get { return this.endTime < 0; } }

        internal ActiveAni(GUCModelInst model)
        {
            this.model = model ?? throw new ArgumentNullException("Model is null!");
        }

        public float GetProgress()
        {
            return endTime < 0 ? 0 : (float)(GameTime.Ticks - this.startTime) / (this.endTime - this.startTime);
        }

        float totalFinishedFrames; // frames from previous animations

        List<TimeActionPair> actionPairs = new List<TimeActionPair>(1);

        void AddPair(float frame, Action callback, long time)
        {
            TimeActionPair pair = new TimeActionPair();
            pair.Frame = frame;
            pair.Callback = callback;
            pair.Time = time < 0 ? long.MaxValue : time;
            for (int i = 0; i < actionPairs.Count; i++)
                if (time > actionPairs[i].Time)
                {
                    actionPairs.Insert(i, pair);
                    return;
                }
            actionPairs.Add(pair);
        }

        float cachedProgress = 0;
        FrameActionPair[] cachedPairs = null;
        bool cachedStuff = false;

        internal void Start(Animation ani, float fpsMult, float progress, FrameActionPair[] pairs)
        {
            if (!this.Model.Vob.IsSpawned)
            {
                this.ani = ani;
                this.fpsMult = fpsMult;
                this.cachedProgress = progress;
                this.cachedPairs = pairs;
                this.cachedStuff = true;
                return;
            }
            else
            {
                cachedStuff = false;
                this.cachedPairs = null;
            }

            this.ani = ani;
            this.fpsMult = fpsMult;
            this.totalFinishedFrames = 0;

            float numFrames = ani.GetFrameNum();
            if (numFrames > 0)
            {
                float coeff = TimeSpan.TicksPerSecond / (ani.FPS * fpsMult);
                float duration = numFrames * coeff;

                startTime = GameTime.Ticks - (long)(progress * duration);
                endTime = startTime + (long)duration;

                if (pairs != null && pairs.Length > 0)
                {
                    for (int i = pairs.Length - 1; i >= 0; i--)
                    {
                        FrameActionPair pair = pairs[i];
                        if (pair.Callback != null)
                        {
                            AddPair(pair.Frame, pair.Callback, startTime + (long)(pair.Frame * coeff));
                        }
                    }
                }
            }
            else // idle ani
            {
                endTime = -1;
            }
        }

        internal void Stop()
        {
            cachedStuff = false;
            this.cachedPairs = null;

            if (this.actionPairs.Count > 0)
            {
                // fire all callbacks which should happen when or after the last nextAni ends
                float lastFrame = this.totalFinishedFrames;
                AniJob nextAniJob = this.AniJob;
                while (nextAniJob != null && this.model.TryGetAniFromJob(nextAniJob, out Animation nextAni))
                {
                    lastFrame += nextAni.GetFrameNum();
                    nextAniJob = nextAniJob.NextAni;
                }

                for (int i = 0; i < actionPairs.Count; i++)
                {
                    if (this.actionPairs[i].Frame >= lastFrame)
                        this.actionPairs[i].Callback();
                    else break;
                }

                this.actionPairs.Clear();
            }
            
            this.ani = null;
        }

        internal void OnTick(long now)
        {
            if (cachedStuff)
            {
                this.Start(ani, fpsMult, cachedProgress, cachedPairs);
            }

            if (!this.IsIdleAni)
            {
                if (now < endTime) // still playing
                {
                    for (int i = actionPairs.Count - 1; i >= 0; i--)
                    {
                        var current = actionPairs[i];
                        if (now < current.Time)
                            break;

                        actionPairs.RemoveAt(i);
                        current.Callback();
                        if (this.ani == null) // ani is stopped
                            break;
                    }
                }
                else
                {
                    AniJob nextAniJob = this.AniJob.NextAni;
                    if (nextAniJob != null)
                    {
                        if (model.TryGetAniFromJob(nextAniJob, out Animation nextAni))
                        {
                            Continue(nextAni); // there is a nextAni, continue playing it
                            return;
                        }
                    }

                    // fire all remaining actions
                    actionPairs.ForEach(p => p.Callback());
                    actionPairs.Clear();

                    // end this animation
                    model.EndAni(this.ani);
                    this.ani = null;
                }
            }
        }

        void Continue(Animation nextAni)
        {
            float oldFrameNum = this.ani.GetFrameNum();
            float newFrameNum = nextAni.GetFrameNum();
            if (newFrameNum > 0)
            {
                float coeff = TimeSpan.TicksPerSecond / (nextAni.FPS * fpsMult);

                this.startTime = this.endTime;
                this.endTime = this.startTime + (long)(newFrameNum * coeff);
                
                for (int i = 0; i < actionPairs.Count; i++)
                {
                    var pair = actionPairs[i];

                    pair.Frame = pair.Frame - oldFrameNum; // new frame
                    pair.Time = startTime + (long)(pair.Frame * coeff);
                }
            }
            else
            {
                endTime = -1;
            }

            this.totalFinishedFrames += oldFrameNum;
            this.ani = nextAni;
        }
    }
}

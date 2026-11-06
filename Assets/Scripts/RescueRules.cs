using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frostline
{
    public enum MissionPhase { Menu, Running, Paused, Won, Lost }
    public sealed class RescueRules
    {
        public MissionPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public int Delivered { get { return delivered.Count; } }
        public int Required { get; private set; }
        public bool Practice { get; private set; }
        public float Limit { get; private set; }
        public string Failure { get; private set; }
        readonly HashSet<int> delivered = new HashSet<int>();
        public RescueRules() { Phase = MissionPhase.Menu; }
        public void Begin(int required, float limit, bool practice)
        {
            delivered.Clear(); Required = required; Limit = limit; Practice = practice;
            Elapsed = 0; Failure = ""; Phase = MissionPhase.Running;
        }
        public void Tick(float delta)
        {
            if (Phase != MissionPhase.Running) return;
            Elapsed += Mathf.Max(0, delta);
            if (!Practice && Elapsed >= Limit) Fail("作业时间已用尽。尝试先提高载荷，再缓慢回转。");
        }
        public bool Deliver(int cargoId)
        {
            if (Phase != MissionPhase.Running || !delivered.Add(cargoId)) return false;
            if (Delivered >= Required) Phase = MissionPhase.Won;
            return true;
        }
        public void Pause(bool paused)
        {
            if (paused && Phase == MissionPhase.Running) Phase = MissionPhase.Paused;
            else if (!paused && Phase == MissionPhase.Paused) Phase = MissionPhase.Running;
        }
        public void Fail(string reason)
        {
            if (Phase != MissionPhase.Running) return;
            Failure = reason; Phase = MissionPhase.Lost;
        }
        public void Menu() { Phase = MissionPhase.Menu; }
        public int Score(float damage)
        {
            return Mathf.Max(0, Mathf.RoundToInt(1000 * Required + Mathf.Max(0, Limit - Elapsed) * 2 - damage * 8));
        }
    }
    [Serializable]
    public sealed class RescueSave
    {
        public int version = 1;
        public int unlocked = 1;
        public int[] best = new int[4];
        public float volume = 0.65f;
        public float sensitivity = 1;
        public int quality = 1;
        public bool tutorialSeen;
    }
}

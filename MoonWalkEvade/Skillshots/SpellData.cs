﻿using EloBuddy;
using SharpDX;

namespace MoonWalkEvade.Skillshots
{
    public class SpellData
    {
        public string DisplayName { get; set; }
        public string SpellName { get; set; }
        public string MissileSpellName { get; set; }
        public SpellSlot Slot { get; set; }
        public int Delay { get; set; }
        public int Range { get; set; }
        public int Radius { get; set; }
        public float MissileSpeed { get; set; }
        public int DangerValue { get; set; }
        public bool IsDangerous { get; set; }
        public string ChampionName { get; set; }
        public string ToggleParticleName { get; set; }
        public bool AddHitbox { get; set; }
        public int ExtraMissiles { get; set; }

        public bool EnabledByDefault { get; set; } = true;

        public bool IsGlobal => Range > 10000;



        public bool IsPerpendicular { get; set; }
        public int SecondaryRadius { get; set; }
        public Vector2 Direction { get; set; }

        public SpellData()
        {
            AddHitbox = true;
        }
    }
}
﻿using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace MoonWalkEvade.Skillshots
{
    public class EvadeSkillshot
    {
        public SpellDetector SpellDetector { get; set; }
        public GameObject SpawnObject { get; set; }
        public Obj_AI_Base Caster { get; set; }
        public GameObjectProcessSpellCastEventArgs CastArgs { get; set; }
        public EloBuddy.SpellData SData { get; set; }
        public SpellData OwnSpellData { get; set; }
        public GameObjectTeam Team { get; set; }
        public bool IsActive { get; set; }
        public bool IsValid { get; set; }
        public bool CastComplete { get; set; }
        public int TimeDetected { get; set; }


        public bool IsProcessSpellCast => Caster != null;

        public string DisplayText => $"{OwnSpellData.ChampionName} {OwnSpellData.Slot} - {OwnSpellData.DisplayName}";

        public virtual Vector3 GetPosition()
        {
            return Vector3.Zero;
        }

        public virtual void OnCreateObject(GameObject obj)
        {
        }

        public virtual void OnDeleteObject(GameObject obj)
        {
        }

        public virtual void OnCreate(GameObject obj)
        {
        }

        public virtual bool OnDelete(GameObject obj)
        {
            return true;
        }

        public virtual void OnDispose()
        {
        }

        public virtual void OnDraw()
        {
        }

        public virtual void OnTick()
        {
        }

        public virtual void OnSpellDetection(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
        }

        public virtual Geometry.Polygon ToPolygon(float extrawidth = 0)
        {
            return null;
        }

        public virtual int GetAvailableTime(Vector2 pos)
        {
            return 0;
        }

        public virtual bool IsFromFow()
        {
            return false;
        }

        public virtual EvadeSkillshot NewInstance()
        {
            return new EvadeSkillshot();
        }

        public override string ToString()
        {
            return string.Format("{0}_{1}_{2}", OwnSpellData.ChampionName, OwnSpellData.Slot, OwnSpellData.DisplayName);
        }
    }
}
﻿using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using MoonWalkEvade.Utils;
using SharpDX;
using Color = System.Drawing.Color;

namespace MoonWalkEvade.Skillshots.SkillshotTypes
{
    class CaitlynTrap : EvadeSkillshot
    {
        public CaitlynTrap()
        {
            Caster = null;
            SpawnObject = null;
            SData = null;
            OwnSpellData = null;
            Team = GameObjectTeam.Unknown;
            IsValid = true;
            TimeDetected = Environment.TickCount;
        }

        public Vector3 EndPosition { get; set; }

        public MissileClient Missile => SpawnObject as MissileClient;

        public override Vector3 GetPosition()
        {
            return EndPosition;
        }

        /// <summary>
        /// Creates an existing Class Object unlike the DataBase contains
        /// </summary>
        /// <returns></returns>
        public override EvadeSkillshot NewInstance(bool debug = false)
        {
            var newInstance = new CaitlynTrap { OwnSpellData = OwnSpellData };
            if (debug)
            {
                var newDebugInst = new CaitlynTrap
                {
                    OwnSpellData = OwnSpellData,
                    EndPosition = Debug.GlobalEndPos,
                    IsValid = true,
                    IsActive = true,
                    TimeDetected = Environment.TickCount,
                    SpawnObject = null
                };
                return newDebugInst;
            }
            return newInstance;
        }

        public override void OnCreate(GameObject obj)
        {
            EndPosition = CastArgs?.End ?? Missile.EndPosition;
        }

        public override void OnCreateObject(GameObject obj)
        {
            //var missile = obj as MissileClient;

            //if (SpawnObject == null && missile != null)
            //{
            //    if (missile.SData.Name == OwnSpellData.ObjectCreationName && missile.SpellCaster.Index == Caster.Index)
            //    {
            //        // Force skillshot to be removed
            //        //IsValid = false;
            //    }
            //}
        }

        public override bool OnDeleteMissile(GameObject obj)
        {
            return false;
        }

        public override void OnDeleteObject(GameObject obj)
        {
            //IsValid = false;
        }

        /// <summary>
        /// check if still valid
        /// </summary>
        public override void OnTick()
        {
            if (EntityManager.Heroes.Allies.Any(x => x.Distance(EndPosition) <= 100 &&
                    x.HasBuff("caitlynyordletrapdebuff")) && IsValid)
                IsValid = false;
            

           if (Environment.TickCount >= TimeDetected + OwnSpellData.Delay + 90 * 1000)
                IsValid = false;
        }

        public override void OnDraw()
        {
            if (!IsValid)
            {
                return;
            }

            ToPolygon().DrawPolygon(Color.White);
        }

        public override Geometry.Polygon ToRealPolygon()
        {
            return ToPolygon();
        }

        public override Geometry.Polygon ToPolygon(float extrawidth = 0)
        {
            if (OwnSpellData.AddHitbox)
            {
                extrawidth += Player.Instance.HitBoxRadius();
            }

            return new Geometry.Polygon.Circle(EndPosition, OwnSpellData.Radius + extrawidth);
        }

        public override int GetAvailableTime(Vector2 pos)
        {
            return OwnSpellData.Delay - (Environment.TickCount - TimeDetected);
        }

        public override bool IsFromFow()
        {
            return false;
        }
    }
}
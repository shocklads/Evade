﻿using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using MoonWalkEvade.Evading;
using MoonWalkEvade.Utils;
using SharpDX;
using Color = System.Drawing.Color;

namespace MoonWalkEvade.Skillshots.SkillshotTypes
{
    public class LinearMissileSkillshot : EvadeSkillshot
    {
        public LinearMissileSkillshot()
        {
            Caster = null;
            SpawnObject = null;
            SData = null;
            OwnSpellData = null;
            Team = GameObjectTeam.Unknown;
            IsValid = true;
            TimeDetected = Environment.TickCount;
        }

        public Vector3 _startPos;
        public Vector3 _endPos;
        private bool DoesCollide, CollisionChecked;
        private Vector2 LastCollisionPos;

        public MissileClient Missile => OwnSpellData.IsPerpendicular ? null : SpawnObject as MissileClient;

        public Vector3 StartPosition
        {
            get
            {
                bool debugMode = EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue;
                if (debugMode)//Simulate Position
                {
                    float speed = OwnSpellData.MissileSpeed;
                    float timeElapsed = Environment.TickCount - TimeDetected - OwnSpellData.Delay;
                    float traveledDist = speed * timeElapsed / 1000;
                    return Debug.GlobalStartPos.Extend(Debug.GlobalEndPos, traveledDist).To3D();
                }

                if (Missile == null)
                {
                    return _startPos;
                }

                return Missile.Position;
            }
        }

        public Vector3 EndPosition
        {
            get
            {

                bool debugMode = EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue;
                if (debugMode)
                    return Debug.GlobalEndPos;

                if (Missile == null)
                {
                    return _endPos;
                }

                if (DoesCollide)
                    return LastCollisionPos.To3D();


                return Missile.StartPosition.ExtendVector3(Missile.EndPosition, OwnSpellData.Range);
            }
        }

        public override Vector3 GetPosition()
        {
            return StartPosition;
        }

        public override EvadeSkillshot NewInstance(bool debug = false)
        {
            var newInstance = new LinearMissileSkillshot { OwnSpellData = OwnSpellData };
            if (debug)
            {
                //SpawnObject = null;//missile = null => using public setable _startPos
                //SpawnObject = new MissileClient();//missile != null
                var newDebugInst = new LinearMissileSkillshot
                {
                    OwnSpellData = OwnSpellData, _startPos = Debug.GlobalStartPos,
                    _endPos = Debug.GlobalEndPos, IsValid = true, IsActive = true, TimeDetected = Environment.TickCount
                };
                return newDebugInst;
            }
            return newInstance;
        }

        public override void OnCreateObject(GameObject obj)
        {
            var missile = obj as MissileClient;

            if (SpawnObject == null && missile != null)
            {
                if (missile.SData.Name == OwnSpellData.ObjectCreationName && missile.SpellCaster.Index == Caster.Index)
                {
                    IsValid = false;
                }
            }
        }

        public override void OnSpellDetection(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!OwnSpellData.IsPerpendicular)
            {
                _startPos = Caster.ServerPosition;
                _endPos = _startPos.ExtendVector3(CastArgs.End, OwnSpellData.Range);
            }
        }

        public override void OnTick()
        {
            if (Missile == null && !EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
            {
                if (Environment.TickCount > TimeDetected + OwnSpellData.Delay + 250)
                {
                    IsValid = false;
                    return;
                }
            }
            else if (Missile != null)
            {
                if (Environment.TickCount > TimeDetected + 6000)
                {
                    IsValid = false;
                    return;
                }
            }
            else if (EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
            {
                float speed = OwnSpellData.MissileSpeed;
                float timeElapsed = Environment.TickCount - TimeDetected - OwnSpellData.Delay;
                float traveledDist = speed * timeElapsed / 1000;

                if (traveledDist >= Debug.GlobalStartPos.Distance(Debug.GlobalEndPos))
                {
                    IsValid = false;
                    return;
                }
            }

            if (!CollisionChecked)
            {
                Vector2 collision = this.GetCollisionPoint();
                DoesCollide = !collision.IsZero;
                LastCollisionPos = collision;
                CollisionChecked = true;
            }
        }

        public override void OnDraw()
        {
            if (!IsValid)
            {
                return;
            }

            Utils.Utils.Draw3DRect(StartPosition, EndPosition, OwnSpellData.Radius * 2, Color.White);
        }

        public override Geometry.Polygon ToPolygon(float extrawidth = 0)
        {
            if (OwnSpellData.AddHitbox)
            {
                extrawidth += Player.Instance.HitBoxRadius();
            }

            return new Geometry.Polygon.Rectangle(StartPosition, EndPosition.ExtendVector3(StartPosition, -extrawidth), OwnSpellData.Radius * 2 + extrawidth);
        }

        public override int GetAvailableTime(Vector2 pos)
        {
            var dist1 =
                Math.Abs((EndPosition.Y - StartPosition.Y) * pos.X - (EndPosition.X - StartPosition.X) * pos.Y +
                         EndPosition.X * StartPosition.Y - EndPosition.Y * StartPosition.X) / StartPosition.Distance(EndPosition);

            var actualDist = Math.Sqrt(StartPosition.Distance(pos).Pow() - dist1.Pow());

            var time = OwnSpellData.MissileSpeed > 0 ? (int) (actualDist / OwnSpellData.MissileSpeed * 1000) : 0;

            if (Missile == null)
            {
                return Math.Max(0, OwnSpellData.Delay - (Environment.TickCount - TimeDetected));
            }

            return time;
        }

        public override bool IsFromFow()
        {
            return Missile != null && !Missile.SpellCaster.IsVisible;
        }
    }
}
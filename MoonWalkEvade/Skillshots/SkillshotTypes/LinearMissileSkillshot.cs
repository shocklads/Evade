using System;
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
                if (Missile == null)
                {
                    return _startPos;
                }

                if (EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue && !Debug.GlobalEndPos.IsZero &&
                   !Debug.GlobalStartPos.IsZero)
                {
                    var endPos = Debug.GlobalEndPos.To3D();
                    float realDist = Missile.Position.Distance(Missile.StartPosition.ExtendVector3(Missile.EndPosition, OwnSpellData.Range));

                    return endPos.Extend(Debug.GlobalStartPos, realDist).To3D();
                }

                return Missile.Position;
            }
        }

        public Vector3 EndPosition
        {
            get
            {
                if (Missile == null)
                {
                    return _endPos;
                }

                if (EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue && !Debug.GlobalEndPos.IsZero)
                {
                    return Debug.GlobalEndPos.To3D();
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

        public override EvadeSkillshot NewInstance()
        {
            var newInstance = new LinearMissileSkillshot { OwnSpellData = OwnSpellData };
            return newInstance;
        }

        public override void OnCreateObject(GameObject obj)
        {
            var missile = obj as MissileClient;

            if (SpawnObject == null && missile != null)
            {
                if (missile.SData.Name == OwnSpellData.MissileSpellName && missile.SpellCaster.Index == Caster.Index)
                {
                    if (!EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
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
            else
            if (EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue && !Debug.GlobalEndPos.IsZero &&
                   !Debug.GlobalStartPos.IsZero)
            {
                var endPos = Debug.GlobalEndPos.To3D();
                float realDist = Missile.Position.Distance(Missile.StartPosition.ExtendVector3(Missile.EndPosition, OwnSpellData.Range));

                _startPos = endPos.Extend(Debug.GlobalStartPos, realDist).To3D();
                _endPos = Debug.GlobalEndPos.To3D();

            }
        }

        public override void OnTick()
        {
            if (Missile == null)
            {
                if (Environment.TickCount > TimeDetected + OwnSpellData.Delay + 250)
                {
                    IsValid = false;
                    return;
                }
            }
            else
            {
                if (Environment.TickCount > TimeDetected + 6000)
                {
                    IsValid = false;
                    return;
                }
            }

            //if (!CollisionChecked)
            //{
            //    Vector2 collision = this.GetCollisionPoint();
            //    DoesCollide = !collision.IsZero;
            //    Chat.Print(DoesCollide);
            //    LastCollisionPos = collision;
            //    CollisionChecked = true;
            //}
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
                time += Math.Max(0, OwnSpellData.Delay - (Environment.TickCount - TimeDetected));
            }

            return time;
        }

        public override bool IsFromFow()
        {
            return Missile != null && !Missile.SpellCaster.IsVisible;
        }
    }
}
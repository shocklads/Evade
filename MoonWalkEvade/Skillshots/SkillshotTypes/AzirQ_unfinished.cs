using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using MoonWalkEvade.Utils;
using SharpDX;

namespace MoonWalkEvade.Skillshots.SkillshotTypes
{
    class AzirQ : EvadeSkillshot
    {
        public AzirQ()
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
        Vector3 RealStartPos_Missile;
        Vector3 RealEndPos_Missile;

        public Vector3 StartPosition
        {
            get
            {
                if (Missile == null)
                {
                    return _startPos;
                }

                float speed = OwnSpellData.MissileSpeed;
                float timeElapsed = Environment.TickCount - TimeDetected - OwnSpellData.Delay;
                float traveledDist = speed * timeElapsed / 1000;

                return RealStartPos_Missile.ExtendVector3(RealEndPos_Missile, traveledDist);
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

                return RealEndPos_Missile;
            }
        }

        public MissileClient Missile => SpawnObject as MissileClient;

        public override Vector3 GetPosition()
        {
            return StartPosition;
        }

        public override void OnCreateObject(GameObject obj)
        {
            OnCreate(obj);
        }

        public override void OnCreate(GameObject obj)
        {
            if (Missile == null)
            {
                OnSpellDetection(null);
            }
            else
            {
                if (!Orbwalker.ValidAzirSoldiers.Any())
                    return;

                RealStartPos_Missile =
                    Orbwalker.ValidAzirSoldiers.OrderBy(x => x.Distance(Missile.StartPosition)).First().Position;
                RealEndPos_Missile = RealStartPos_Missile.ExtendVector3(Missile.EndPosition, OwnSpellData.Range);
            }
        }

        public override EvadeSkillshot NewInstance(bool debug = false)
        {
            var newInstance = new AzirQ { OwnSpellData = OwnSpellData };
            return newInstance;
        }

        public override void OnSpellDetection(Obj_AI_Base sender)
        {
            if (!Orbwalker.ValidAzirSoldiers.Any())
                return;

            _startPos = Orbwalker.ValidAzirSoldiers.OrderBy(x => x.Distance(CastArgs.Start)).First().Position;
            _endPos = _startPos.ExtendVector3(CastArgs.End, OwnSpellData.Range);
        }

        public override void OnTick()
        {
            if (Missile == null)
            {
                if (Environment.TickCount > TimeDetected + OwnSpellData.Delay + 250)
                {
                    IsValid = false;
                }
            }
            else if (Missile != null)
            {
                if (Environment.TickCount > TimeDetected + 6000)
                {
                    IsValid = false;
                }
            }
        }

        public override void OnDraw()
        {
            if (!IsValid)
            {
                return;
            }

            Utils.Utils.Draw3DRect(StartPosition, EndPosition, OwnSpellData.Radius * 2, System.Drawing.Color.White);
        }

        public override Geometry.Polygon ToRealPolygon()
        {
            var halfWidth = OwnSpellData.Radius;
            var d1 = StartPosition.To2D();
            var d2 = EndPosition.To2D();
            var direction = (d1 - d2).Perpendicular().Normalized();

            Vector3[] points =
            {
                (d1 + direction*halfWidth).To3DPlayer(),
                (d1 - direction*halfWidth).To3DPlayer(),
                (d2 - direction*halfWidth).To3DPlayer(),
                (d2 + direction*halfWidth).To3DPlayer()
            };
            var p = new Geometry.Polygon();
            p.Points.AddRange(points.Select(x => x.To2D()).ToList());

            return p;
        }

        public override Geometry.Polygon ToPolygon(float extrawidth = 0)
        {
            extrawidth = 20;
            if (OwnSpellData.AddHitbox)
            {
                extrawidth += Player.Instance.HitBoxRadius();
            }

            return new Geometry.Polygon.Rectangle(StartPosition, EndPosition.ExtendVector3(StartPosition, -extrawidth), OwnSpellData.Radius + extrawidth);
        }

        public override int GetAvailableTime(Vector2 pos)
        {
            var dist1 =
                Math.Abs((EndPosition.Y - StartPosition.Y) * pos.X - (EndPosition.X - StartPosition.X) * pos.Y +
                         EndPosition.X * StartPosition.Y - EndPosition.Y * StartPosition.X) / StartPosition.Distance(EndPosition);

            var actualDist = Math.Sqrt(StartPosition.Distance(pos).Pow() - dist1.Pow());

            var time = OwnSpellData.MissileSpeed > 0 ? (int)(actualDist / OwnSpellData.MissileSpeed * 1000) : 0;

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

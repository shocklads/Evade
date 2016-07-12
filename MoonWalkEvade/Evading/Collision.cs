using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using MoonWalkEvade.Skillshots.SkillshotTypes;
using MoonWalkEvade.Utils;
using SharpDX;

namespace MoonWalkEvade.Evading
{
    internal static class Collision
    {
        internal class DetectedCollision
        {
            public float Difference;
            public float Distance;
            public Vector2 Position;
            public Obj_AI_Base Unit;
        }

        internal class FastPredResult
        {
            public Vector2 CurrentPos;
            public bool IsMoving;
            public Vector2 PredictedPos;
        }

        public static FastPredResult FastPrediction(Vector2 currentSpellPos, Obj_AI_Base unit, int skillshotActivationDelay, 
            int missileSpeed)
        {
            var tDelay = skillshotActivationDelay / 1000f + currentSpellPos.Distance(unit) / missileSpeed;
            var unitTravelRadius = tDelay * unit.MoveSpeed;
            var unitPath = unit.RealPath();

            if (unitPath.Length > unitTravelRadius)
            {
                return new FastPredResult
                {
                    IsMoving = true,
                    CurrentPos = unit.ServerPosition.To2D(),
                    PredictedPos = unitPath.CutPath((int)unitTravelRadius)[0],
                };
            }

            return new FastPredResult
            {
                IsMoving = false,
                CurrentPos = unitPath[unitPath.Length - 1].To2D(),
                PredictedPos = unitPath[unitPath.Length - 1].To2D(),
            };
        }

        public static Vector2 GetCollisionPoint(this LinearMissileSkillshot skillshot)
        {
            var collisions = new List<DetectedCollision>();
            var currentSpellPos = skillshot.GetPosition().To2D();
            if (!skillshot.OwnSpellData.MinionCollision || skillshot.Missile == null)
                return Vector2.Zero;


            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var minion in
                            EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => !x.IsDead && x.IsValid && x.Health >= 200
                            && x.Distance(skillshot.StartPosition) < skillshot.OwnSpellData.Range))
            {
                var pred = FastPrediction(currentSpellPos, minion,
                    Math.Max(0, skillshot.OwnSpellData.Delay - (Environment.TickCount - skillshot.TimeDetected)),
                        (int)skillshot.OwnSpellData.MissileSpeed);
                var predictedUnitPos = pred.PredictedPos;

                var w = skillshot.OwnSpellData.Radius + minion.BoundingRadius - 15 -
                        predictedUnitPos.Distance(currentSpellPos, skillshot.EndPosition.To2D(), true);
                if (w > 0)
                {
                    collisions.Add(
                        new DetectedCollision
                        {
                            Position =
                                predictedUnitPos.ProjectOn(skillshot.EndPosition.To2D(), skillshot.StartPosition.To2D()).LinePoint +
                                (skillshot.EndPosition.To2D() - skillshot.StartPosition.To2D()).Normalized() * 30,
                            Unit = minion,
                            Distance = predictedUnitPos.Distance(currentSpellPos),
                            Difference = w,
                        });
                }
            }

            var result = collisions.Count > 0 ? collisions.OrderBy(c => c.Distance).ToList().First().Position : Vector2.Zero;

            return result;
        }
    }
}

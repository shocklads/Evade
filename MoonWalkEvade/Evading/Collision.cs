using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using MoonWalkEvade.Skillshots.SkillshotTypes;
using MoonWalkEvade.Utils;
using SharpDX;

namespace MoonWalkEvade.Evading
{
    internal static class Collision
    {
        public static Vector2 GetCollisionPoint(this LinearMissileSkillshot skillshot)
        {
            if (!skillshot.OwnSpellData.MinionCollision || skillshot.Missile == null)
                return Vector2.Zero;

            var collisions = new List<Vector2>();

            var currentSpellPos = skillshot.GetPosition().To2D();
            var spellEndPos = skillshot.EndPosition;
            var rect = new Geometry.Polygon.Rectangle(currentSpellPos, spellEndPos.To2D(), skillshot.OwnSpellData.Radius * 2);

            if (EvadeMenu.CollisionMenu["minion"].Cast<CheckBox>().CurrentValue)
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var minion in
                    EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => !x.IsDead && x.IsValid && x.Health >= 200
                                                                             &&
                                                                             x.Distance(skillshot.StartPosition) <
                                                                             skillshot.OwnSpellData.Range))
                {
                    if (rect.IsInside(minion))
                        collisions.Add(minion.Position.To2D());
                }
            }

            var result = collisions.Count > 0 ? collisions.
                OrderBy(c => c.Distance(currentSpellPos)).ToList().First() : Vector2.Zero;

            return result;
        }
    }
}

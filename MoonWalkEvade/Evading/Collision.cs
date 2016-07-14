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
        private static Vector2 WindWallStartPosition = Vector2.Zero;
        private static int WallCastTick;
        public static void Init()
        {
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
            {
                if (args.SData.Name == "YasuoWMovingWall" && sender.IsAlly)
                {
                    WindWallStartPosition = sender.Position.To2D();
                    WallCastTick = Environment.TickCount;
                }
            };
        }
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
                bool useProj = EvadeMenu.CollisionMenu["useProj"].Cast<CheckBox>().CurrentValue;
                foreach (var minion in
                    EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => !x.IsDead && x.IsValid && x.Health >= 200 &&
                                                        x.Distance(skillshot.StartPosition) < skillshot.OwnSpellData.Range))
                {
                    if (rect.IsInside(minion) && !useProj)
                        collisions.Add(minion.Position.To2D());
                    else if (useProj)
                    {
                        var proj = minion.Position.To2D()
                            .ProjectOn(skillshot.StartPosition.To2D(), skillshot.EndPosition.To2D());
                        if (proj.IsOnSegment && proj.SegmentPoint.Distance(minion) <= skillshot.OwnSpellData.Radius)
                            collisions.Add(proj.SegmentPoint);
                    }
                }
            }
            if (EvadeMenu.CollisionMenu["yasuoWall"].Cast<CheckBox>().CurrentValue && skillshot.Missile != null)
            {
                GameObject wall = null;
                foreach (var gameObject in ObjectManager.Get<GameObject>().
                    Where(gameObject => gameObject.IsValid && System.Text.RegularExpressions.Regex.IsMatch(
                       gameObject.Name, "_w_windwall.\\.troy", System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
                {
                    wall = gameObject;
                }
                if (wall == null)
                    return Vector2.Zero;

                var level = wall.Name.Substring(wall.Name.Length - 6, 1);
                var wallWidth = 300 + 50 * Convert.ToInt32(level);
                var wallDirection = (wall.Position.To2D() - WindWallStartPosition).Normalized().Perpendicular();

                var wallStart = wall.Position.To2D() + wallWidth / 2f * wallDirection;
                var wallEnd = wallStart - wallWidth * wallDirection;
                var wallPolygon = new Geometry.Polygon.Rectangle(wallStart, wallEnd, 75);

                var intersections = wallPolygon.GetIntersectionPointsWithLineSegment(skillshot.GetPosition().To2D(),
                    skillshot.EndPosition.To2D());

                
                if (intersections.Length > 0)
                {
                    float wallDisappearTime = WallCastTick + 250 + 3750 - Environment.TickCount;

                    collisions.AddRange(intersections.Where(intersec => 
                        intersec.Distance(currentSpellPos) / skillshot.OwnSpellData.MissileSpeed*1000 < 
                            wallDisappearTime).ToList());
                }
            }

            var result = collisions.Count > 0 ? collisions.
                OrderBy(c => c.Distance(currentSpellPos)).ToList().First() : Vector2.Zero;

            return result;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using MoonWalkEvade.Utils;
using SharpDX;

namespace MoonWalkEvade.EvadeSpells
{
    public static class EvadeSpellManager
    {
        public static bool ProcessFlash(Evading.MoonWalkEvade moonWalkEvade)
        {
            var dangerValue = moonWalkEvade.GetDangerValue();
            var flashDangerValue = EvadeMenu.SpellMenu["flash"].Cast<Slider>().CurrentValue;

            if (flashDangerValue > 0 && flashDangerValue <= dangerValue)
            {
                var castPos = GetBlinkCastPos(moonWalkEvade, Player.Instance.ServerPosition.To2D(), 425);
                var slot = GetFlashSpellSlot();

                if (!castPos.IsZero && slot != SpellSlot.Unknown && Player.CanUseSpell(slot) == SpellState.Ready)
                {
                    //Player.IssueOrder(GameObjectOrder.Stop, Player.Instance.Position, true);
                    Player.CastSpell(slot, castPos.To3DWorld());
                    return true;
                }
            }

            return false;
        }

        public static SpellSlot GetFlashSpellSlot()
        {
            if (Player.GetSpell(SpellSlot.Summoner1).Name == "summonerflash")
                return SpellSlot.Summoner1;
            if (Player.GetSpell(SpellSlot.Summoner2).Name == "summonerflash")
                return SpellSlot.Summoner2;
            return SpellSlot.Unknown;
        }


        public static Vector2 GetBlinkCastPos(MoonWalkEvade.Evading.MoonWalkEvade moonWalkEvade, Vector2 center, float maxRange)
        {
            var polygons = moonWalkEvade.ClippedPolygons.Where(p => p.IsInside(center)).ToArray();
            var segments = new List<Vector2[]>();

            foreach (var pol in polygons)
            {
                for (var i = 0; i < pol.Points.Count; i++)
                {
                    var start = pol.Points[i];
                    var end = i == pol.Points.Count - 1 ? pol.Points[0] : pol.Points[i + 1];

                    var intersections =
                        Utils.Utils.GetLineCircleIntersectionPoints(center, maxRange, start, end)
                            .Where(p => p.IsInLineSegment(start, end))
                            .ToList();

                    if (intersections.Count == 0)
                    {
                        if (start.Distance(center, true) < maxRange.Pow() &&
                            end.Distance(center, true) < maxRange.Pow())
                        {
                            intersections = new[] { start, end }.ToList();
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (intersections.Count == 1)
                    {
                        intersections.Add(center.Distance(start, true) > center.Distance(end, true)
                            ? end
                            : start);
                    }

                    segments.Add(intersections.ToArray());
                }
            }

            if (!segments.Any())
            {
                return Vector2.Zero;
            }

            const int maxdist = 2000;
            const int division = 30;
            var points = new List<Vector2>();

            foreach (var segment in segments)
            {
                var dist = segment[0].Distance(segment[1]);
                if (dist > maxdist)
                {
                    segment[0] = segment[0].Extend(segment[1], dist / 2 - maxdist / 2);
                    segment[1] = segment[1].Extend(segment[1], dist / 2 - maxdist / 2);
                    dist = maxdist;
                }

                var step = maxdist / division;
                var count = dist / step;

                for (var i = 0; i < count; i++)
                {
                    var point = segment[0].Extend(segment[1], i * step);
                    if (!Extensions.IsWall(point))
                    {
                        points.Add(point);
                    }
                }
            }

            if (!points.Any())
            {
                return Vector2.Zero;
            }

            var evadePoint =
                points.OrderByDescending(p => p.Distance(moonWalkEvade.LastIssueOrderPos) + p.Distance(center)).Last();
            return evadePoint;
        }

        public static bool TryEvadeSpell(Evading.MoonWalkEvade.EvadeResult evadeResult, 
            Evading.MoonWalkEvade moonWalkEvadeInstance)
        {
            IEnumerable<EvadeSpellData> evadeSpells = EvadeMenu.MenuEvadeSpells.Where(evadeSpell => 
                EvadeMenu.SpellMenu[evadeSpell.spellName + "/enable"].Cast<CheckBox>().CurrentValue);

            foreach (EvadeSpellData evadeSpell in evadeSpells)
            {
                int dangerValue =
                        EvadeMenu.MenuEvadeSpells.First(x => x.spellName == evadeSpell.spellName).dangerlevel;
                if (moonWalkEvadeInstance.GetDangerValue() < dangerValue)
                    continue;

                //dash
                if (evadeSpell.range != 0)
                {
                    var evadePos = GetBlinkCastPos(moonWalkEvadeInstance, Player.Instance.Position.To2D(), evadeSpell.range);
                    float castTime = evadeSpell.spellDelay;
                    if (evadeResult.TimeAvailable >= castTime && !evadePos.IsZero && moonWalkEvadeInstance.IsPointSafe(evadePos))
                    {
                        CastEvadeSpell(evadeSpell, evadePos);
                        return true;
                    }
                }

                //speed buff (spell or item)
                if (evadeSpell.evadeType == EvadeType.MovementSpeedBuff)
                {
                    var playerPos = Player.Instance.Position.To2D();

                    float speed = Player.Instance.MoveSpeed;
                    speed += speed * evadeSpell.speedArray[Player.Instance.Spellbook.GetSpell(evadeSpell.spellKey).Level - 1] / 100;
                    float maxTime = evadeResult.TimeAvailable - evadeSpell.spellDelay;
                    float maxTravelDist = speed * (maxTime / 1000);
                    var evadePoints = moonWalkEvadeInstance.GetEvadePoints(playerPos, maxTravelDist);

                    var evadePoint = evadePoints.OrderBy(x => !x.IsUnderTurret()).ThenBy(p => p.Distance(playerPos)).FirstOrDefault();
                    if (evadePoint != default(Vector2))
                    {
                        CastEvadeSpell(evadeSpell, evadeSpell.isItem ? Vector2.Zero : evadePoint);
                        return true;
                    }
                }

                //items
                if (evadeSpell.isItem && evadeSpell.evadeType != EvadeType.MovementSpeedBuff)
                {
                    if (evadeResult.TimeAvailable >= evadeSpell.spellDelay)
                        CastEvadeSpell(evadeSpell, Vector2.Zero);
                    return true;
                }
            }

            return false;
        }

        private static void CastEvadeSpell(EvadeSpellData evadeSpell, Vector2 evadePos)
        {
            bool isItem = evadePos.IsZero;

            if (isItem)
            {
                Item.UseItem(evadeSpell.itemID);
                return;
            }


            switch (evadeSpell.castType)
            {
                case CastType.Position:
                    if (!evadeSpell.isReversed)
                        Player.Instance.Spellbook.CastSpell(evadeSpell.spellKey, evadePos.To3D());
                    else
                        Player.Instance.Spellbook.CastSpell(evadeSpell.spellKey,
                            evadePos.Extend(Player.Instance, evadePos.Distance(Player.Instance) + evadeSpell.range).To3D());
                    break;
                case CastType.Self:
                    Player.Instance.Spellbook.CastSpell(evadeSpell.spellKey, Player.Instance);
                    break;
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using MoonWalkEvade.EvadeSpells;
using MoonWalkEvade.Skillshots;
using MoonWalkEvade.Utils;
using SharpDX;
using Color = System.Drawing.Color;

namespace MoonWalkEvade.Evading
{
    public class MoonWalkEvade
    {
        #region Properties

        public int ServerTimeBuffer => EvadeMenu.MainMenu["serverTimeBuffer"].Cast<Slider>().CurrentValue + 45;

        public bool EvadeEnabled => EvadeMenu.HotkeysMenu["enableEvade"].Cast<KeyBind>().CurrentValue;

        public bool DodgeDangerousOnly => EvadeMenu.HotkeysMenu["dodgeOnlyDangerous"].Cast<KeyBind>().CurrentValue;

        public int ExtraEvadeRange => EvadeMenu.MainMenu["extraEvadeRange"].Cast<Slider>().CurrentValue;

        public bool RandomizeExtraEvadeRange => EvadeMenu.MainMenu["randomizeExtraEvadeRange"].Cast<CheckBox>().CurrentValue;

        public bool AllowRecalculateEvade => EvadeMenu.MainMenu["recalculatePosition"].Cast<CheckBox>().CurrentValue;

        public bool RestorePosition => EvadeMenu.MainMenu["moveToInitialPosition"].Cast<CheckBox>().CurrentValue;

        public bool DisableDrawings => EvadeMenu.DrawMenu["disableAllDrawings"].Cast<CheckBox>().CurrentValue;

        public bool DrawEvadePoint => EvadeMenu.DrawMenu["drawEvadePoint"].Cast<CheckBox>().CurrentValue;

        public bool DrawEvadeStatus => EvadeMenu.DrawMenu["drawEvadeStatus"].Cast<CheckBox>().CurrentValue;

        public bool DrawDangerPolygon => EvadeMenu.DrawMenu["drawDangerPolygon"].Cast<CheckBox>().CurrentValue;

        public int IssueOrderTickLimit => 0;

        #endregion

        #region Vars

        public SpellDetector SpellDetector { get; private set; }
        public PathFinding PathFinding { get; private set; }

        public EvadeSkillshot[] Skillshots { get; private set; }
        public Geometry.Polygon[] Polygons { get; private set; }
        public List<Geometry.Polygon> ClippedPolygons { get; private set; }
        public Vector2 LastIssueOrderPos;

        private readonly Dictionary<EvadeSkillshot, Geometry.Polygon> _skillshotPolygonCache;

        private EvadeResult LastEvadeResult;
        private Text StatusText;
        private int EvadeIssurOrderTime;

        #endregion

        public MoonWalkEvade(SpellDetector detector)
        {
            Skillshots = new EvadeSkillshot[] {};
            Polygons = new Geometry.Polygon[] {};
            ClippedPolygons = new List<Geometry.Polygon>();
            PathFinding = new PathFinding(this);
            StatusText = new Text("MoonWalkEvade", new Font("Euphemia", 10F, FontStyle.Bold)); //Calisto MT
            _skillshotPolygonCache = new Dictionary<EvadeSkillshot, Geometry.Polygon>();

            SpellDetector = detector;
            SpellDetector.OnUpdateSkillshots += OnUpdateSkillshots;
            SpellDetector.OnSkillshotActivation += OnSkillshotActivation;
            SpellDetector.OnSkillshotDetected += OnSkillshotDetected;
            SpellDetector.OnSkillshotDeleted += OnSkillshotDeleted;

            Player.OnIssueOrder += PlayerOnIssueOrder;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Dash.OnDash += OnDash;
            Game.OnTick += Ontick;
            Drawing.OnDraw += OnDraw;
        }

        private void OnUpdateSkillshots(EvadeSkillshot skillshot, bool remove, bool isProcessSpell)
        {
            CacheSkillshots();
            DoEvade();
        }

        private void OnSkillshotActivation(EvadeSkillshot skillshot)
        {
            CacheSkillshots();
            DoEvade();
        }

        private void OnSkillshotDetected(EvadeSkillshot skillshot, bool isProcessSpell)
        {
            //TODO: update
            if (skillshot.ToPolygon().IsInside(Player.Instance))
            {
                LastEvadeResult = null;
            }
        }

        private void OnSkillshotDeleted(EvadeSkillshot skillshot)
        {
            if (RestorePosition && !SpellDetector.DetectedSkillshots.Any())
            {
                if (AutoPathing.IsPathing && Player.Instance.IsWalking())
                {
                    var destination = AutoPathing.Destination;
                    AutoPathing.StopPath();
                    Player.IssueOrder(GameObjectOrder.MoveTo, destination.To3DWorld(), false);
                }
                else if (LastEvadeResult != null && Player.Instance.IsMovingTowards(LastEvadeResult.EvadePoint))
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, LastIssueOrderPos.To3DWorld(), false);
                }
            }
        }

        private void Ontick(EventArgs args)
        {
            if (!Player.Instance.IsWalking() && LastEvadeResult != null)
            {
                //MoveTo(LastEvadeResult.WalkPoint);
            }

            if (IsHeroInDanger() && LastEvadeResult == null)
            {
                DoEvade();
            }
        }

        private void PlayerOnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (args.Order == GameObjectOrder.AttackUnit)
            {
                LastIssueOrderPos =
                    (Player.Instance.Distance(args.Target, true) >
                     Player.Instance.GetAutoAttackRange(args.Target as AttackableUnit).Pow()
                        ? args.Target.Position
                        : Player.Instance.Position).To2D();
            }
            else
            {
                LastIssueOrderPos = (args.Target?.Position ?? args.TargetPosition).To2D();
            }

            CacheSkillshots();
            switch (args.Order)
            {
                case GameObjectOrder.Stop:
                    if (DoEvade(null, args))
                    {
                        args.Process = false;
                    }
                    break;

                case GameObjectOrder.HoldPosition:
                    if (DoEvade(null, args))
                    {
                        args.Process = false;
                    }
                    break;

                case GameObjectOrder.AttackUnit:
                    if (DoEvade(null, args))
                    {
                        args.Process = false;
                    }
                    break;

                default:
                    if (DoEvade(Player.Instance.GetPath(LastIssueOrderPos.To3DWorld(), true), args))
                    {
                        args.Process = false;
                    }
                    break;
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name == "summonerflash")
            {
                LastEvadeResult = null;
            }
        }

        private void OnDash(Obj_AI_Base sender, Dash.DashEventArgs dashEventArgs)
        {
            if (!sender.IsMe || LastEvadeResult == null)
            {
                return;
            }

            LastEvadeResult = null;
            Player.IssueOrder(GameObjectOrder.MoveTo, LastIssueOrderPos.To3DWorld(), false);
        }

        private void OnDraw(EventArgs args)
        {
            if (DisableDrawings)
            {
                return;
            }

            if (DrawEvadePoint && LastEvadeResult != null)
            {
                if (LastEvadeResult.IsValid && LastEvadeResult.EnoughTime && !LastEvadeResult.Expired())
                {
                    Circle.Draw(new ColorBGRA(255, 0, 0, 255), Player.Instance.BoundingRadius, 25, LastEvadeResult.WalkPoint);
                }
            }

            if (DrawEvadeStatus)
            {
                StatusText.Color = EvadeEnabled ? Color.White : Color.Red;
                if (DodgeDangerousOnly)
                    StatusText.Color = Color.DarkOrange;
                StatusText.TextValue = "MoonWalkEvade ";
                StatusText.Position = Player.Instance.Position.WorldToScreen() - new Vector2(StatusText.Bounding.Width / 2f, -25);
                StatusText.Draw();
            }

            if (DrawDangerPolygon)
            {
                foreach (var pol in Geometry.ClipPolygons(SpellDetector.ActiveSkillshots.Select(c => c.ToPolygon())).ToPolygons())
                {
                    pol.Draw(Color.White, 3);
                }
            }
        }

        public void CacheSkillshots()
        {
            Skillshots =
                (DodgeDangerousOnly
                    ? SpellDetector.ActiveSkillshots.Where(c => c.OwnSpellData.IsDangerous)
                    : SpellDetector.ActiveSkillshots).ToArray();

            _skillshotPolygonCache.Clear();
            
            Polygons = Skillshots.Select(c =>
            {
                var pol = c.ToPolygon();
                _skillshotPolygonCache.Add(c, pol);

                return pol;
            }).ToArray();

            ClippedPolygons = Geometry.ClipPolygons(Polygons).ToPolygons();
        }

        public bool IsPointSafe(Vector2 point)
        {
            return !ClippedPolygons.Any(p => p.IsInside(point));
        }

        public bool IsHeroInDanger(AIHeroClient hero = null)
        {
            hero = hero ?? Player.Instance;
            return !IsPointSafe(hero.ServerPosition.To2D());
        }

        public int GetTimeAvailable(AIHeroClient hero = null)
        {
            hero = hero ?? Player.Instance;
            var skillshots = Skillshots.Where(c => _skillshotPolygonCache[c].IsInside(hero.Position)).ToArray();

            if (!skillshots.Any())
            {
                return short.MaxValue;
            }

            var times =
                skillshots.Select(c => c.GetAvailableTime(hero.ServerPosition.To2D()))
                    .Where(t => t > 0)
                    .OrderByDescending(t => t);

            return times.Any() ? times.Last() : short.MaxValue;
        }

        public int GetDangerValue(AIHeroClient hero = null)
        {
            hero = hero ?? Player.Instance;
            var skillshots = Skillshots.Where(c => _skillshotPolygonCache[c].IsInside(hero.Position)).ToArray();

            if (!skillshots.Any())
                return 0;

            var values = skillshots.Select(c => c.OwnSpellData.DangerValue).OrderByDescending(t => t);
            return values.Any() ? values.First() : 0;
        }

        public bool IsPathSafe(Vector2[] path, AIHeroClient hero = null)
        {
            hero = hero ?? Player.Instance;

            for (var i = 0; i < path.Length - 1; i++)
            {
                var start = path[i];
                var end = path[i + 1];

                foreach (var pair in _skillshotPolygonCache)
                {
                    var skillshot = pair.Key;
                    var polygon = pair.Value;

                    if (polygon.IsInside(start) && polygon.IsInside(end))
                    {
                        //var time1 = skillshot.GetAvailableTime(start);
                        var time2 = skillshot.GetAvailableTime(end);

                        if (hero.WalkingTime(start, end) >= time2 - Game.Ping)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        var intersections =
                            polygon.GetIntersectionPointsWithLineSegment(start, end)
                                .Concat(new[] { start, end })
                                .ToList().GetSortedPath(start).ToArray();

                        for (var i2 = 0; i2 < intersections.Length - 1; i2++)
                        {
                            var point1 = intersections[i2];
                            var point2 = intersections[i2 + 1];

                            if (polygon.IsInside(point2) || polygon.IsInside(point1))
                            {
                                //var time1 = polygon.IsInside(point1) ? skillshot.GetAvailableTime(point1) : short.MaxValue;
                                var time2 = polygon.IsInside(point2) ? skillshot.GetAvailableTime(point2) : short.MaxValue;

                                if (hero.WalkingTime(point1, point2) >= time2 - Game.Ping)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public bool IsPathSafe(Vector2 end, AIHeroClient hero = null)
        {
             hero = hero ?? Player.Instance;

            return IsPathSafe(hero.GetPath(end.To3DWorld(), true).ToVector2(), hero);
        }

        public bool CheckPathCollision(Obj_AI_Base unit, Vector2 movePos)
        {
            var path = unit.GetPath(Player.Instance.Position, movePos.To3D());

            if (path.Length > 0)
            {
                if (movePos.Distance(path[path.Length - 1].To2D()) > 5 || path.Length > 2)
                {
                    return true;
                }
            }

            return false;
        }


        public Vector2[] GetBestPositionMovementBlock(Vector2 movePos)
        {
            int posChecked = 0;
            int maxPosToCheck = 100;
            int posRadius = 50;
            int radiusIndex = 0;

            Vector2 heroPoint = Player.Instance.Position.To2D();

            List<Vector2> posTable = new List<Vector2>();

            while (posChecked < maxPosToCheck)
            {
                radiusIndex++;

                int curRadius = radiusIndex * (2 * posRadius);
                int curCircleChecks = (int)Math.Ceiling((2 * Math.PI * curRadius) / (2 * (double)posRadius));

                for (int i = 1; i < curCircleChecks; i++)
                {
                    posChecked++;
                    var cRadians = (2 * Math.PI / (curCircleChecks - 1)) * i; //check decimals
                    var pos = new Vector2((float)Math.Floor(heroPoint.X + curRadius * Math.Cos(cRadians)), (float)Math.Floor(heroPoint.Y + curRadius * Math.Sin(cRadians)));

                    posTable.Add(pos);
                }
            }

            return posTable.ToArray();
        }



        public Vector2[] GetEvadePoints(Vector2 from, float moveRadius)
        {
            var mode = EvadeMenu.MainMenu["evadeMode"].Cast<ComboBox>().CurrentValue;
            if (mode == 1)
            {
                var polygons = ClippedPolygons.Where(p => p.IsInside(from)).ToArray();
                var segments = new List<Vector2[]>();

                foreach (var pol in polygons)
                {
                    for (var i = 0; i < pol.Points.Count; i++)
                    {
                        var start = pol.Points[i];
                        var end = i == pol.Points.Count - 1 ? pol.Points[0] : pol.Points[i + 1];

                        var intersections =
                            Utils.Utils.GetLineCircleIntersectionPoints(from, moveRadius, start, end)
                                .Where(p => p.IsInLineSegment(start, end))
                                .ToList();

                        if (intersections.Count == 0)
                        {
                            if (start.Distance(from, true) < moveRadius.Pow() &&
                                end.Distance(from, true) < moveRadius.Pow())
                            {
                                intersections = new[] {start, end}.ToList();
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (intersections.Count == 1)
                        {
                            intersections.Add(from.Distance(start, true) > from.Distance(end, true) ? end : start);
                        }

                        segments.Add(intersections.ToArray());
                    }
                }

                if (!segments.Any()) //not enough time
                {
                    return new Vector2[] {};
                }

                const int maxdist = 3000;
                const int division = 10;
                var points = new List<Vector2>();

                foreach (var segment in segments)
                {
                    var dist = segment[0].Distance(segment[1]);
                    if (dist > maxdist)
                    {
                        segment[0] = segment[0].Extend(segment[1], dist/2 - maxdist/2f);
                        segment[1] = segment[1].Extend(segment[1], dist/2 - maxdist/2f);
                        dist = maxdist;
                    }

                    var step = maxdist/division;
                    var count = dist/step;

                    for (var i = 0; i < count; i++)
                    {
                        var point = segment[0].Extend(segment[1], i*step);

                        if (!Utils.Utils.IsWall(point) && IsPathSafe(point) &&
                            Player.Instance.GetPath(point.To3DWorld(), true).Length <= 2)
                        {
                            points.Add(point);
                        }
                    }
                }

                return points.ToArray();
            }

            var playerPos = Player.Instance.Position.To2D();

            return GetBestPositionMovementBlock(playerPos).Where(x => IsPointSafe(x) && IsPathSafe(x)).ToArray();
        }

        public Vector2 GetClosestEvadePoint(Vector2 from)
        {
            var polygons = ClippedPolygons.Where(p => p.IsInside(from)).ToArray();

            var polPoints =
                polygons.Select(pol => pol.ToDetailedPolygon())
                    .SelectMany(pol => pol.Points)
                    .OrderByDescending(p => p.Distance(from, true));

            return !polPoints.Any() ? Vector2.Zero : polPoints.Last();
        }

        public EvadeResult CalculateEvade(Vector2 anchor)
        {
            var playerPos = Player.Instance.ServerPosition.To2D();
            var maxTime = GetTimeAvailable();
            var time = Math.Max(0, maxTime - (Game.Ping + ServerTimeBuffer));
            var moveRadius = time / 1000F * Player.Instance.MoveSpeed;

            var points = GetEvadePoints(playerPos, moveRadius).Where(x => !Utils.Utils.IsWall(x));

            if (!points.Any())
            {
                return new EvadeResult(this, Game.CursorPos.To2D(), anchor, maxTime, time, true);
            }

            var evadePoint = points.OrderBy(x => !x.IsUnderTurret()).ThenBy(x => x.Distance(Game.CursorPos)).FirstOrDefault();
            if (evadePoint.Equals(default(Vector2)) && EvadeMenu.MainMenu["evadeMode"].Cast<ComboBox>().CurrentValue == 1)
                evadePoint = points.OrderBy(p => p.Distance(playerPos)).FirstOrDefault();

            return new EvadeResult(this, evadePoint, anchor, maxTime, time,
                !IsHeroInDanger() || Player.Instance.WalkingTime(Player.Instance.Position.To2D(), evadePoint) < time);
        }

        public bool IsHeroPathSafe(EvadeResult evade, Vector3[] desiredPath, AIHeroClient hero = null)
        {
            hero = hero ?? Player.Instance;

            var path = (desiredPath ?? hero.RealPath()).ToVector2();
            return IsPathSafe(path, hero);

            var polygons = ClippedPolygons;
            var points = new List<Vector2>();

            for (var i = 0; i < path.Length - 1; i++)
            {
                var start = path[i];
                var end = path[i + 1];

                foreach (var pol in polygons)
                {
                    var intersections = pol.GetIntersectionPointsWithLineSegment(start, end);
                    if (intersections.Length > 0 && !pol.IsInside(hero))
                    {
                        return false;
                    }

                    points.AddRange(intersections);
                }
            }

            if (points.Count == 1)
            {
                var walkTime = hero.WalkingTime(points[0]);
                return walkTime <= evade.TimeAvailable;
            }

            return false;
        }

        public bool MoveTo(Vector2 point, bool limit = true)
        {
            if (limit && EvadeIssurOrderTime + IssueOrderTickLimit >= Environment.TickCount)
            {
                return false;
            }

            EvadeIssurOrderTime = Environment.TickCount;
            Player.IssueOrder(GameObjectOrder.MoveTo, point.To3DWorld(), false);

            return true;
        }

        public bool MoveTo(Vector3 point, bool limit = true)
        {
            return MoveTo(point.To2D(), limit);
        }

        public bool DoEvade(Vector3[] desiredPath = null, PlayerIssueOrderEventArgs args = null)
        {
            if (!EvadeEnabled || Player.Instance.IsDead || Player.Instance.IsDashing())
            {
                LastEvadeResult = null;
                AutoPathing.StopPath();
                return false;
            }

            var hero = Player.Instance;

            if (args != null && args.Order == GameObjectOrder.AttackUnit)
            {
                if (!hero.IsInAutoAttackRange((AttackableUnit)args.Target))
                {
                    desiredPath = hero.GetPath(args.Target.Position, true);
                }
            }

            if (IsHeroInDanger(hero))
            {
                if (LastEvadeResult != null && (!IsPointSafe(LastEvadeResult.EvadePoint) || LastEvadeResult.Expired()))
                {
                    // LastEvadeResult = null;
                }

                var evade = CalculateEvade(LastIssueOrderPos);
                if (evade.IsValid && evade.EnoughTime)
                {
                    if (LastEvadeResult == null ||
                        (LastEvadeResult.EvadePoint.Distance(evade.EvadePoint, true) > 500.Pow() &&
                         AllowRecalculateEvade))
                    {
                        LastEvadeResult = evade;
                    }
                }
                else
                {
                    return EvadeSpellManager.TryEvadeSpell(evade, this);
                }

                if (LastEvadeResult != null)
                {
                    var isPathSafe = IsHeroPathSafe(evade, desiredPath);

                    if (!hero.IsMovingTowards(LastEvadeResult.WalkPoint) || !isPathSafe)
                    {
                        AutoPathing.StopPath();
                        MoveTo(LastEvadeResult.WalkPoint, false);
                    }

                    return true;
                }
            }
            else if (!IsPathSafe(hero.RealPath().ToVector2()) || (desiredPath != null && !IsPathSafe(desiredPath.ToVector2())))
            {
                var path = PathFinding.GetPath(hero.Position.To2D(), LastIssueOrderPos);
                var evade = CalculateEvade(LastIssueOrderPos);

                if (evade.IsValid)
                {
                    path = new[] { evade.EvadePoint }.Concat(path).ToArray();
                }

                if (path.Length > 0 && AutoPathing.Destination.Distance(path.Last(), true) > 50.Pow())
                {
                    AutoPathing.DoPath(path);
                }

                LastEvadeResult = null;
                return desiredPath != null;
            }
            else
            {
                AutoPathing.StopPath();
                LastEvadeResult = null;
            }

            return false;
        }

        public class EvadeResult
        {
            private MoonWalkEvade _moonWalkEvade;
            private int ExtraRange { get; set; }

            public int Time { get; set; }
            public Vector2 PlayerPos { get; set; }
            public Vector2 EvadePoint { get; set; }
            public Vector2 AnchorPoint { get; set; }
            public int TimeAvailable { get; set; }
            public int TotalTimeAvailable { get; set; }
            public bool EnoughTime { get; set; }

            public bool OutsideEvade => Environment.TickCount - OutsideEvadeTime <= 500;

            public int OutsideEvadeTime { get; set; }

            public bool IsValid => !EvadePoint.IsZero;

            public Vector3 WalkPoint
            {
                get
                {
                    var walkPoint = EvadePoint.Extend(PlayerPos, -80);
                    var newPoint = walkPoint.Extend(PlayerPos, -ExtraRange);
                   
                    if (_moonWalkEvade.IsPointSafe(newPoint))
                    {
                        return newPoint.To3DWorld();
                    }

                    return walkPoint.To3DWorld();
                }
            }

            public EvadeResult(MoonWalkEvade moonWalkEvade, Vector2 evadePoint, Vector2 anchorPoint, int totalTimeAvailable,
                int timeAvailable,
                bool enoughTime)
            {
                _moonWalkEvade = moonWalkEvade;
                PlayerPos = Player.Instance.Position.To2D();
                Time = Environment.TickCount;

                EvadePoint = evadePoint;
                AnchorPoint = anchorPoint;
                TotalTimeAvailable = totalTimeAvailable;
                TimeAvailable = timeAvailable;
                EnoughTime = enoughTime;

                // extra _moonWalkEvade Range
                if (_moonWalkEvade.ExtraEvadeRange > 0)
                {
                    ExtraRange = (_moonWalkEvade.RandomizeExtraEvadeRange
                        ? Utils.Utils.Random.Next(_moonWalkEvade.ExtraEvadeRange / 3, _moonWalkEvade.ExtraEvadeRange)
                        : _moonWalkEvade.ExtraEvadeRange);
                }
            }

            public bool Expired(int time = 4000)
            {
                return Elapsed(time);
            }

            public bool Elapsed(int time)
            {
                return Elapsed() > time;
            }

            public int Elapsed()
            {
                return Environment.TickCount - Time;
            }
        }
    }
}
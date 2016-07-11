using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace MoonWalkEvade.Evading
{
    public static class AutoPathing
    {
        public static Vector2[] Path { get; private set; }
        public static bool IsPathing { get; private set; }
        public static int Index;

        public static Vector2 CurrentPoint => Path[Index];

        public static Vector2 Destination
        {
            get
            {
                if (Path == null || Path.Length == 0 || !IsPathing)
                {
                    return Vector2.Zero;
                }

                return Path.Last();
            }
        }

        public static float SwitchDistance => (Game.Ping)*Player.Instance.MoveSpeed/1000 + 90;

        static AutoPathing()
        {
            Game.OnTick += OnTick;
        }


        private static void OnTick(EventArgs args)
        {
            if (Path == null || Index > Path.Length - 1 || Player.Instance.IsDead)
                StopPath();

            if (!IsPathing || !Player.Instance.CanMove)
                return;

            if (Player.Instance.ServerPosition.To2D().Distance(CurrentPoint, true) <= SwitchDistance.Pow())
            {
                Index += 1;
            }

            if (Index <= Path.Length - 1 &&
                (Player.Instance.Path.Last().Distance(CurrentPoint, true) > SwitchDistance.Pow()))
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, CurrentPoint.To3DWorld(), false);
            }
        }

        public static void StopPath()
        {
            Path = null;
            IsPathing = false;
        }

        public static void DoPath(Vector2[] path)
        {
            if (path == null || path.Length == 0)
                return;

            Path = path;
            IsPathing = true;
            Index = 0;
        }
    }
}
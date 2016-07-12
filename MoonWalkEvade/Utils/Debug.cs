using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace MoonWalkEvade.Utils
{
    static class Debug
    {
        public static Vector2 GlobalEndPos = Vector2.Zero, GlobalStartPos = Vector2.Zero;

        static Debug()
        {
            Drawing.OnDraw += args =>
            {
                if (!GlobalEndPos.IsZero)
                    new Circle {Color = System.Drawing.Color.DodgerBlue, Radius = 100}.Draw(GlobalEndPos.To3D());
                if (!GlobalStartPos.IsZero)
                    new Circle { Color = System.Drawing.Color.Red, Radius = 100 }.Draw(GlobalStartPos.To3D());
            };
        }
    }
}

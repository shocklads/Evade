using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace MoonWalkEvade.Utils
{
    static class Debug
    {
        public static Vector2 GlobalEndPos = Vector2.Zero, GlobalStartPos = Vector2.Zero;

        public static void Init()
        {
            Game.OnWndProc += GameOnOnWndProc;
            Drawing.OnDraw += args =>
            {
                if (!GlobalEndPos.IsZero)
                    new Circle { Color = System.Drawing.Color.DodgerBlue, Radius = 100 }.Draw(GlobalEndPos.To3D());
                if (!GlobalStartPos.IsZero)
                    new Circle { Color = System.Drawing.Color.Red, Radius = 100 }.Draw(GlobalStartPos.To3D());
            };
        }

        private static void GameOnOnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown && EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
            {
                if (GlobalEndPos.IsZero)
                    GlobalEndPos = Game.CursorPos.To2D();
                else if (GlobalStartPos.IsZero)
                    GlobalStartPos = Game.CursorPos.To2D();
            }
        }

        
    }
}

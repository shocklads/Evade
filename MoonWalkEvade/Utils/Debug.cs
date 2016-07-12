using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using MoonWalkEvade.Skillshots;
using SharpDX;

namespace MoonWalkEvade.Utils
{
    static class Debug
    {
        public static Vector3 GlobalEndPos = Vector3.Zero, GlobalStartPos = Vector3.Zero;
        private static SpellDetector spellDetector;
        public static int LastCreationTick;

        public static void Init(ref SpellDetector detector)
        {
            spellDetector = detector;

            Game.OnWndProc += GameOnOnWndProc;
            Drawing.OnDraw += args =>
            {
                if (!GlobalEndPos.IsZero)
                    new Circle { Color = System.Drawing.Color.DodgerBlue, Radius = 100 }.Draw(GlobalEndPos);
                if (!GlobalStartPos.IsZero)
                    new Circle { Color = System.Drawing.Color.Red, Radius = 100 }.Draw(GlobalStartPos);
            };
            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (!EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
                return;

            if (GlobalStartPos.IsZero || GlobalEndPos.IsZero)
                return;

            if (Environment.TickCount - LastCreationTick < EvadeMenu.HotkeysMenu["debugModeIntervall"].Cast<Slider>().CurrentValue)
                return;

            LastCreationTick = Environment.TickCount;
            var skillshot =
                SkillshotDatabase.Database.First(
                    evadeSkillshot => evadeSkillshot.OwnSpellData.SpellName == "FlashFrostSpell");

            var nSkillshot = skillshot.NewInstance(true);
            spellDetector.AddSkillshot(nSkillshot);
        }

        private static void GameOnOnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown && EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
            {
                if (GlobalEndPos.IsZero)
                    GlobalEndPos = Game.CursorPos;
                else if (GlobalStartPos.IsZero)
                    GlobalStartPos = Game.CursorPos;
            }
        }

        
    }
}

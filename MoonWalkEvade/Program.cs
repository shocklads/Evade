using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using MoonWalkEvade.Skillshots;
using MoonWalkEvade.Utils;

namespace MoonWalkEvade
{
    internal static class Program
    {
        public static bool DeveloperMode = false;

        private static SpellDetector _spellDetector;
        private static Evading.MoonWalkEvade _moonWalkEvade;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += delegate
            {
                _spellDetector = new SpellDetector(DeveloperMode ? DetectionTeam.AnyTeam : DetectionTeam.EnemyTeam);
                _moonWalkEvade = new Evading.MoonWalkEvade(_spellDetector);
                EvadeMenu.CreateMenu();

                if (EvadeMenu.MainMenu["serverTimeBuffer"].Cast<Slider>().CurrentValue < 80) 
                Core.DelayAction(() =>
                    Chat.Print("<b><font color =\"#52A8FF\">Recommended MoonWalkEvade - Server Time Buffer: 80</font></b>"),
                        3000);
            };

            Game.OnWndProc += GameOnOnWndProc;
        }

        private static void GameOnOnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint) WindowMessages.LeftButtonDown && EvadeMenu.HotkeysMenu["debugMode"].Cast<KeyBind>().CurrentValue)
            {
                if (Debug.GlobalEndPos.IsZero)
                    Debug.GlobalEndPos = Game.CursorPos.To2D();
                else if (Debug.GlobalStartPos.IsZero)
                    Debug.GlobalStartPos = Game.CursorPos.To2D();
            }
        }
    }
}

using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Events;
using MoonWalkEvade.Evading;
using MoonWalkEvade.Skillshots;
using MoonWalkEvade.Utils;

namespace MoonWalkEvade
{
    internal static class Program
    {
        public static bool DeveloperMode = true;

        private static SpellDetector _spellDetector;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += delegate
            {
                _spellDetector = new SpellDetector(DeveloperMode ? DetectionTeam.AnyTeam : DetectionTeam.EnemyTeam);
                new Evading.MoonWalkEvade(_spellDetector);
                EvadeMenu.CreateMenu();

                Collision.Init();
                Debug.Init(ref _spellDetector);
            };
        }
    }
}

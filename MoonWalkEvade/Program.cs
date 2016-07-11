using EloBuddy.SDK.Events;
using MoonWalkEvade.Skillshots;

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
            };
        }
    }
}

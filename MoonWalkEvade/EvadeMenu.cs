using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using MoonWalkEvade.EvadeSpells;
using MoonWalkEvade.Skillshots;

namespace MoonWalkEvade
{
    internal class EvadeMenu
    {
        public static Menu MainMenu { get; private set; }
        public static Menu SkillshotMenu { get; private set; }
        public static Menu SpellMenu { get; private set; }
        public static Menu DrawMenu { get; private set; }
        public static Menu HotkeysMenu { get; private set; }

        public static Menu CollisionMenu { get; private set; }

        public static readonly Dictionary<string, EvadeSkillshot> MenuSkillshots = new Dictionary<string, EvadeSkillshot>();
        public static readonly List<EvadeSpellData> MenuEvadeSpells = new List<EvadeSpellData>(); 

        public static void CreateMenu()
        {
            if (MainMenu != null)
            {
                return;
            }

            MainMenu = EloBuddy.SDK.Menu.MainMenu.AddMenu("MoonWalkEvade", "MoonWalkEvade");

            // Set up main menu
            MainMenu.AddGroupLabel("General Settings");
            MainMenu.Add("evadeMode", new ComboBox("Evade Mode", 0, "Smooth - EzEvade Style", "Fast - EvadePlus Style"));
            MainMenu.AddSeparator();

            MainMenu.Add("fowDetection", new CheckBox("Enable FOW Detection"));
            MainMenu.Add("processSpellDetection", new CheckBox("Enable Fast Spell Detection"));
            MainMenu.Add("limitDetectionRange", new CheckBox("Limit Spell Detection Range"));
            MainMenu.Add("recalculatePosition", new CheckBox("Allow Recalculation Of Evade EndPosition", false));
            MainMenu.Add("moveToInitialPosition", new CheckBox("Move To Desired EndPosition After Evade", false));
            MainMenu.Add("serverTimeBuffer", new Slider("Server Time Buffer Delay", 80, 0, 200));
            MainMenu.AddSeparator();

            MainMenu.AddGroupLabel("Humanizer");
            MainMenu.Add("skillshotActivationDelay", new Slider("Reaction Delay", 0, 0, 400));
            MainMenu.AddSeparator();

            MainMenu.Add("extraEvadeRange", new Slider("Extra Evade Range", 0, 0, 300));
            MainMenu.Add("randomizeExtraEvadeRange", new CheckBox("Randomize Extra Range", false));

            var heroes = Program.DeveloperMode ? EntityManager.Heroes.AllHeroes : EntityManager.Heroes.Enemies;
            var heroNames = heroes.Select(obj => obj.ChampionName).ToArray();
            var skillshots =
                SkillshotDatabase.Database.Where(s => heroNames.Contains(s.OwnSpellData.ChampionName)).ToList();
            skillshots.AddRange(
                SkillshotDatabase.Database.Where(
                    s =>
                        s.OwnSpellData.ChampionName == "AllChampions" &&
                        heroes.Any(obj => obj.Spellbook.Spells.Select(c => c.Name).Contains(s.OwnSpellData.SpellName))));
            var evadeSpells =
                EvadeSpellDatabase.Spells.Where(s => Player.Instance.ChampionName.Contains(s.charName)).ToList();
            evadeSpells.AddRange(EvadeSpellDatabase.Spells.Where(s => s.charName == "AllChampions"));


            SkillshotMenu = MainMenu.AddSubMenu("Skillshots");

            foreach (var c in skillshots)
            {
                var skillshotString = c.ToString().ToLower();

                if (MenuSkillshots.ContainsKey(skillshotString))
                    continue;

                MenuSkillshots.Add(skillshotString, c);

                SkillshotMenu.AddGroupLabel(c.DisplayText);
                SkillshotMenu.Add(skillshotString + "/enable", new CheckBox("Dodge", c.OwnSpellData.EnabledByDefault));
                SkillshotMenu.Add(skillshotString + "/draw", new CheckBox("Draw"));

                var dangerous = new CheckBox("Dangerous", c.OwnSpellData.IsDangerous);
                dangerous.OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
                {
                    GetSkillshot(sender.SerializationId).OwnSpellData.IsDangerous = args.NewValue;
                };
                SkillshotMenu.Add(skillshotString + "/dangerous", dangerous);

                var dangerValue = new Slider("Danger Value", c.OwnSpellData.DangerValue, 1, 5);
                dangerValue.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    GetSkillshot(sender.SerializationId).OwnSpellData.DangerValue = args.NewValue;
                };
                SkillshotMenu.Add(skillshotString + "/dangervalue", dangerValue);

                SkillshotMenu.AddSeparator();
            }

            // Set up spell menu
            SpellMenu = MainMenu.AddSubMenu("Evading Spells");
            SpellMenu.AddGroupLabel("Flash");
            SpellMenu.Add("flash", new Slider("Danger Value", 5, 0, 5));

            foreach (var e in evadeSpells)
            {
                var evadeSpellString = e.spellName;

                if (MenuEvadeSpells.Any(x => x.spellName == evadeSpellString))
                    continue;

                MenuEvadeSpells.Add(e);

                SpellMenu.AddGroupLabel(evadeSpellString);
                SpellMenu.Add(evadeSpellString + "/enable", new CheckBox("Use " + e.spellKey));

                var dangerValueSlider = new Slider("Danger Value", e.dangerlevel, 1, 5);
                dangerValueSlider.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    MenuEvadeSpells.First(x => 
                        x.spellName.Contains(sender.SerializationId.Split('/')[0])).dangerlevel = args.NewValue;
                };
                SpellMenu.Add(evadeSpellString + "/dangervalue", dangerValueSlider);

                SpellMenu.AddSeparator();
            }


            DrawMenu = MainMenu.AddSubMenu("Drawings");
            DrawMenu.Add("disableAllDrawings", new CheckBox("Disable All Drawings", false));
            DrawMenu.Add("drawEvadePoint", new CheckBox("Draw Evade Point"));
            DrawMenu.Add("drawEvadeStatus", new CheckBox("Draw Evade Status"));
            DrawMenu.Add("drawDangerPolygon", new CheckBox("Draw Danger Polygon"));


            HotkeysMenu = MainMenu.AddSubMenu("KeyBinds");
            HotkeysMenu.Add("enableEvade", new KeyBind("Enable Evade", true, KeyBind.BindTypes.PressToggle, 'M'));
            HotkeysMenu.Add("dodgeOnlyDangerous", new KeyBind("Dodge Only Dangerous", false, KeyBind.BindTypes.HoldActive));
            HotkeysMenu.Add("debugMode", new KeyBind("Debug Mode", false, KeyBind.BindTypes.PressToggle));


        }

        private static EvadeSkillshot GetSkillshot(string s)
        {
            return MenuSkillshots[s.ToLower().Split('/')[0]];
        }

        public static bool IsSkillshotEnabled(EvadeSkillshot skillshot)
        {
            var valueBase = SkillshotMenu[skillshot + "/enable"];
            return valueBase != null && valueBase.Cast<CheckBox>().CurrentValue;
        }

        public static bool IsSkillshotDrawingEnabled(EvadeSkillshot skillshot)
        {
            var valueBase = SkillshotMenu[skillshot + "/draw"];
            return valueBase != null && valueBase.Cast<CheckBox>().CurrentValue;
        }
    }
}
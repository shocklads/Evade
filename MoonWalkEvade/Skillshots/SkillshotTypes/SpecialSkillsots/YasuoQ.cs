﻿using EloBuddy;
using EloBuddy.SDK;
using MoonWalkEvade.Utils;

namespace MoonWalkEvade.Skillshots.SkillshotTypes.SpecialSkillsots
{
    public class YasuoQ : LinearMissileSkillshot
    {
        public override EvadeSkillshot NewInstance()
        {
            var newInstance = new YasuoQ { SpellData = SpellData };
            return newInstance;
        }

        public override void OnSpellDetection(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            _startPos = Caster.ServerPosition;
            _endPos = _startPos.ExtendVector3(EndPos.To3D(), -SpellData.Range);
        }
    }
}

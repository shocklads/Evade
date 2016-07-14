using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace MoonWalkEvade.Skillshots
{
    public abstract class EvadeSkillshot
    {
        public SpellDetector SpellDetector { get; set; }
        public GameObject SpawnObject { get; set; }
        public Obj_AI_Base Caster { get; set; }
        public GameObjectProcessSpellCastEventArgs CastArgs { get; set; }
        public EloBuddy.SpellData SData { get; set; }
        public SpellData OwnSpellData { get; set; }
        public GameObjectTeam Team { get; set; }
        public bool IsActive { get; set; }
        public bool IsValid { get; set; }
        public bool CastComplete { get; set; }
        public int TimeDetected { get; set; }

        public bool IsProcessSpellCast => Caster != null;

        public string DisplayText => $"{OwnSpellData.ChampionName} {OwnSpellData.Slot} - {OwnSpellData.DisplayName}";

        public abstract Vector3 GetPosition();

        public abstract void OnCreateObject(GameObject obj);

        public virtual void OnDeleteObject(GameObject obj) { }

        public virtual void OnCreate(GameObject obj) { }

        public virtual bool OnDelete(GameObject obj)
        {
            return true;
        }

        public virtual void OnDispose() { }

        public abstract void OnDraw();

        public abstract void OnTick();

        public virtual void OnSpellDetection(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) { }

        public abstract Geometry.Polygon ToRealPolygon();

        public abstract Geometry.Polygon ToPolygon(float extrawidth = 0);

        public abstract int GetAvailableTime(Vector2 pos);

        public abstract bool IsFromFow();

        public abstract EvadeSkillshot NewInstance(bool debug = false);

        public override string ToString()
        {
            return $"{OwnSpellData.ChampionName}_{OwnSpellData.Slot}_{OwnSpellData.DisplayName}";
        }
    }
}
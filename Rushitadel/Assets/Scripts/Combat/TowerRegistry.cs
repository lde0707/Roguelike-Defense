using System.Collections.Generic;

namespace Rushitadel.Combat
{
    public static class TowerRegistry
    {
        private static readonly HashSet<TowerController> Towers = new();

        public static IEnumerable<TowerController> ActiveTowers => Towers;

        public static void Register(TowerController tower) => Towers.Add(tower);
        public static void Unregister(TowerController tower) => Towers.Remove(tower);
    }
}

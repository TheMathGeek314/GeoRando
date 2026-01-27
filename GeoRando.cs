using Modding;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;

namespace GeoRando {
    public class GeoRando: Mod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings> {
        new public string GetName() => "GeoRando";
        public override string GetVersion() => "1.0.1.1";

        public static GlobalSettings Settings { get; set; } = new();
        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

        public static LocalSettings localSettings { get; set; } = new();
        public void OnLoadLocal(LocalSettings s) => localSettings = s;
        public LocalSettings OnSaveLocal() => localSettings;

        internal static GeoRando instance;

        public GeoRando() : base(null) {
            instance = this;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            FlingObjectsFromGlobalPool[] flingActions = preloadedObjects["Crossroads_10"]["Chest"].LocateMyFSM("Chest Control").GetState("Spawn Items").GetActionsOfType<FlingObjectsFromGlobalPool>();
            GeoMultiLocation.smallPrefab = flingActions[0].gameObject.Value;
            GeoMultiLocation.medPrefab = flingActions[1].gameObject.Value;
            GeoMultiLocation.largePrefab = flingActions[2].gameObject.Value;

            RandoInterop.Hook();
        }

        public override List<(string, string)> GetPreloadNames() {
            return [
                ("Crossroads_10", "Chest")
            ];
        }
    }
}
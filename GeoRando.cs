using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace GeoRando {
    public class GeoRando: Mod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings> {
        new public string GetName() => "GeoRando";
        public override string GetVersion() => "1.0.0.0";

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
            RandoInterop.Hook();
        }
    }
}

//TODO
//  Do colos and grubfather
//  repeatable chest checks
//  Geo rocks aren't being broken when empty on room load
//  Geo rocks think they should be breakable when checks haven't refreshed yet
//  Geo rocks should turn yellow or something when all unique checks have been obtained but refreshed checks are available
//  Mantis Lords geo chest didn't work for blossom
//  test togglable recent items filter
//  can GeoItem be something other than AbstractItem so that it doesn't appear as a shiny?
//  fstats GeoControl IL is broken
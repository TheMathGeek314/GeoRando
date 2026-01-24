using Modding;
using System.IO;
using System.Linq;
using System.Reflection;
using ItemChanger;
using ItemChanger.Modules;
using ItemChanger.Tags;
using RandomizerMod.RC;

namespace GeoRando {
    internal static class RandoInterop {
        public static void Hook() {
            RandoMenuPage.Hook();
            RequestModifier.Hook();
            LogicAdder.Hook();

            GeoMultiLocation.prepReflection();

            DefineLocations();
            DefineItems();

            RandoController.OnExportCompleted += EditModules;

            if(ModHooks.GetMod("RandoSettingsManager") is Mod)
                RSMInterop.Hook();
        }

        private static void EditModules(RandoController controller) {
            if(GeoRando.Settings.Grubfather) {
                ItemChangerMod.Modules.Remove(ItemChangerMod.Modules.GetOrAdd<FastGrubfather>());
            }
        }

        public static void DefineLocations() {
            static void DefineLoc(AbstractLocation loc, string scene, float x, float y) {
                InteropTag tag = AddTag(loc);
                tag.Properties["PinSprite"] = new EmbeddedSprite("geopin");
                if(scene != SceneNames.Crossroads_38)
                    tag.Properties["WorldMapLocation"] = (scene, x, y);
                Finder.DefineCustomLocation(loc);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            string geoDataName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("GeoData.json"));
            using Stream geoStream = assembly.GetManifestResourceStream(geoDataName);

            foreach(JsonGeoData geoData in new ParseJson(geoStream).parseFile<JsonGeoData>()) {
                geoData.translate();
                string genericName = JsonGeoData.convertLocationName(geoData.icName);
                for(int i = 1; i <= geoData.copies; i++) {
                    GeoMultiLocation geoLoc = new() { name = $"{genericName} ({i})", sceneName = geoData.scene };
                    DefineLoc(geoLoc, geoData.scene, geoData.x, geoData.y);
                }
            }
        }

        public static void DefineItems() {
            SmallGeoItem smallGeo = new();
            Finder.DefineCustomItem(smallGeo);

            MediumGeoItem medGeo = new();
            Finder.DefineCustomItem(medGeo);

            LargeGeoItem largeGeo = new();
            Finder.DefineCustomItem(largeGeo);

            SmallColoGeoItem smallColo = new();
            Finder.DefineCustomItem(smallColo);

            MediumColoGeoItem medColo = new();
            Finder.DefineCustomItem(medColo);

            LargeColoGeoItem largeColo = new();
            Finder.DefineCustomItem(largeColo);
        }

        public static InteropTag AddTag(TaggableObject obj) {
            InteropTag tag = obj.GetOrAddTag<InteropTag>();
            tag.Message = "RandoSupplementalMetadata";
            tag.Properties["ModSource"] = GeoRando.instance.GetName();
            return tag;
        }
    }
}

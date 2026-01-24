using System.IO;
using System.Reflection;
using RandomizerCore;
using RandomizerCore.Json;
using RandomizerCore.Logic;
using RandomizerCore.LogicItems;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace GeoRando {
    public static class LogicAdder {
        public static void Hook() {
            RCData.RuntimeLogicOverride.Subscribe(50, ApplyLogic);
        }

        private static void ApplyLogic(GenerationSettings gs, LogicManagerBuilder lmb) {
            if(!GeoRando.Settings.Any)
                return;
            JsonLogicFormat fmt = new();
            Assembly assembly = typeof(LogicAdder).Assembly;
            
            using Stream s = assembly.GetManifestResourceStream("GeoRando.Resources.logic.json");
            lmb.DeserializeFile(LogicFileType.Locations, fmt, s);

            using Stream st = assembly.GetManifestResourceStream("GeoRando.Resources.logicSubstitutions.json");
            lmb.DeserializeFile(LogicFileType.LogicSubst, fmt, st);

            DefineItems(lmb);
        }

        private static void DefineItems(LogicManagerBuilder lmb) {
            lmb.AddItem(new SingleItem(Consts.SmallGeo, new TermValue(lmb.GetTerm("GEO"), 1)));
            lmb.AddItem(new SingleItem(Consts.MedGeo, new TermValue(lmb.GetTerm("GEO"), 5)));
            lmb.AddItem(new SingleItem(Consts.LargeGeo, new TermValue(lmb.GetTerm("GEO"), 25)));

            lmb.AddItem(new SingleItem(Consts.SmallColoGeo, new TermValue(lmb.GetTerm("GEO"), 1)));
            lmb.AddItem(new SingleItem(Consts.MedColoGeo, new TermValue(lmb.GetTerm("GEO"), 5)));
            lmb.AddItem(new SingleItem(Consts.LargeColoGeo, new TermValue(lmb.GetTerm("GEO"), 25)));
        }
    }
}

using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace GeoRando {
    internal static class RSMInterop {
        public static void Hook() {
            RandoSettingsManagerMod.Instance.RegisterConnection(new GrSettingsProxy());
        }
    }

    internal class GrSettingsProxy: RandoSettingsProxy<GlobalSettings, string> {
        public override string ModKey => GeoRando.instance.GetName();

        public override VersioningPolicy<string> VersioningPolicy { get; } = new EqualityVersioningPolicy<string>(GeoRando.instance.GetVersion());

        public override void ReceiveSettings(GlobalSettings settings) {
            settings ??= new();
            RandoMenuPage.Instance.ResetMenu(settings);
        }

        public override bool TryProvideSettings(out GlobalSettings settings) {
            settings = GeoRando.Settings;
            return settings.Any;
        }
    }
}
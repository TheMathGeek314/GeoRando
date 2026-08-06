using ConnectionSettingsRando;

namespace GeoRando {
    internal class CSRInterop {
        public static void Hook() {
            CSR.Register(
                GeoRando.instance.GetName(),
                () => GeoRando.Settings,
                s => SettingsRandomizer.CopyTo(s, GeoRando.Settings)
            );
        }
    }
}

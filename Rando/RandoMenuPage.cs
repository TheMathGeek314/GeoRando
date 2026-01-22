using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using static RandomizerMod.Localization;

namespace GeoRando {
    public class RandoMenuPage {
        internal MenuPage GeoRandoPage;
        internal MenuElementFactory<GlobalSettings> grMEF;
        internal VerticalItemPanel grVIP;

        internal SmallButton JumpToGRButton;

        internal static RandoMenuPage Instance { get; private set; }

        public static void OnExitMenu() {
            Instance = null;
        }

        public static void Hook() {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button) {
            button = Instance.JumpToGRButton;
            return true;
        }

        private void SetTopLevelButtonColor() {
            if(JumpToGRButton != null) {
                JumpToGRButton.Text.color = GeoRando.Settings.Any ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            }
        }

        private static void ConstructMenu(MenuPage landingPage) => Instance = new(landingPage);

        private RandoMenuPage(MenuPage landingPage) {
            GeoRandoPage = new MenuPage(Localize("GeoRando"), landingPage);
            grMEF = new(GeoRandoPage, GeoRando.Settings);
            grVIP = new(GeoRandoPage, new(0, 300), 75f, true, grMEF.Elements);
            Localize(grMEF);
            foreach(IValueElement e in grMEF.Elements) {
                e.SelfChanged += obj => SetTopLevelButtonColor();
            }

            JumpToGRButton = new(landingPage, Localize("GeoRando"));
            JumpToGRButton.AddHideAndShowEvent(landingPage, GeoRandoPage);
            SetTopLevelButtonColor();
        }

        internal void ResetMenu(GlobalSettings settings) {
            grMEF.SetMenuValues(settings);
            SetTopLevelButtonColor();
        }
    }
}

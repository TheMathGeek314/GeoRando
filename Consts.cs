namespace GeoRando {
    public class Consts {
        public const string SmallGeo = "SmallGeoPiece";
        public const string MedGeo = "MediumGeoPiece";
        public const string LargeGeo = "LargeGeoPiece";
    }

    public class Quantities {
        private const int rockSmallPieces = 4656;
        private const int rockMedPieces = 84;
        private const int rockLargePieces = 0;

        private const int chestSmallPieces = 134;
        private const int chestMedPieces = 109;
        private const int chestLargePieces = 68;

        //these add to 100 locations and multiply to 1000 with their size values
        private const int coloSmallPieces = 0;//35;
        private const int coloMedPieces = 0;//33;
        private const int coloLargePieces = 0;//32;

        private const int grubSmallPieces = 0;
        private const int grubMedPieces = 0;
        private const int grubLargePieces = 0;

        //1xColo1 + 2xColo2 + 3xColo3
        private static int allColoSmall => coloSmallPieces * 6;
        private static int allColoMedium => coloMedPieces * 6;
        private static int allColoLarge => coloLargePieces * 6;

        public static (int, int, int) totalGeoPieces() {
            int small = 0;
            int medium = 0;
            int large = 0;
            GlobalSettings gs = GeoRando.Settings;
            if(gs.Rocks) {
                small += rockSmallPieces;
                medium += rockMedPieces;
                large += rockLargePieces;
            }
            if(gs.Chests) {
                small += chestSmallPieces;
                medium += chestMedPieces;
                large += chestLargePieces;
            }
            if(gs.Colosseum) {
                small += allColoSmall;
                medium += allColoMedium;
                large += allColoLarge;
            }
            if(gs.GrubFather) {
                small += grubSmallPieces;
                medium += grubMedPieces;
                large += grubLargePieces;
            }
            return (small, medium, large);
        }
    }
}

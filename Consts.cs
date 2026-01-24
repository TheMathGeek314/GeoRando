using System.Collections.Generic;
using RandomizerMod.RC;

namespace GeoRando {
    public class Consts {
        public const string SmallGeo = "SmallGeoPiece";
        public const string MedGeo = "MediumGeoPiece";
        public const string LargeGeo = "LargeGeoPiece";
        public const string SmallColoGeo = "SmallColoGeoPiece";
        public const string MedColoGeo = "MediumColoGeoPiece";
        public const string LargeColoGeo = "LargeColoGeoPiece";
    }

    public class Quantities {
        private const int rockSmallPieces = 4656;
        private const int rockMedPieces = 84;
        private const int rockLargePieces = 0;

        private const int chestSmallPieces = 134;
        private const int chestMedPieces = 109;
        private const int chestLargePieces = 68;

        public const int coloBronzeSmallPieces = 35;
        public const int coloBronzeMedPieces = 33;
        public const int coloBronzeLargePieces = 32;
        public const int coloSilverSmallPieces = 0;
        public const int coloSilverMedPieces = 70;
        public const int coloSilverLargePieces = 66;
        public const int coloGoldSmallPieces = 0;
        public const int coloGoldMedPieces = 100;
        public const int coloGoldLargePieces = 100;

        public static (int, int, int) totalGeoPieces(RequestBuilder rb) {
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
            if(gs.Grubfather) {
                for(int i = 1; i <= rb.gs.CostSettings.GrubTolerance; i++) {
                    (int s, int m, int l) = GrubCounts.geoSizes[i];
                    small += s;
                    medium += m;
                    large += l;
                }
            }
            return (small, medium, large);
        }

        public static (int, int, int) totalColoGeoPieces() {
            if(GeoRando.Settings.Colosseum) {
                int small = coloBronzeSmallPieces + coloSilverSmallPieces + coloGoldSmallPieces;
                int medium = coloBronzeMedPieces + coloSilverMedPieces + coloGoldMedPieces;
                int large = coloBronzeLargePieces + coloSilverLargePieces + coloGoldLargePieces;
                return (small, medium, large);
            }
            return (0, 0, 0);
        }

        public static (int, int, int) totalColoLocations() {
            int bronze = coloBronzeSmallPieces + coloBronzeMedPieces + coloBronzeLargePieces;
            int silver = coloSilverSmallPieces + coloSilverMedPieces + coloSilverLargePieces;
            int gold = coloGoldSmallPieces + coloGoldMedPieces + coloGoldLargePieces;
            return (bronze, silver, gold);
        }
    }

    public class GrubCounts {
        public static readonly List<int> shinyRewards = [5, 10, 16, 23, 31, 38, 46];

        public static readonly List<(int, int, int)> geoSizes = [
            (0, 0, 0),//0: (index offset)
            (5, 1, 0),
            (5, 3, 0),
            (5, 5, 0),
            (5, 2, 1),
            (0, 0, 0),// 5: mask shard
            (5, 4, 1),
            (10, 5, 1),
            (5, 3, 2),
            (5, 5, 2),
            (0, 0, 0),// 10: grubsong
            (10, 6, 2),
            (15, 7, 2),
            (15, 9, 2),
            (10, 7, 3),
            (10, 7, 3),
            (0, 0, 0),// 16: rancid egg
            (15, 10, 3),
            (15, 12, 3),
            (15, 9, 4),
            (15, 10, 4),
            (15, 11, 4),
            (15, 13, 4),
            (0, 0, 0),// 23: hallownest seal
            (15, 12, 5),
            (15, 12, 5),
            (15, 13, 5),
            (15, 14, 5),
            (15, 10, 6),
            (15, 11, 6),
            (15, 11, 6),
            (0, 0, 0),// 31: pale ore
            (15, 13, 6),
            (15, 14, 6),
            (15, 10, 7),
            (15, 11, 7),
            (15, 12, 7),
            (15, 13, 7),
            (0, 0, 0),// 38: king's idol
            (15, 9, 8),
            (15, 10, 8),
            (15, 11, 8),
            (15, 13, 8),
            (15, 10, 9),
            (15, 11, 9),
            (15, 12, 9),
            (0, 0, 0)// 46: elegy
        ];
    }
}

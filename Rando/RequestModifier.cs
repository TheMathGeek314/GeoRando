using ItemChanger;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace GeoRando {
    public class RequestModifier {
        public static void Hook() {
            RequestBuilder.OnUpdate.Subscribe(-100, ApplyGeoPieceDef);
            RequestBuilder.OnUpdate.Subscribe(-499, SetupItems);
            RequestBuilder.OnUpdate.Subscribe(-499.5f, DefinePools);
            RequestBuilder.OnUpdate.Subscribe(1200, RemoveVanillaGeo);
            RequestBuilder.OnUpdate.Subscribe(0, CopyGlobalToLocal);
        }

        private static void AddAndEditLocation(RequestBuilder rb, string name, string scene) {
            rb.AddLocationByName(name);
            rb.EditLocationRequest(name, info => {
                info.customPlacementFetch = (factory, placement) => {
                    if(factory.TryFetchPlacement(name, out AbstractPlacement ap1))
                        return ap1;
                    AbstractLocation absLoc = Finder.GetLocation(name);
                    absLoc.flingType = FlingType.DirectDeposit;
                    AbstractPlacement ap = absLoc.Wrap();
                    factory.AddPlacement(ap);
                    return ap;
                };
                info.getLocationDef = () => new() {
                    Name = name,
                    FlexibleCount = false,
                    AdditionalProgressionPenalty = false,
                    SceneName = scene
                };
            });
        }

        public static void ApplyGeoPieceDef(RequestBuilder rb) {
            foreach((string, string) key in JsonGeoData.geoDataDict.Keys) {
                //string scene = key.Item1;//key.Replace(" - ", "-").Split('-')[0];
                string locationName = JsonGeoData.icNameConversion[key];
                int copies = JsonGeoData.geoDataDict[key].Item2;
                bool isRock = locationName.StartsWith("Geo_Rock_Piece-") && GeoRando.Settings.Rocks;
                bool isChest = locationName.StartsWith("Geo_Chest_Piece-") && GeoRando.Settings.Chests;
                if(isRock || isChest) {
                    for(int i = 1; i <= copies; i++) {
                        AddAndEditLocation(rb, $"{locationName} ({i})", key.Item1);
                    }
                }
            }
        }

        private static void SetupItems(RequestBuilder rb) {
            if(!GeoRando.Settings.Any)
                return;
            foreach(string geoConst in new string[] { Consts.SmallGeo, Consts.MedGeo, Consts.LargeGeo }) {
                rb.EditItemRequest(geoConst, info => {
                    info.getItemDef = () => new ItemDef() {
                        Name = geoConst,
                        Pool = PoolNames.Geo,
                        MajorItem = false,
                        PriceCap = 1
                    };
                });
            }
            (int s, int m, int l) = Quantities.totalGeoPieces();
            rb.AddItemByName(Consts.SmallGeo, s);
            rb.AddItemByName(Consts.MedGeo, m);
            rb.AddItemByName(Consts.LargeGeo, l);
        }

        private static void DefinePools(RequestBuilder rb) {
            GlobalSettings s = GeoRando.Settings;
            if(!s.Any)
                return;
            rb.OnGetGroupFor.Subscribe(0.01f, ResolveGrGroup);
            bool ResolveGrGroup(RequestBuilder rb, string item, RequestBuilder.ElementType type, out GroupBuilder gb) {
                if(type == RequestBuilder.ElementType.Item) {
                    if(item == Consts.SmallGeo || item == Consts.MedGeo || item == Consts.LargeGeo) {
                        gb = rb.GetGroupFor(ItemNames.Geo_Rock_Default);
                        return true;
                    }
                }
                else if(type == RequestBuilder.ElementType.Location) {
                    if(item.StartsWith("Geo_Rock_Piece-") || item.StartsWith("Geo_Chest_Piece-")) {
                        gb = rb.GetGroupFor(ItemNames.Geo_Rock_Default);
                        return true;
                    }
                }
                gb = default;
                return false;
            }
        }

        private static void RemoveVanillaGeo(RequestBuilder rb) {
            if(GeoRando.Settings.Rocks) {
                rb.RemoveItemsWhere(item => item.StartsWith("Geo_Rock-"));
                rb.RemoveLocationsWhere(loc => loc.StartsWith("Geo_Rock-"));
            }
            if(GeoRando.Settings.Chests) {
                rb.RemoveItemsWhere(item => item.StartsWith("Geo_Chest-"));
                rb.RemoveLocationsWhere(loc => loc.StartsWith("Geo_Chest-"));
            }
        }

        private static void CopyGlobalToLocal(RequestBuilder rb) {
            LocalSettings l = GeoRando.localSettings;
            GlobalSettings g = GeoRando.Settings;
            l.Rocks = g.Rocks;
            l.Chests = g.Chests;
            l.Colosseum = g.Colosseum;
            l.GrubFather = g.GrubFather;
        }
    }
}

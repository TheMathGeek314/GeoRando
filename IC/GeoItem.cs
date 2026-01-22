using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;

namespace GeoRando {
    public class GeoItem: AbstractItem {
        protected int geoSize;

        public GeoItem(int size) {
            geoSize = size;
            InteropTag tag = RandoInterop.AddTag(this);
            tag.Properties["PinSprite"] = new EmbeddedSprite("geopin");
            UIDef = new MsgUIDef {
                name = new BoxedString($"{geoSize} Geo"),
                shopDesc = new BoxedString("This seems like a good investment."),
                sprite = new ItemChangerSprite("ShopIcons.Geo")
            };
        }

        public override void GiveImmediate(GiveInfo info) {
            HeroController.instance.AddGeo(geoSize);
        }
    }

    public class SmallGeoItem: GeoItem {
        public SmallGeoItem() : base(1) {
            name = Consts.SmallGeo;
        }
    }

    public class MediumGeoItem: GeoItem {
        public MediumGeoItem() : base(5) {
            name = Consts.MedGeo;
        }
    }

    public class LargeGeoItem: GeoItem {
        public LargeGeoItem() : base(25) {
            name = Consts.LargeGeo;
        }
    }
}

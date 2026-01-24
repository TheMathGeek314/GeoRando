using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;

namespace GeoRando {
    public class ColoGeoItem: AbstractItem {
        protected int geoSize;

        public ColoGeoItem(int size) {
            geoSize = size;
            InteropTag tag = RandoInterop.AddTag(this);
            tag.Properties["PinSprite"] = new EmbeddedSprite("geopin");
            AddTag<PersistentItemTag>().Persistence = Persistence.SemiPersistent;
            UIDef = new MsgUIDef {
                name = new BoxedString($"{geoSize} Geo"),
                shopDesc = new BoxedString("Call me a Fool, but I can't seem to get rid of this geo."),
                sprite = new ItemChangerSprite("ShopIcons.Geo")
            };
        }

        public override void GiveImmediate(GiveInfo info) {
            HeroController.instance.AddGeo(geoSize);
        }
    }

    public class SmallColoGeoItem: ColoGeoItem {
        public SmallColoGeoItem(): base(1) {
            name = Consts.SmallColoGeo;
        }
    }

    public class MediumColoGeoItem: ColoGeoItem {
        public MediumColoGeoItem(): base(5) {
            name = Consts.MedColoGeo;
        }
    }

    public class LargeColoGeoItem: ColoGeoItem {
        public LargeColoGeoItem(): base(25) {
            name = Consts.LargeColoGeo;
        }
    }
}

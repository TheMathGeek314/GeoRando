namespace GeoRando {
    public class GlobalSettings {
        public bool Rocks = false;
        public bool Chests = false;
        public bool Colosseum = false;
        public bool Grubfather = false;

        public bool Any => Rocks || Chests || Colosseum || Grubfather;
    }

    public class LocalSettings {
        public bool Rocks = false;
        public bool Chests = false;
        public bool Colosseum = false;
        public bool Grubfather = false;

        public bool Any => Rocks || Chests || Colosseum || Grubfather;
    }
}

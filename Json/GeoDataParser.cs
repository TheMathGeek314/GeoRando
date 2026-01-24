using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GeoRando {
    public class ParseJson {
        private readonly string _jsonFilePath;
        private readonly Stream _jsonStream;
        private bool isPath;

        public ParseJson(string jsonFilePath) {
            _jsonFilePath = jsonFilePath;
            isPath = true;
        }

        public ParseJson(Stream jsonStream) {
            _jsonStream = jsonStream;
            isPath = false;
        }

        public List<T> parseFile<T>() {
            if(isPath) {
                using StreamReader reader = new(_jsonFilePath);
                var json = reader.ReadToEnd();
                List<T> values = JsonConvert.DeserializeObject<List<T>>(json);
                return values;
            }
            else {
                using StreamReader reader = new(_jsonStream);
                var json = reader.ReadToEnd();
                List<T> values = JsonConvert.DeserializeObject<List<T>>(json);
                return values;
            }
        }
    }

    public class JsonGeoData {
        public static Dictionary<(string, string), (string, int)> geoDataDict = new();
        public static Dictionary<(string, string), string> icNameConversion = new();

        public string scene;
        public string objectName;
        public string icName;
        public int copies;
        public float x;
        public float y;

        public void translate() {
            if(!icName.StartsWith("Room_Colosseum")) {
                geoDataDict.Add((scene, objectName), (icName, copies));
                icNameConversion.Add((scene, objectName), convertLocationName(icName));
            }
        }

        public static string convertLocationName(string vanillaName) {
            return vanillaName.Replace("Geo_Rock-", "Geo_Rock_Piece-").Replace("Geo_Chest-", "Geo_Chest_Piece-");
        }
    }
}

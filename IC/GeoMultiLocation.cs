using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod.Cil;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Internal;
using ItemChanger.Locations;
using uRandom = UnityEngine.Random;

namespace GeoRando {
    public class GeoMultiLocation: AutoLocation {
        private static readonly Dictionary<string, GeoMultiLocation> SubscribedLocations = new();
        public static FieldInfo geoControlSize;

        private static readonly Queue<AbstractPlacement> _giveQueue = new();
        private static bool _queueRunnerActive;

        public static GameObject smallPrefab;
        public static GameObject medPrefab;
        public static GameObject largePrefab;

        public static List<string> availableColoLocations = new();

        public static GiveInfo gi = new() {
            FlingType = FlingType.DirectDeposit,
            Container = Container.Unknown,
            MessageType = MessageType.Corner
        };

        protected override void OnLoad() {
            if(SubscribedLocations.Count == 0)
                HookMultiGeo();
            SubscribedLocations[UnsafeSceneName] = this;
        }

        protected override void OnUnload() {
            SubscribedLocations.Remove(UnsafeSceneName);
            if(SubscribedLocations.Count == 0)
                UnhookMultiGeo();
        }

        private static void HookMultiGeo() {
            On.PlayMakerFSM.OnEnable += editFsm;
            IL.GeoControl.OnTriggerEnter2D += editGeo;
        }

        private static void UnhookMultiGeo() {
            On.PlayMakerFSM.OnEnable -= editFsm;
            IL.GeoControl.OnTriggerEnter2D -= editGeo;
        }

        public static void prepReflection() {
            geoControlSize = typeof(GeoControl).GetField("size", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static void editFsm(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self) {
            orig(self);
            if(self.FsmName == "Geo Rock" && GeoRando.localSettings.Rocks) {
                if(MultiShouldRecolor(self.gameObject.scene.name, recursiveParentSearch(self.gameObject, self.gameObject.name))) {
                    self.gameObject.GetComponent<tk2dSprite>().color = new Color(1, 0.6f, 0.4f, 1);
                }
                FsmState hitState = self.GetState("Hit");
                IntCompare compareAction = hitState.GetFirstActionOfType<IntCompare>();
                compareAction.greaterThan = compareAction.lessThan;
                FlingObjectsFromGlobalPool flingAction = hitState.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
                flingAction.Enabled = false;
                hitState.InsertAction(new FlingAndTag(flingAction, FlingTagType.Rock), 1);

                self.GetState("Destroy").GetFirstActionOfType<FlingObjectsFromGlobalPool>().Enabled = false;

                FsmState initState = self.GetState("Initiate");
                initState.GetFirstActionOfType<IntCompare>().Enabled = false;
                initState.InsertAction(new Lambda(() => {
                    if(MultiAllObtained(self.gameObject.scene.name, recursiveParentSearch(self.gameObject, self.gameObject.name))) {
                        self.SendEvent("BROKEN");
                    }
                }), 1);
            }
            if(self.FsmName == "Chest Control" && GeoRando.localSettings.Chests) {
                if(JsonGeoData.geoDataDict.ContainsKey((self.gameObject.scene.name, recursiveParentSearch(self.gameObject, self.gameObject.name)))) {
                    if(MultiShouldRecolor(self.gameObject.scene.name, recursiveParentSearch(self.gameObject, self.gameObject.name))) {
                        self.gameObject.GetComponent<tk2dSprite>().color = new Color(1, 0.6f, 0.4f, 1);
                    }
                    FsmState initState = self.GetState("Init");
                    initState.GetFirstActionOfType<BoolTest>().Enabled = false;
                    initState.InsertAction(new Lambda(() => {
                        if(MultiAllObtained(self.gameObject.scene.name, recursiveParentSearch(self.gameObject, self.gameObject.name))) {
                            self.SendEvent("ACTIVATE");
                        }
                    }), 6);

                    FsmState spawnState = self.GetState("Spawn Items");
                    if(self.gameObject.name == "Mantis Chest (2)") {
                        spawnState.GetFirstActionOfType<SpawnFromPool>().Enabled = false;
                        foreach(GameObject prefab in new GameObject[] { smallPrefab, medPrefab, largePrefab }) {
                            spawnState.InsertAction(new FlingAndTag(new FlingObjectsFromGlobalPool() {
                                gameObject = new FsmGameObject { Value = prefab },
                                spawnPoint = self.FsmVariables.GetFsmGameObject("Self"),
                                position = new FsmVector3 { Value = Vector3.zero },
                                speedMin = new FsmFloat { Value = 25f },
                                speedMax = new FsmFloat { Value = 38f },
                                angleMin = new FsmFloat { Value = 78f },
                                angleMax = new FsmFloat { Value = 102f },
                                originVariationX = new FsmFloat { Value = 1 },
                                originVariationY = new FsmFloat { Value = 1 },
                                Fsm = self.Fsm
                            }, FlingTagType.Chest), 2);
                        }
                    }
                    else {
                        FlingObjectsFromGlobalPool[] flingActions = spawnState.GetActionsOfType<FlingObjectsFromGlobalPool>();
                        foreach(FlingObjectsFromGlobalPool fAction in flingActions) {
                            spawnState.AddLastAction(new FlingAndTag(fAction, FlingTagType.Chest));
                            fAction.Enabled = false;
                        }
                    }
                }
            }
            if(GeoRando.localSettings.Colosseum && self.gameObject.name.StartsWith("Colosseum Manager") && self.FsmName == "Geo Pool") {
                FsmState initState = self.GetState("Init");
                initState.ClearTransitions();
                initState.AddTransition("FINISHED", "At min");
                availableColoLocations = findAvailableColoLocations(self.gameObject.scene.name);
                initState.AddLastAction(new Lambda(() => {
                    self.FsmVariables.GetFsmInt("Starting Pool").Value = availableColoLocations.Count;
                }));

                FsmState spawnState = self.GetState("Spawn");
                spawnState.GetFirstActionOfType<IntOperator>().integer2 = new FsmInt { Value = 1 };
                spawnState.InsertAction(new Lambda(() => {
                    int counter = self.FsmVariables.GetFsmInt("Starting Pool").Value;
                    string locationName = availableColoLocations[counter - 1];
                    AbstractPlacement ap = Ref.Settings.Placements[locationName];
                    self.FsmVariables.GetFsmGameObject("Geo Object").Value.GetOrAddComponent<GeoPieceComponent>().assignPlacement(ap);
                }), 2);
            }
            if(GeoRando.localSettings.Grubfather) {
                if(self.FsmName == "grub_reward_geo") {
                    int threshold = int.Parse(self.gameObject.name.Substring(7));
                    if(RandomizerMod.RandomizerMod.RS.GenerationSettings.CostSettings.MaximumGrubCost >= threshold) {
                        self.GetState("Init").AddLastAction(new Lambda(() => {
                            (int s, int m, int l) = GrubCounts.geoSizes[threshold];
                            self.FsmVariables.GetFsmInt("Geo").Value = s + m + l;
                        }));
                        foreach(string stateName in new string[] { "Small", "Med", "Large" }) {
                            FsmState varState = self.GetState(stateName);
                            varState.GetFirstActionOfType<IntAdd>().add.Value = -1;
                            if(stateName != "Small") {
                                varState.GetFirstActionOfType<IntCompare>().Enabled = false;
                            }
                            FlingObjectsFromGlobalPool flingAction = varState.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
                            varState.AddLastAction(new FlingAndTag(flingAction, FlingTagType.Grubs));
                            flingAction.Enabled = false;
                        }
                    }
                }
                else if(self.gameObject.name == "Grub King" && self.FsmName == "King Control") {
                    FsmState initState = self.GetState("Init Check");
                    FlingObjectsFromGlobalPool flingTemplate = new() {
                        gameObject = new FsmGameObject { Value = smallPrefab },
                        spawnPoint = new FsmGameObject { Value = GameObject.Find("Reward 1") },
                        position = new FsmVector3 { Value = Vector3.zero },
                        speedMin = new FsmFloat { Value = 20f },
                        speedMax = new FsmFloat { Value = 25f },
                        angleMin = new FsmFloat { Value = 80f },
                        angleMax = new FsmFloat { Value = 100f },
                        originVariationX = new FsmFloat { Value = 0f },
                        originVariationY = new FsmFloat { Value = 0f }
                    };
                    foreach((string toState, string fromEvent, string newStateName, string delayName) in new (string, string, string, string)[] {
                        ("Beckon", "FINISHED", "Persistent Flings B", "Persistent Delay B"),
                        ("All Given", "ALL GIVEN", "Persistent Flings A", "Persistent Delay A")
                    }) {
                        FsmState pFlingState = self.AddState(newStateName);
                        FsmState pWaitState = self.AddState(delayName);
                        initState.RemoveTransitionsTo(toState);
                        initState.AddTransition(fromEvent, delayName);
                        pWaitState.AddTransition("FINISHED", newStateName);
                        pFlingState.AddTransition("FINISHED", toState);

                        pWaitState.AddFirstAction(new Wait() {
                            time = new FsmFloat { Value = 0.5f },
                            finishEvent = new FsmEvent("FINISHED"),
                            realTime = true
                        });

                        pFlingState.AddFirstAction(new FlingAndTag(flingTemplate, FlingTagType.Grubs, true));
                    }
                }
            }
        }

        private static void editGeo(ILContext il) {
            ILCursor cursor = new ILCursor(il).Goto(0);
            cursor.GotoNext(i => i.MatchLdfld<GeoControl>("hero"));
            cursor.RemoveRange(5);
            cursor.EmitDelegate<Action<GeoControl>>(j => {
                if(j.gameObject.TryGetComponent(out GeoPieceComponent gpc) && gpc.isRandod) {
                    EnqueueGive(gpc.placement);
                }
                else {
                    HeroController.instance.AddGeo(((GeoControl.Size)geoControlSize.GetValue(j)).value);
                }
            });
        }

        public static string recursiveParentSearch(GameObject gameObject, string name) {
            Transform parent = gameObject.transform.parent;
            if(parent == null)
                return name;
            return recursiveParentSearch(parent.gameObject, parent.gameObject.name + "/" + name);
        }

        private static bool MultiAllObtained(string scene, string objectName) {
            (string, int) tempTuple = JsonGeoData.geoDataDict[(scene, objectName)];
            string icName = JsonGeoData.convertLocationName(tempTuple.Item1);
            int copies = tempTuple.Item2;
            for(int i = 1; i <= copies; i++) {
                if(!Ref.Settings.Placements[$"{icName} ({i})"].AllObtained())
                    return false;
            }
            return true;
        }

        private static bool MultiShouldRecolor(string scene, string objectName) {
            (string, int) tempTuple = JsonGeoData.geoDataDict[(scene, objectName)];
            string icName = JsonGeoData.convertLocationName(tempTuple.Item1);
            int copies = tempTuple.Item2;
            bool anyWereObtained = false;
            for(int i = 1; i <= copies; i++) {
                AbstractPlacement ap = Ref.Settings.Placements[$"{icName} ({i})"];
                if(ap.Items.Any(item => !item.WasEverObtained())) {
                    return false;
                }
                if(ap.Items.Any(item => item.WasEverObtained())) {
                    anyWereObtained = true;
                }
            }
            return anyWereObtained;
        }

        private static List<string> findAvailableColoLocations(string scene) {
            int small, medium, large;
            string room = "";
            switch(scene) {
                case "Room_Colosseum_Bronze":
                    small = Quantities.coloBronzeSmallPieces;
                    medium = Quantities.coloBronzeMedPieces;
                    large = Quantities.coloBronzeLargePieces;
                    room = "Bronze";
                    break;
                case "Room_Colosseum_Silver":
                    small = Quantities.coloSilverSmallPieces;
                    medium = Quantities.coloSilverMedPieces;
                    large = Quantities.coloSilverLargePieces;
                    room = "Silver";
                    break;
                case "Room_Colosseum_Gold":
                    small = Quantities.coloGoldSmallPieces;
                    medium = Quantities.coloGoldMedPieces;
                    large = Quantities.coloGoldLargePieces;
                    room = "Gold";
                    break;
                default:
                    small = medium = large = 0;
                    break;
            }
            List<string> aLocs = new();
            int j = 0;
            foreach(int count in new int[] { small, medium, large }) {
                for(int i = 1; i <= count; i++) {
                    j++;
                    string locName = $"Colo_Geo_Piece-{room} ({j})";
                    if(!Ref.Settings.Placements[locName].AllObtained()) {
                        aLocs.Add(locName);
                    }
                }
            }
            return aLocs;
        }

        public static List<string> findPersistentGrubLocations() {
            List<string> aLocs = new();
            int cap = RandomizerMod.RandomizerMod.RS.GenerationSettings.CostSettings.MaximumGrubCost;
            for(int i = 1; i <= cap; i++) {
                if(GrubCounts.shinyRewards.Contains(i)) {
                    continue;
                }
                (int small, int med, int large) = GrubCounts.geoSizes[i];
                for(int j = 1; j <= small + med + large; j++) {
                    string locationName = $"Geo_Piece_Grubfather-Reward {i} ({j})";
                    if(Ref.Settings.Placements.TryGetValue(locationName, out AbstractPlacement ap)) {
                        if(ap.Items.Any(item => item.WasEverObtained() && !item.IsObtained())) {
                            aLocs.Add(locationName);
                        }
                    }
                    else {
                        break;
                    }
                }
            }
            return aLocs;
        }

        private static void EnqueueGive(AbstractPlacement ap) {
            _giveQueue.Enqueue(ap);
            if(!_queueRunnerActive) {
                _queueRunnerActive = true;
                GameManager.instance.StartCoroutine(GiveQueueRunner());
            }
        }

        private static IEnumerator GiveQueueRunner() {
            while(_giveQueue.Count > 0) {
                AbstractPlacement ap = _giveQueue.Dequeue();
                ap.GiveAll(gi);
                yield return null;
            }
            _queueRunnerActive = false;
        }
    }

    public enum FlingTagType {
        Rock,
        Chest,
        Grubs
    }

    public class FlingAndTag(): RigidBody2dActionBase {
        private FsmGameObject gameObject, spawnPoint;
        private FsmVector3 position;
        private FsmFloat speedMin, speedMax, angleMin, angleMax, originVariationX, originVariationY;
        private float vectorX, vectorY;
        private bool originAdjusted;
        private FlingTagType tagType;
        private List<int> availableLocations;
        private string locationName;
        private int totalCount;
        private List<string> locationOverride;

        public FlingAndTag(FlingObjectsFromGlobalPool flingAction, FlingTagType type, bool checkPersistentGrubRewards = false): this() {
            if(checkPersistentGrubRewards) {
                locationOverride = GeoMultiLocation.findPersistentGrubLocations();
            }
            if(!checkPersistentGrubRewards) {
                GameObject fgo = flingAction.Fsm.GameObject;
                (string, string) dictKey = (fgo.scene.name, GeoMultiLocation.recursiveParentSearch(fgo, fgo.name));

                if(JsonGeoData.geoDataDict.TryGetValue(dictKey, out (string, int) nameCount)) {
                    locationName = JsonGeoData.convertLocationName(nameCount.Item1);
                    totalCount = nameCount.Item2;
                }
                else {
                    return;
                }
            }

            gameObject = flingAction.gameObject;
            spawnPoint = flingAction.spawnPoint;
            position = flingAction.position;
            speedMin = flingAction.speedMin;
            speedMax = flingAction.speedMax;
            angleMin = flingAction.angleMin;
            angleMax = flingAction.angleMax;
            originVariationX = flingAction.originVariationX;
            originVariationY = flingAction.originVariationY;
            tagType = type;
        }

        public override void OnEnter() {
            FsmVariables fv = Fsm.FsmComponent.FsmVariables;
            int geoSmall = fv.GetFsmInt("Geo Small").Value;
            int geoMed = fv.GetFsmInt("Geo Med").Value;
            int geoLarge = fv.GetFsmInt("Geo Large").Value;
            int aCount = 0;
            if(locationOverride == null) {
                availableLocations = findAvailableLocations(locationName, totalCount);
                aCount = availableLocations.Count;
            }

            if(gameObject.Value) {
                Vector3 a = Vector3.zero;
                if(spawnPoint.Value) {
                    a = spawnPoint.Value.transform.position + (position.IsNone ? Vector3.zero : position.Value);
                }
                else if(!position.IsNone) {
                    a = position.Value;
                }

                int num = tagType switch {
                    FlingTagType.Rock => availableLocations.Count,
                    FlingTagType.Chest => gameObject.Value.name.Substring(0, 5) switch {
                        "Geo S" => Mathf.Min(aCount, geoSmall),
                        "Geo M" => Mathf.Min(aCount - geoSmall, geoMed),
                        "Geo L" => Mathf.Min(aCount - geoSmall - geoMed, geoLarge),
                        _ => aCount
                    },
                    FlingTagType.Grubs => locationOverride == null ? 1 : locationOverride.Count,
                    _ => aCount
                };

                for(int i = 0; i < num; i++) {
                    GameObject go = gameObject.Value.Spawn(a, Quaternion.Euler(Vector3.zero));
                    float x = go.transform.position.x;
                    float y = go.transform.position.y;
                    if(originVariationX != null) {
                        x += uRandom.Range(-originVariationX.Value, originVariationX.Value);
                        originAdjusted = true;
                    }
                    if(originVariationY != null) {
                        y += uRandom.Range(-originVariationY.Value, originVariationY.Value);
                        originAdjusted = true;
                    }
                    if(originAdjusted)
                        go.transform.position = new Vector3(x, y, go.transform.position.z);
                    base.CacheRigidBody2d(go);
                    float num2 = uRandom.Range(speedMin.Value, speedMax.Value);
                    float num3 = uRandom.Range(angleMin.Value, angleMax.Value);
                    vectorX = num2 * Mathf.Cos(num3 * 0.017453292f);
                    vectorY = num2 * Mathf.Sin(num3 * 0.017453292f);
                    Vector2 velocity = new(vectorX, vectorY);
                    rb2d.velocity = velocity;

                    int numberId = 0;
                    switch(tagType) {
                        case FlingTagType.Rock:
                            numberId = availableLocations[i];
                            break;
                        case FlingTagType.Chest:
                            if(gameObject.Value.name.StartsWith("Geo Small")) {
                                numberId = availableLocations[i];
                            }
                            else if(gameObject.Value.name.StartsWith("Geo Med")) {
                                numberId = availableLocations[i + geoSmall];
                            }
                            else if(gameObject.Value.name.StartsWith("Geo Large")) {
                                numberId = availableLocations[i + geoSmall + geoMed];
                            }
                            break;
                        case FlingTagType.Grubs:
                            if(locationOverride == null)
                                numberId = availableLocations[Fsm.FsmComponent.FsmVariables.GetFsmInt("Geo").Value];
                            break;
                        default:
                            return;
                    }

                    string locationIdName;
                    if(locationOverride == null) {
                        GameObject source = Fsm.GameObject;
                        locationIdName = string.Concat(JsonGeoData.icNameConversion[(source.scene.name, GeoMultiLocation.recursiveParentSearch(source, source.name))], " (", numberId, ")");
                    }
                    else {
                        locationIdName = locationOverride[i];
                    }
                    if(Ref.Settings.Placements.TryGetValue(locationIdName, out AbstractPlacement ap)) {
                        go.GetOrAddComponent<GeoPieceComponent>().assignPlacement(ap);
                    }
                }
            }
            Finish();
        }

        private static List<int> findAvailableLocations(string name, int count) {
            List<int> aLocs = new();
            for(int i = 1; i <= count; i++) {
                string locName = $"{name} ({i})";
                if(!Ref.Settings.Placements[locName].AllObtained()) {
                    aLocs.Add(i);
                }
            }
            return aLocs;
        }
    }

    public class GeoPieceComponent: MonoBehaviour {
        public AbstractPlacement placement;
        public bool isRandod;

        public void assignPlacement(AbstractPlacement ap) {
            placement = ap;
            isRandod = true;
        }

        void OnDisable() {
            isRandod = false;
        }
    }
}

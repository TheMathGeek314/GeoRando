using System;
using System.Collections;
using System.Collections.Generic;
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
            On.GeoControl.Disable += editDisableGeo;
        }

        private static void UnhookMultiGeo() {
            On.PlayMakerFSM.OnEnable -= editFsm;
            IL.GeoControl.OnTriggerEnter2D -= editGeo;
            On.GeoControl.Disable -= editDisableGeo;
        }

        public static void prepReflection() {
            geoControlSize = typeof(GeoControl).GetField("size", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static void editFsm(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self) {
            orig(self);
            if(self.FsmName == "Geo Rock" && GeoRando.localSettings.Rocks) {
                FsmState hitState = self.GetState("Hit");
                IntCompare compareAction = hitState.GetFirstActionOfType<IntCompare>();
                compareAction.greaterThan = compareAction.lessThan;
                FlingObjectsFromGlobalPool flingAction = hitState.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
                flingAction.Enabled = false;
                hitState.InsertAction(new FlingAndTag(flingAction, FlingTagType.GeoRock), 1);

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
                FsmState spawnState = self.GetState("Spawn Items");
                FlingObjectsFromGlobalPool[] flingActions = spawnState.GetActionsOfType<FlingObjectsFromGlobalPool>();
                foreach(FlingObjectsFromGlobalPool fAction in flingActions) {
                    spawnState.AddLastAction(new FlingAndTag(fAction, FlingTagType.Chest));
                    fAction.Enabled = false;
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

        private static void editDisableGeo(On.GeoControl.orig_Disable orig, GeoControl self, float waitTime) {
            orig(self, waitTime);
            if(self.gameObject.TryGetComponent(out GeoPieceComponent gpc)) {
                gpc.wipe();
            }
        }

        public static string recursiveParentSearch(GameObject gameObject, string name) {
            Transform parent = gameObject.transform.parent;
            if(parent == null)
                return name;
            return recursiveParentSearch(parent.gameObject, parent.gameObject.name + "/" + name);
        }

        private static bool MultiAllObtained(string scene, string objectName) {
            (string icName, int copies) = JsonGeoData.geoDataDict[(scene, objectName)];
            for(int i = 1; i <= copies; i++) {
                if(!Ref.Settings.Placements[$"{icName} ({i})"].AllObtained())
                    return false;
            }
            return true;
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
        GeoRock,
        Chest
    }

    public class FlingAndTag(): RigidBody2dActionBase {
        private FsmGameObject gameObject, spawnPoint;
        private FsmVector3 position;
        private FsmInt spawnMin, spawnMax;
        private FsmFloat speedMin, speedMax, angleMin, angleMax, originVariationX, originVariationY;
        private float vectorX, vectorY;
        private bool originAdjusted;
        private FlingTagType tagType;
        private List<int> availableLocations;
        private string locationName;
        private int totalCount;

        public FlingAndTag(FlingObjectsFromGlobalPool flingAction, FlingTagType type): this() {
            FsmVariables fv = flingAction.Fsm.FsmComponent.FsmVariables;
            (string, int) nameCount = JsonGeoData.geoDataDict[(flingAction.Fsm.GameObject.scene.name, GeoMultiLocation.recursiveParentSearch(flingAction.Fsm.GameObject, flingAction.Fsm.GameObjectName))];
            locationName = JsonGeoData.convertLocationName(nameCount.Item1);
            totalCount = nameCount.Item2;
            availableLocations = findAvailableLocations(locationName, totalCount);

            gameObject = flingAction.gameObject;
            spawnPoint = flingAction.spawnPoint;
            position = flingAction.position;
            spawnMin = availableLocations.Count;
            spawnMax = availableLocations.Count;
            speedMin = flingAction.speedMin;
            speedMax = flingAction.speedMax;
            angleMin = flingAction.angleMin;
            angleMax = flingAction.angleMax;
            originVariationX = flingAction.originVariationX;
            originVariationY = flingAction.originVariationY;
            tagType = type;
        }

        public override void OnEnter() {
            if(gameObject.Value) {
                Vector3 a = Vector3.zero;
                if(spawnPoint.Value) {
                    a = spawnPoint.Value.transform.position + (position.IsNone ? Vector3.zero : position.Value);
                }
                else if(!position.IsNone) {
                    a = position.Value;
                }
                int num = uRandom.Range(spawnMin.Value, spawnMax.Value + 1);
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

                    FsmVariables fv = Fsm.FsmComponent.FsmVariables;
                    int numberId = 0;
                    switch(tagType) {
                        case FlingTagType.GeoRock:
                            numberId = availableLocations[i];
                            break;
                        case FlingTagType.Chest:
                            if(gameObject.Value.name.StartsWith("Geo Small")) {
                                numberId = i + 1;
                            }
                            else if(gameObject.Value.name.StartsWith("Geo Med")) {
                                numberId = i + fv.GetFsmInt("Geo Small").Value + 1;
                            }
                            else if(gameObject.Value.name.StartsWith("Geo Large")) {
                                numberId = i + fv.GetFsmInt("Geo Small").Value + fv.GetFsmInt("Geo Med").Value + 1;
                            }
                            break;
                        default:
                            return;
                    }
                    GameObject source = Fsm.GameObject;
                    string locationIdName = string.Concat(JsonGeoData.icNameConversion[(source.scene.name, GeoMultiLocation.recursiveParentSearch(source, source.name))], " (", numberId, ")");
                    if(Ref.Settings.Placements.TryGetValue(locationIdName, out AbstractPlacement ap)) {
                        go.GetOrAddComponent<GeoPieceComponent>().assignPlacement(ap);
                    }
                }
            }
            Finish();
        }

        private List<int> findAvailableLocations(string name, int count) {
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

        public void wipe() {
            isRandod = false;
        }
    }
}

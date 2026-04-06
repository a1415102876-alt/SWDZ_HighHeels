using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Character;
using SwdzHighheels;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SwdzHighheels
{
    [BepInPlugin("com.user.swdzhighheels", "Swdz Highheels", "0.1.0")]
    public class HeelzPlugin : BasePlugin
    {
        public static HeelzPlugin Instance;
        private static GameObject _poseRunnerGO;
        private static Type _cachedHSceneType;
        private static Type _cachedHSceneActorTypeEnum;
        private static bool _hSceneTypeResolved;

        public ConfigEntry<bool> CfgEnabled;
        public ConfigEntry<bool> CfgEditMode;
        public ConfigEntry<string> CfgManualGamePath;
        public ConfigEntry<bool> CfgEnableStandaloneUI;
        public ConfigEntry<KeyCode> CfgUIKey;
        public ConfigEntry<float> CfgUIScale;
        public ConfigEntry<float> CfgHeight;
        public ConfigEntry<float> CfgAngle;
        public ConfigEntry<float> CfgToeAngle;
        public ConfigEntry<bool> CfgShowPlane;
        public ConfigEntry<bool> CfgFootPlaneFollowFeet;
        public ConfigEntry<float> CfgMeasureOffset;
        public ConfigEntry<bool> CfgEnablePoseAdjust;
        public ConfigEntry<float> CfgHipOffset;
        public ConfigEntry<float> CfgThighAngle;
        public ConfigEntry<float> CfgKneeAngle;
        public ConfigEntry<bool> CfgSaveButton;
        public ConfigEntry<string> CfgStatus;
        public ConfigEntry<Language> CfgLanguage;

        public static string DebugShoeName = "None";
        public static string DebugHeightInfo = "0.00";
        public static string FocusCharName = "";
        public static List<HeelConfigData> ConfigList = new List<HeelConfigData>();
        public static string DebugPoseInfo = "";
        public static string DebugAnimInfo = "";
        public static string DebugShoePresetInfo = "None";
        public static string DebugPosePresetInfo = "None";

        private PropertyInfo _propCoorde;
        private string _cachedConfigDir;
        private readonly Dictionary<string, PosePresetData> _posePresets = new Dictionary<string, PosePresetData>(StringComparer.OrdinalIgnoreCase);

        public string ConfigDir
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedConfigDir))
                {
                    string localPath = Path.Combine(Paths.PluginPath, "SwdzHighheels", "config");
                    if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
                    _cachedConfigDir = localPath;
                }
                return _cachedConfigDir;
            }
        }

        public string AnimationConfigDir
        {
            get
            {
                string dir = Path.Combine(ConfigDir, "animation");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private string GetConfigCategoryName(string category)
        {
            return Localization.CurrentLanguage == Language.Chinese
                ? category.Replace("1. System", Localization.Get("Config_System"))
                          .Replace("3. Body", Localization.Get("Config_Body"))
                          .Replace("4. Tools", Localization.Get("Config_Tools"))
                          .Replace("5. Info", Localization.Get("Config_Info"))
                : category;
        }

        private ConfigDescription GetConfigDescription(string key, string description = null)
        {
            string dispName = Localization.Get($"Config_{key}");
            string desc = description ?? "";
            return new ConfigDescription(desc, null, new ConfigurationManagerAttributes { DispName = dispName });
        }

        public override void Load()
        {
            Instance = this;
            ClassInjector.RegisterTypeInIl2Cpp<HeelsController>();
            ClassInjector.RegisterTypeInIl2Cpp<HeelzGui>();
            ClassInjector.RegisterTypeInIl2Cpp<PoseLateApplyRunner>();

            CfgLanguage = Config.Bind("1. System", "Language", Language.English,
                new ConfigDescription("Language: English (0) or Chinese (1)", null,
                    new ConfigurationManagerAttributes { DispName = "Language" }));

            Localization.CurrentLanguage = CfgLanguage.Value;
            CfgLanguage.SettingChanged += (sender, args) =>
            {
                Localization.CurrentLanguage = CfgLanguage.Value;
                UpdateAllConfigDisplayNames();
            };

            CfgEnabled = Config.Bind("1. System", "Enable", true, GetConfigDescription("Enable"));
            CfgEditMode = Config.Bind("1. System", "Edit Mode", false, GetConfigDescription("EditMode"));
            CfgManualGamePath = Config.Bind("1. System", "Game Path", "", GetConfigDescription("GamePath"));
            CfgEnableStandaloneUI = Config.Bind("1. System", "Enable GUI", true, GetConfigDescription("EnableGUI"));
            CfgUIKey = Config.Bind("1. System", "GUI Key", KeyCode.H, GetConfigDescription("GUIKey"));
            CfgUIScale = Config.Bind("1. System", "UI Scale", 1.0f, GetConfigDescription("UIScale"));
            CfgHeight = Config.Bind("3. Body", "Height", 0.05f, GetConfigDescription("Height"));
            CfgAngle = Config.Bind("3. Body", "Ankle", 25f, GetConfigDescription("Ankle"));
            CfgToeAngle = Config.Bind("3. Body", "Toe", 15f, GetConfigDescription("Toe"));
            CfgEnablePoseAdjust = Config.Bind("3. Body", "Enable Pose Adjust", true, GetConfigDescription("EnablePoseAdjust"));
            CfgHipOffset = Config.Bind("3. Body", "Pose Hip Offset", 0f, GetConfigDescription("PoseHipOffset"));
            CfgThighAngle = Config.Bind("3. Body", "Pose Thigh Angle", 0f, GetConfigDescription("PoseThighAngle"));
            CfgKneeAngle = Config.Bind("3. Body", "Pose Knee Angle", 0f, GetConfigDescription("PoseKneeAngle"));
            CfgShowPlane = Config.Bind("4. Tools", "Show Plane", false, GetConfigDescription("ShowPlane"));
            CfgFootPlaneFollowFeet = Config.Bind("4. Tools", "Foot Plane At Feet", true,
                GetConfigDescription("FootPlaneFollowFeet", "Align foot reference plane to feet (world); off = root local (0,0,0) like legacy."));
            CfgMeasureOffset = Config.Bind("4. Tools", "Head Offset", 0.18f, GetConfigDescription("HeadOffset"));
            CfgSaveButton = Config.Bind("4. Tools", "UI Panel", false, new ConfigDescription("UI", null, new ConfigurationManagerAttributes { HideDefaultButton = true, CustomDrawer = DrawF1UI, DispName = Localization.Get("Config_UIPanel") }));
            CfgStatus = Config.Bind("5. Info", "Status", "...", GetConfigDescription("Status"));

            LoadAllConfigs();
            LoadAllPoseConfigs();
            Harmony.CreateAndPatchAll(typeof(Hooks));

            if (HeelzGui.Instance == null)
            {
                GameObject guiGO = new GameObject("HeelzGui_Manager");
                GameObject.DontDestroyOnLoad(guiGO);
                guiGO.AddComponent<HeelzGui>();
            }

            EnsurePoseLateRunner();

            Log.LogInfo("Swdz Highheels 0.1.0 Loaded.");
        }

        public void Update()
        {
            if (Time.frameCount % 10 == 0)
            {
                float minDist = 999f;
                string closest = "";
                int count = 0;
                string firstChar = "";

                foreach (var ctrl in HeelsController.ActiveInstances)
                {
                    if (ctrl != null && ctrl.transform != null && !string.IsNullOrEmpty(ctrl.CharacterName))
                    {
                        count++;
                        if (string.IsNullOrEmpty(firstChar)) firstChar = ctrl.CharacterName;

                        if (Camera.main != null)
                        {
                            float dist = Vector3.Distance(Camera.main.transform.position, ctrl.transform.position);
                            if (dist < minDist) { minDist = dist; closest = ctrl.CharacterName; }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(closest)) closest = ctrl.CharacterName;
                        }
                    }
                }

                if (string.IsNullOrEmpty(closest) && count > 0)
                {
                    closest = firstChar;
                }

                if (string.IsNullOrEmpty(closest))
                {
                    foreach (var ctrl in HeelsController.ActiveInstances)
                    {
                        if (ctrl != null && ctrl.transform != null && !string.IsNullOrEmpty(ctrl.CharacterName))
                        {
                            if (ctrl.CurrentShoeID != -1 || !string.IsNullOrEmpty(ctrl.CurrentShoeName))
                            {
                                closest = ctrl.CharacterName;
                                break;
                            }
                        }
                    }
                }

                FocusCharName = closest ?? "";
            }
        }

        private void EnsurePoseLateRunner()
        {
            if (_poseRunnerGO != null) return;
            _poseRunnerGO = new GameObject("Heelz_PoseLateRunner");
            GameObject.DontDestroyOnLoad(_poseRunnerGO);
            _poseRunnerGO.AddComponent<PoseLateApplyRunner>();
        }

        public void DrawF1UI(ConfigEntryBase entry) { HeelzGui.DrawContent(false); }

        private void UpdateAllConfigDisplayNames()
        {
            try
            {
                UpdateConfigDisplayName(CfgEnabled, "Enable");
                UpdateConfigDisplayName(CfgEditMode, "EditMode");
                UpdateConfigDisplayName(CfgManualGamePath, "GamePath");
                UpdateConfigDisplayName(CfgEnableStandaloneUI, "EnableGUI");
                UpdateConfigDisplayName(CfgUIKey, "GUIKey");
                UpdateConfigDisplayName(CfgUIScale, "UIScale");
                UpdateConfigDisplayName(CfgHeight, "Height");
                UpdateConfigDisplayName(CfgAngle, "Ankle");
                UpdateConfigDisplayName(CfgToeAngle, "Toe");
                UpdateConfigDisplayName(CfgEnablePoseAdjust, "EnablePoseAdjust");
                UpdateConfigDisplayName(CfgHipOffset, "PoseHipOffset");
                UpdateConfigDisplayName(CfgThighAngle, "PoseThighAngle");
                UpdateConfigDisplayName(CfgKneeAngle, "PoseKneeAngle");
                UpdateConfigDisplayName(CfgShowPlane, "ShowPlane");
                UpdateConfigDisplayName(CfgFootPlaneFollowFeet, "FootPlaneFollowFeet");
                UpdateConfigDisplayName(CfgMeasureOffset, "HeadOffset");
                UpdateConfigDisplayName(CfgStatus, "Status");
                UpdateConfigDisplayName(CfgLanguage, "Language");
            }
            catch { }
        }

        private void UpdateConfigDisplayName(ConfigEntryBase entry, string key)
        {
            try
            {
                if (entry == null) return;
                var descProp = entry.GetType().GetProperty("Description");
                if (descProp == null) return;
                var desc = descProp.GetValue(entry) as ConfigDescription;
                if (desc == null) return;
                var tagsProp = desc.GetType().GetProperty("Tags");
                if (tagsProp == null) return;
                var tags = tagsProp.GetValue(desc);
                if (tags == null) return;

                ConfigurationManagerAttributes attrs = tags as ConfigurationManagerAttributes;
                if (attrs == null)
                {
                    var tagsArray = tags as object[];
                    if (tagsArray != null)
                    {
                        foreach (var tag in tagsArray)
                        {
                            if (tag is ConfigurationManagerAttributes)
                            {
                                attrs = tag as ConfigurationManagerAttributes;
                                break;
                            }
                        }
                    }
                }

                if (attrs != null)
                {
                    attrs.DispName = Localization.Get($"Config_{key}");
                }
            }
            catch { }
        }

        private static void ResolveHSceneTypes()
        {
            if (_hSceneTypeResolved) return;
            _hSceneTypeResolved = true;

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    if (_cachedHSceneType == null)
                    {
                        _cachedHSceneType = asm.GetType("H.HScene", false) ?? asm.GetType("SV.H.HScene", false);
                    }
                    if (_cachedHSceneActorTypeEnum == null)
                    {
                        _cachedHSceneActorTypeEnum = asm.GetType("H.HScene+ActorType", false) ?? asm.GetType("SV.H.HScene+ActorType", false);
                    }
                    if (_cachedHSceneType != null && _cachedHSceneActorTypeEnum != null) break;
                }
            }
            catch { }
        }

        private static object FindHSceneInstance()
        {
            ResolveHSceneTypes();
            if (_cachedHSceneType == null) return null;
            try
            {
                var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allBehaviours)
                {
                    if (mb == null) continue;
                    var t = mb.GetType();
                    if (t == null) continue;
                    if (_cachedHSceneType.IsAssignableFrom(t) || t.FullName == _cachedHSceneType.FullName)
                        return mb;
                }
            }
            catch { }
            return null;
        }

        private int GetShoeID(Character.Human human) { if (human == null) return -1; try { if (_propCoorde == null) _propCoorde = human.GetType().GetProperty("coorde"); var coordObj = _propCoorde?.GetValue(human) as HumanCoordinate; if (coordObj != null && coordObj.Now != null && coordObj.Now.Clothes != null) { var parts = coordObj.Now.Clothes.parts; int id = -1; if (parts.Length > 7) id = parts[7].id; if ((id == 0 || id == -1) && parts.Length > 8) id = parts[8].id; return id; } } catch { } return -1; }
        private SkinnedMeshRenderer GetShoeRenderer(GameObject go)
        {
            if (go == null) return null;
            try
            {
                var meshes = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                SkinnedMeshRenderer bestMatch = null;
                int highestPriority = -1;

                Transform footL = null, footR = null;
                try
                {
                    footL = RecursiveFindTransform(go.transform, "cf_j_foot_L");
                    footR = RecursiveFindTransform(go.transform, "cf_j_foot_R");
                }
                catch { }

                bool hasFootBones = (footL != null || footR != null);

                foreach (var m in meshes)
                {
                    if (m == null || m.sharedMesh == null || !m.enabled || !m.gameObject.activeInHierarchy) continue;
                    string n = m.name.ToLower();

                    if ((n.Contains("nail") && !n.Contains("shoe")) ||
                        (n.Contains("finger") && !n.Contains("shoe")) ||
                        n.Contains("hand") || n.Contains("arm") ||
                        n.Contains("head") || n.Contains("hair") ||
                        n.Contains("face") || n.Contains("eye") || n.Contains("mouth") ||
                        n.Contains("nose") || n.Contains("ear") || n.Contains("cheek") ||
                        (n.Contains("body") && !n.Contains("foot")) ||
                        (n.Contains("skin") && !n.Contains("shoe") && !n.Contains("foot")) ||
                        (n.Contains("cloth") && !n.Contains("shoe") && !n.Contains("foot")) ||
                        n.Contains("dress") || n.Contains("shirt") ||
                        (n.Contains("pant") && !n.Contains("foot")) ||
                        n.Contains("skirt") || n.Contains("bra") || n.Contains("underwear") ||
                        n.StartsWith("cf_o_face") || n.StartsWith("cf_n_face") ||
                        n.Contains("_face_") || n.Contains("face_"))
                    {
                        continue;
                    }

                    int priority = 0;
                    if (n.Contains("heel")) priority = 5;
                    else if (n.Contains("boot")) priority = 4;
                    else if (n.Contains("shoe")) priority = 3;
                    else if (n.Contains("foot")) priority = 2;
                    else if (n.Contains("leg") && !n.Contains("thigh")) priority = 1;
                    else if (n.Contains("o_shoes") || n.Contains("shoes_")) priority = 3;

                    bool isFootLevel = false;
                    if (hasFootBones)
                    {
                        Transform footBone = footL != null ? footL : footR;
                        float distToFoot = Vector3.Distance(m.transform.position, footBone.position);
                        isFootLevel = distToFoot < 0.25f;

                        if (footL != null && footR != null)
                        {
                            float distToFootL = Vector3.Distance(m.transform.position, footL.position);
                            float distToFootR = Vector3.Distance(m.transform.position, footR.position);
                            isFootLevel = (distToFootL < 0.25f || distToFootR < 0.25f) &&
                                         (distToFootL < 0.5f && distToFootR < 0.5f);
                        }
                    }
                    else
                    {
                        float charY = go.transform.position.y;
                        float meshY = m.transform.position.y;
                        isFootLevel = meshY < charY - 0.2f;
                    }

                    if (priority > highestPriority)
                    {
                        if (priority >= 3 || isFootLevel)
                        {
                            bestMatch = m;
                            highestPriority = priority;
                        }
                    }
                    else if (priority > 0 && bestMatch == null && isFootLevel)
                    {
                        bestMatch = m;
                        highestPriority = priority;
                    }
                }

                if (bestMatch == null && hasFootBones)
                {
                    foreach (var m in meshes)
                    {
                        if (m == null || m.sharedMesh == null || !m.enabled || !m.gameObject.activeInHierarchy) continue;
                        string n = m.name.ToLower();

                        if ((n.Contains("nail") && !n.Contains("shoe")) ||
                            (n.Contains("finger") && !n.Contains("shoe")) ||
                            n.Contains("hand") || n.Contains("arm") ||
                            n.Contains("head") || n.Contains("hair") ||
                            n.Contains("face") || n.Contains("eye") || n.Contains("mouth") ||
                            n.Contains("nose") || n.Contains("ear") || n.Contains("cheek") ||
                            (n.Contains("body") && !n.Contains("foot")) ||
                            (n.Contains("skin") && !n.Contains("shoe") && !n.Contains("foot")) ||
                            (n.Contains("cloth") && !n.Contains("shoe") && !n.Contains("foot")) ||
                            n.Contains("dress") || n.Contains("shirt") ||
                            (n.Contains("pant") && !n.Contains("foot")) ||
                            n.Contains("skirt") || n.Contains("bra") || n.Contains("underwear") ||
                            n.StartsWith("cf_o_face") || n.StartsWith("cf_n_face") ||
                            n.Contains("_face_") || n.Contains("face_"))
                        {
                            continue;
                        }

                        Transform footBone = footL != null ? footL : footR;
                        float distToFoot = Vector3.Distance(m.transform.position, footBone.position);

                        if (footL != null && footR != null)
                        {
                            float distToFootL = Vector3.Distance(m.transform.position, footL.position);
                            float distToFootR = Vector3.Distance(m.transform.position, footR.position);
                            if ((distToFootL < 0.25f || distToFootR < 0.25f) &&
                                (distToFootL < 0.5f && distToFootR < 0.5f))
                            {
                                bestMatch = m;
                                break;
                            }
                        }
                        else if (distToFoot < 0.25f)
                        {
                            bestMatch = m;
                            break;
                        }
                    }
                }

                return bestMatch;
            }
            catch { }
            return null;
        }

        private Transform RecursiveFindTransform(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = RecursiveFindTransform(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private bool TryGetCharacterId(Character.Human human, out int characterId)
        {
            characterId = -1;
            try
            {
                if (human != null && human.data != null)
                {
                    characterId = human.data.ID;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void GetRuntimePoseInfo(GameObject go, out string poseName, out string animName)
        {
            poseName = "";
            animName = "";
            if (go == null) return;

            // H 场景优先：直接读取 HScene 当前动画信息，避免角色 Animator 在该场景常驻 Idle 的误导。
            bool hasHSceneAnim = TryGetHScenePoseInfo(out string hScenePoseName, out string hSceneAnimName);
            if (hasHSceneAnim)
            {
                animName = hSceneAnimName;
                poseName = hScenePoseName;
            }

            try
            {
                var binders = go.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var binder in binders)
                {
                    if (binder == null) continue;
                    var type = binder.GetType();
                    if (type == null || type.Name != "MotionIKDataBinder") continue;
                    var prop = type.GetProperty("StateName", BindingFlags.Public | BindingFlags.Instance);
                    var value = prop?.GetValue(binder) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (hasHSceneAnim && IsIdleLike(value)) continue;
                        poseName = value;
                        break;
                    }
                }
            }
            catch { }

            try
            {
                var animator = go.GetComponentInChildren<Animator>(true);
                if (animator != null && animator.isActiveAndEnabled)
                {
                    string bestAnim = GetBestAnimatorStateName(animator);
                    if (!string.IsNullOrEmpty(bestAnim))
                    {
                        if (!(hasHSceneAnim && IsIdleLike(bestAnim)))
                            animName = bestAnim;
                    }
                }
            }
            catch { }
        }

        private bool IsIdleLike(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            string s = value.ToLowerInvariant();
            return s == "idle" || s.Contains("idle") || s.Contains("statehash:0");
        }

        private bool TryGetHSceneAnimationName(out string animationName)
        {
            if (TryGetHScenePoseInfo(out _, out string anim))
            {
                animationName = anim;
                return true;
            }
            animationName = "";
            return false;
        }

        private bool TryGetHScenePoseInfo(out string poseName, out string animationName)
        {
            poseName = "";
            animationName = "";
            try
            {
                ResolveHSceneTypes();
                var hSceneType = _cachedHSceneType;
                if (hSceneType == null) return false;

                object hSceneObj = FindHSceneInstance();
                if (hSceneObj == null) return false;

                var infoProp = hSceneType.GetProperty("NowAnimationInfo");
                var infoObj = infoProp?.GetValue(hSceneObj);
                if (infoObj == null) return false;

                var infoType = infoObj.GetType();
                string nameAnimation = infoType.GetField("NameAnimation")?.GetValue(infoObj)?.ToString()
                                    ?? infoType.GetProperty("NameAnimation")?.GetValue(infoObj)?.ToString()
                                    ?? infoType.GetProperty("Name")?.GetValue(infoObj)?.ToString()
                                    ?? infoType.GetProperty("name")?.GetValue(infoObj)?.ToString()
                                    ?? "";
                int id = ConvertToInt(infoType.GetField("ID")?.GetValue(infoObj)
                                   ?? infoType.GetProperty("ID")?.GetValue(infoObj), -1);
                int mode = ConvertToInt(infoType.GetField("Mode")?.GetValue(infoObj)
                                     ?? infoType.GetProperty("Mode")?.GetValue(infoObj), -1);
                int kind = ConvertToInt(infoType.GetField("Kind")?.GetValue(infoObj)
                                     ?? infoType.GetProperty("Kind")?.GetValue(infoObj), -1);

                if (string.IsNullOrEmpty(nameAnimation) && id < 0 && mode < 0 && kind < 0) return false;

                animationName = string.IsNullOrEmpty(nameAnimation) ? $"state:id={id}" : nameAnimation;
                poseName = (mode >= 0 || kind >= 0 || id >= 0)
                    ? $"pose:m{mode}_k{kind}_id{id}"
                    : animationName;
                return true;
            }
            catch { }
            return false;
        }

        private int ConvertToInt(object value, int fallback)
        {
            try
            {
                if (value == null) return fallback;
                return Convert.ToInt32(value);
            }
            catch { }
            return fallback;
        }

        private string GetBestAnimatorStateName(Animator animator)
        {
            string firstCandidate = "";
            int layers = Math.Max(1, animator.layerCount);
            for (int i = 0; i < layers; i++)
            {
                string clipName = "";
                try
                {
                    var clips = animator.GetCurrentAnimatorClipInfo(i);
                    if (clips != null && clips.Length > 0 && clips[0].clip != null)
                        clipName = clips[0].clip.name;
                }
                catch { }

                if (string.IsNullOrEmpty(clipName))
                {
                    try
                    {
                        var st = animator.GetCurrentAnimatorStateInfo(i);
                        clipName = $"stateHash:{st.shortNameHash}";
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(firstCandidate) && !string.IsNullOrEmpty(clipName))
                    firstCandidate = clipName;

                if (!string.IsNullOrEmpty(clipName) && !clipName.ToLowerInvariant().Contains("idle"))
                    return clipName;
            }
            return firstCandidate;
        }

        public void SaveCurrentPoseConfig(string poseKey)
        {
            if (string.IsNullOrEmpty(poseKey))
            {
                CfgStatus.Value = Localization.Get("UI_Status_CharIdInvalid");
                return;
            }

            string shoeName = "";
            int shoeVerts = 0;
            try
            {
                var activeCtrl = HeelsController.ActiveInstances.FirstOrDefault(c => c != null && c.CharacterName == FocusCharName)
                              ?? HeelsController.ActiveInstances.FirstOrDefault(c => c != null);
                if (activeCtrl != null)
                {
                    shoeName = activeCtrl.CurrentShoeName ?? "";
                    shoeVerts = activeCtrl.CurrentShoeVertCount;
                }
            }
            catch { }

            string key = BuildPosePresetKey(shoeName, shoeVerts, poseKey);
            var data = new PosePresetData
            {
                poseKey = key,
                hipOffset = CfgHipOffset.Value,
                thighAngle = CfgThighAngle.Value,
                kneeAngle = CfgKneeAngle.Value
            };
            _posePresets[key] = data;
            SavePoseConfigToDisk(data);
            CfgStatus.Value = Localization.Get("UI_Status_PoseSaved", key);
        }

        private string BuildPosePresetKey(string shoeName, int shoeVerts, string poseKey)
        {
            string shoePart = NormalizeShoeKey(shoeName, shoeVerts);
            string posePart = NormalizePoseKey(poseKey);
            return $"{shoePart}@@{posePart}";
        }

        private string NormalizeShoeKey(string shoeName, int shoeVerts)
        {
            string name = (shoeName ?? "").Trim();
            if (string.IsNullOrEmpty(name)) name = "unknown-shoe";
            int verts = shoeVerts < 0 ? 0 : shoeVerts;
            return $"{name}#{verts}";
        }

        private string NormalizePoseKey(string poseKey)
        {
            return (poseKey ?? "").Trim();
        }

        private string GetPoseConfigPath(string poseKey)
        {
            string safe = SanitizeFileName(poseKey);
            if (string.IsNullOrEmpty(safe)) safe = "unknown";
            return Path.Combine(AnimationConfigDir, $"pose_{safe}.json");
        }

        private void SavePoseConfigToDisk(PosePresetData data)
        {
            if (data == null) return;
            try
            {
                string path = GetPoseConfigPath(data.poseKey);
                File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private void LoadAllPoseConfigs()
        {
            try
            {
                _posePresets.Clear();
                if (!Directory.Exists(AnimationConfigDir)) return;
                var files = Directory.GetFiles(AnimationConfigDir, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var text = File.ReadAllText(file);
                        if (string.IsNullOrEmpty(text) || text == "{}") continue;
                        var data = JsonSerializer.Deserialize<PosePresetData>(text);
                        if (data != null && !string.IsNullOrEmpty(data.poseKey))
                        {
                            string key = NormalizePoseKey(data.poseKey);
                            _posePresets[key] = data;
                        }
                    }
                    catch { }
                }
                Log.LogInfo($"Pose Presets Loaded: {_posePresets.Count}");
            }
            catch { }
        }

        public void ProcessCharacter(Character.Human human)
        {
            if (human == null || human.Pointer == IntPtr.Zero) return;
            GameObject go = null;
            try { go = human.gameObject; } catch { }
            if (go == null) return;

            if (HeelzGui.Instance == null)
            {
                GameObject guiGO = new GameObject("HeelzGui_Manager");
                GameObject.DontDestroyOnLoad(guiGO);
                guiGO.AddComponent<HeelzGui>();
            }

            var controller = go.GetComponent<HeelsController>();
            if (controller == null) controller = go.AddComponent<HeelsController>();
            controller.CharacterName = go.name;
            if (CfgEnabled.Value)
            {
                controller.ShowDebugPlane = CfgShowPlane.Value;
                controller.HeadOffsetVisual = CfgMeasureOffset.Value;
                controller.FootPlaneFollowFeet = CfgFootPlaneFollowFeet.Value;
            }

            bool isFocus = (go.name == FocusCharName) || (HeelsController.ActiveInstances.Count == 1);
            TryGetCharacterId(human, out int characterId);
            GetRuntimePoseInfo(go, out string poseName, out string animationName);

            string matchedShoePresetName = "";
            string matchedPosePresetKey = "";

            if (isFocus)
            {
                DebugHeightInfo = Localization.Get("UI_HeightDisplay", controller.CurrentHeadHeight);
                DebugShoeName = controller.CurrentShoeName;
                DebugPoseInfo = string.IsNullOrEmpty(poseName) ? Localization.Get("UI_None") : poseName;
                DebugAnimInfo = string.IsNullOrEmpty(animationName) ? Localization.Get("UI_None") : animationName;
            }

            if (Time.frameCount % 60 == 0 || controller.CurrentShoeID == -1)
            {
                int id = GetShoeID(human);
                var renderer = GetShoeRenderer(go);

                string shoeName = "";
                int vertCount = 0;
                bool isVisible = false;
                bool isShoeLikeName = false;

                if (renderer != null)
                {
                    isVisible = renderer.enabled && renderer.gameObject.activeInHierarchy;
                    shoeName = renderer.name.Replace("(Clone)", "").Trim();
                    if (renderer.sharedMesh != null) vertCount = renderer.sharedMesh.vertexCount;

                    string n = shoeName.ToLower();
                    isShoeLikeName = n.Contains("heel") || n.Contains("boot") || n.Contains("shoe") ||
                                    n.Contains("foot") || n.Contains("o_shoes") || n.Contains("shoes_");
                }

                Func<HeelConfigData, string> GetPureShoeName = (config) =>
                {
                    if (string.IsNullOrEmpty(config.shoeName)) return "";
                    int pipeIndex = config.shoeName.LastIndexOf('|');
                    if (pipeIndex > 0) return config.shoeName.Substring(0, pipeIndex);
                    return config.shoeName;
                };

                Func<HeelConfigData, int> GetShoeNameVertCount = (config) =>
                {
                    if (string.IsNullOrEmpty(config.shoeName)) return 0;
                    int pipeIndex = config.shoeName.LastIndexOf('|');
                    if (pipeIndex > 0 && int.TryParse(config.shoeName.Substring(pipeIndex + 1), out int parsedVert))
                        return parsedVert;
                    return config.vertCount;
                };

                HeelConfigData data = null;

                // 如果找到了渲染器但它是隐藏的 -> 强制脱鞋（归零）
                if (renderer != null && !isVisible)
                {
                    controller.UpdateShoeData(-1, 0, 0, 0);
                    controller.CurrentShoeName = ""; // 清空名字，避免UI显示还在穿鞋
                    controller.CurrentShoeVertCount = 0;
                    controller.CurrentShoeID = -1;   // ID归零，让Update不再认为穿着鞋
                    controller.ActiveRenderer = renderer;
                }
                else if (renderer != null && isVisible)
                {
                    Func<HeelConfigData, string> GetMatchName = (config) =>
                    {
                        if (config == null) return "";
                        if (!string.IsNullOrEmpty(config.matchName)) return config.matchName.Trim();
                        return GetPureShoeName(config);
                    };

                    var dataByNameAndVert = !string.IsNullOrEmpty(shoeName)
                        ? ConfigList.FirstOrDefault(x =>
                            string.Equals(GetMatchName(x), shoeName, StringComparison.OrdinalIgnoreCase) &&
                            (x.vertCount == vertCount || GetShoeNameVertCount(x) == vertCount))
                        : null;
                    var dataByName = !string.IsNullOrEmpty(shoeName)
                        ? ConfigList.FirstOrDefault(x =>
                            string.Equals(GetMatchName(x), shoeName, StringComparison.OrdinalIgnoreCase))
                        : null;
                    // 兼容旧配置：当 matchName 缺失/不匹配时，尝试用唯一顶点数反查
                    var vertCandidates = ConfigList.Where(x =>
                        (x.vertCount == vertCount || GetShoeNameVertCount(x) == vertCount)).ToList();
                    var dataByVertUnique = vertCandidates.Count == 1 ? vertCandidates[0] : null;

                    // 不依赖动态 id：优先 matchName+vert，其次 matchName
                    if (dataByNameAndVert != null)
                    {
                        data = dataByNameAndVert;
                    }
                    else if (dataByName != null)
                    {
                        data = dataByName;
                    }
                    else if (dataByVertUnique != null)
                    {
                        data = dataByVertUnique;
                    }

                    if (data != null)
                    {
                        controller.UpdateShoeData(id, data.height, data.angle, data.toe);
                        controller.CurrentShoeName = data.shoeName;
                        controller.CurrentShoeVertCount = data.vertCount;
                        controller.ActiveRenderer = renderer;
                        controller.CurrentShoeID = id;
                        matchedShoePresetName = data.shoeName;
                    }
                    else
                    {
                        controller.UpdateShoeData(-1, 0, 0, 0);
                        controller.CurrentShoeName = shoeName;
                        controller.CurrentShoeVertCount = vertCount;
                        controller.ActiveRenderer = renderer;
                        if (id != -1 && id != 0)
                        {
                            controller.CurrentShoeID = id;
                        }
                        matchedShoePresetName = "";
                    }
                }
                else if (id != -1 && id != 0)
                {
                    // 裸足/脱鞋（常见是鞋模型被 SetActive(false)）：即使 coorde 里还有 id，也应关闭高跟鞋调整
                    controller.UpdateShoeData(-1, 0, 0, 0);
                    controller.CurrentShoeName = "";
                    controller.CurrentShoeVertCount = 0;
                    controller.CurrentShoeID = -1;
                    controller.ActiveRenderer = null;
                    matchedShoePresetName = "";
                }
                else
                {
                    controller.UpdateShoeData(-1, 0, 0, 0);
                    controller.CurrentShoeName = "";
                    controller.CurrentShoeVertCount = 0;
                    controller.CurrentShoeID = -1;
                    if (renderer != null) controller.ActiveRenderer = renderer;
                    matchedShoePresetName = "";
                }
            }

            // 脱鞋时（鞋模型不可见/未激活）统一关闭高跟与姿势附加修正
            bool hasVisibleShoe = controller.ActiveRenderer != null
                               && controller.ActiveRenderer.enabled
                               && controller.ActiveRenderer.gameObject != null
                               && controller.ActiveRenderer.gameObject.activeInHierarchy
                               && controller.CurrentShoeID != -1;

            if (CfgEnabled.Value)
            {
                // 编辑模式也只在“当前确实穿着并显示鞋子”时生效，脱鞋后强制关闭高跟鞋调整
                bool applyEdit = CfgEditMode.Value && isFocus && hasVisibleShoe;
                controller.RefreshFinalData(applyEdit, CfgHeight.Value, CfgAngle.Value, CfgToeAngle.Value);
            }

            float extraHipOffset = 0f;
            float extraThighAngle = 0f;
            float extraKneeAngle = 0f;
            bool applyPoseEditToAll = CfgEditMode.Value
                                   && HeelsController.ActiveInstances.Count > 1
                                   && (!string.IsNullOrEmpty(poseName) || !string.IsNullOrEmpty(animationName));
            if (CfgEnablePoseAdjust.Value && hasVisibleShoe)
            {
                if (isFocus || applyPoseEditToAll)
                {
                    extraHipOffset = CfgHipOffset.Value;
                    extraThighAngle = CfgThighAngle.Value;
                    extraKneeAngle = CfgKneeAngle.Value;
                }
                else if (!string.IsNullOrEmpty(poseName))
                {
                    // 新规则：姿势预设按「鞋子 + 姿势」联合匹配，避免不同鞋共用同一套姿势
                    string composedKey = BuildPosePresetKey(controller.CurrentShoeName, controller.CurrentShoeVertCount, poseName);
                    if (_posePresets.TryGetValue(composedKey, out var poseCfg))
                    {
                        extraHipOffset = poseCfg.hipOffset;
                        extraThighAngle = poseCfg.thighAngle;
                        extraKneeAngle = poseCfg.kneeAngle;
                        matchedPosePresetKey = composedKey;
                    }
                    // 兼容旧数据：如果不存在联合键，则回退到仅姿势键
                    else if (_posePresets.TryGetValue(NormalizePoseKey(poseName), out poseCfg))
                    {
                        extraHipOffset = poseCfg.hipOffset;
                        extraThighAngle = poseCfg.thighAngle;
                        extraKneeAngle = poseCfg.kneeAngle;
                        matchedPosePresetKey = NormalizePoseKey(poseName);
                    }
                }
            }

            if (isFocus)
            {
                DebugShoePresetInfo = string.IsNullOrEmpty(matchedShoePresetName) ? "None" : matchedShoePresetName;
                DebugPosePresetInfo = string.IsNullOrEmpty(matchedPosePresetKey) ? "None" : matchedPosePresetKey;
            }

            controller.UpdateCharacterRuntime(characterId, poseName, animationName, extraHipOffset, extraThighAngle, extraKneeAngle, TryGetHSceneAnimationName(out _));
        }

        public string SanitizeFileName(string name) { string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars())); string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars); return Regex.Replace(name, invalidRegStr, "_"); }
        public void SaveCurrentConfig(int id, string name, int verts)
        {
            if (string.IsNullOrEmpty(name)) { CfgStatus.Value = Localization.Get("UI_Status_NameEmpty"); return; }

            string cleanName = name;
            int pipeIndex = name.LastIndexOf('|');
            if (pipeIndex > 0)
            {
                cleanName = name.Substring(0, pipeIndex);
                if (verts == 0 && int.TryParse(name.Substring(pipeIndex + 1), out int parsedVert))
                {
                    verts = parsedVert;
                }
            }

            string runtimeMatchName = "";
            try
            {
                var activeCtrl = HeelsController.ActiveInstances.FirstOrDefault(c => c != null && c.CharacterName == FocusCharName)
                              ?? HeelsController.ActiveInstances.FirstOrDefault(c => c != null);
                if (activeCtrl != null && activeCtrl.ActiveRenderer != null)
                    runtimeMatchName = activeCtrl.ActiveRenderer.name.Replace("(Clone)", "").Trim();
            }
            catch { }
            if (string.IsNullOrEmpty(runtimeMatchName)) runtimeMatchName = cleanName;

            // 优先用已存在配置里的列表显示名，避免把 AB 子物体名（如 o_shoes_xxx）作为保存名
            string preferredDisplayName = cleanName;
            var existedByMatch = ConfigList.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.matchName) &&
                x.matchName.Equals(runtimeMatchName, StringComparison.OrdinalIgnoreCase) &&
                (x.vertCount == verts || x.vertCount == 0 || verts == 0));
            if (existedByMatch != null && !string.IsNullOrEmpty(existedByMatch.shoeName))
            {
                preferredDisplayName = existedByMatch.shoeName;
            }

            var newData = new HeelConfigData
            {
                id = id,
                shoeName = preferredDisplayName,   // 列表显示名 / 存储名
                matchName = runtimeMatchName, // 运行时匹配名（子物体名）
                vertCount = verts,
                height = CfgHeight.Value,
                angle = CfgAngle.Value,
                toe = CfgToeAngle.Value
            };
            // 动态 ID 不稳定：不再按 id 删除，改为按匹配键去重
            ConfigList.RemoveAll(x =>
                (!string.IsNullOrEmpty(x.matchName) &&
                 x.matchName.Equals(runtimeMatchName, StringComparison.OrdinalIgnoreCase) &&
                 (x.vertCount == verts || x.vertCount == 0 || verts == 0)) ||
                (x.shoeName == preferredDisplayName && x.vertCount == verts));
            ConfigList.Add(newData);
            try
            {
                string safeName = SanitizeFileName(preferredDisplayName);
                int safeVerts = verts < 0 ? 0 : verts;
                string fileName = $"{safeName}_{safeVerts}.json";
                string fullPath = Path.Combine(ConfigDir, fileName);
                File.WriteAllText(fullPath, JsonSerializer.Serialize(newData, new JsonSerializerOptions { WriteIndented = true }));
                CfgStatus.Value = Localization.Get("UI_Status_Saved", fileName);
            }
            catch (Exception e) { CfgStatus.Value = Localization.Get("UI_Status_Error", e.Message); }
        }

        static class Hooks
        {
            [HarmonyPatch(typeof(Character.Human), "Update")]
            [HarmonyPostfix]
            public static void OnHumanUpdate(Character.Human __instance)
            {
                if (Instance != null) Instance.ProcessCharacter(__instance);
            }

            [HarmonyPatch(typeof(Character.Human), "LateUpdate")]
            [HarmonyPostfix]
            public static void OnHumanLateUpdate(Character.Human __instance)
            {
                if (__instance == null) return;
                try
                {
                    var go = __instance.gameObject;
                    if (go == null) return;
                    var controller = go.GetComponent<HeelsController>();
                    if (controller != null)
                    {
                        controller.ForceApplyPoseAdjustments();
                        controller.ApplyFootPlaneAlignAfterHuman();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 额外的每帧 LateUpdate 驱动，确保在 H 场景也持续应用姿势修正。
        /// </summary>
        public class PoseLateApplyRunner : MonoBehaviour
        {
            public PoseLateApplyRunner(IntPtr ptr) : base(ptr) { }

            void LateUpdate()
            {
                // H 场景里可能不会稳定触发 Character.Human.Update，这里做兜底扫描。
                if (Instance != null && (Time.frameCount % 15 == 0))
                {
                    try
                    {
                        ResolveHSceneTypes();
                        var hSceneType = _cachedHSceneType;
                        if (hSceneType != null)
                        {
                            object hSceneObj = FindHSceneInstance();
                            if (hSceneObj != null)
                            {
                                IEnumerable actorsEnum = null;
                                var actorsProp = hSceneType.GetProperty("Actors");
                                var actorsObj = actorsProp?.GetValue(hSceneObj);
                                actorsEnum = actorsObj as IEnumerable;

                                // Aicomi(H)常见路径：GetActors(HScene.ActorType.All)
                                if (actorsEnum == null)
                                {
                                    var actorTypeType = _cachedHSceneActorTypeEnum;
                                    var getActorsMethod = hSceneType.GetMethod("GetActors");
                                    if (getActorsMethod != null && actorTypeType != null)
                                    {
                                        object allEnumValue = Enum.ToObject(actorTypeType, 3); // All
                                        var arr = getActorsMethod.Invoke(hSceneObj, new object[] { allEnumValue });
                                        actorsEnum = arr as IEnumerable;
                                    }
                                }

                                if (actorsEnum != null)
                                {
                                    foreach (var hActor in actorsEnum)
                                    {
                                        if (hActor == null) continue;
                                        var actorProp = hActor.GetType().GetProperty("Actor");
                                        var actorObj = actorProp?.GetValue(hActor);
                                        var humanProp = actorObj?.GetType().GetProperty("Human");
                                        var humanObj = humanProp?.GetValue(actorObj);
                                        if (humanObj is Character.Human human)
                                        {
                                            Instance.ProcessCharacter(human);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                foreach (var ctrl in HeelsController.ActiveInstances)
                {
                    if (ctrl == null) continue;
                    ctrl.ForceApplyPoseAdjustments();
                }
            }
        }
        private void LoadAllConfigs() { ConfigList.Clear(); if (!Directory.Exists(ConfigDir)) return; var files = Directory.GetFiles(ConfigDir, "*.json"); foreach (var file in files) { try { var text = File.ReadAllText(file); if (string.IsNullOrEmpty(text) || text == "{}") continue; try { var singleData = JsonSerializer.Deserialize<HeelConfigData>(text); if (singleData != null) ConfigList.Add(singleData); } catch { } } catch { } } Log.LogInfo($"Config Loaded: {ConfigList.Count}"); }
    }

    [Serializable] public class HeelConfigData { public int id { get; set; } public string shoeName { get; set; } public string matchName { get; set; } public int vertCount { get; set; } public float height { get; set; } public float angle { get; set; } public float toe { get; set; } }
    [Serializable] public class PosePresetData { public string poseKey { get; set; } public float hipOffset { get; set; } public float thighAngle { get; set; } public float kneeAngle { get; set; } }
}

namespace SwdzHighheels
{
    public class ConfigurationManagerAttributes { public bool? IsAdvanced; public int? Order; public bool? HideDefaultButton; public bool? HideSettingName; public string DispName; public string Description; public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer; }
}

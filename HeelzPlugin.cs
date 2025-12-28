using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Character;
using NewHeelz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Reflection; // 反射必备

namespace NewHeelz
{
    [BepInPlugin("com.user.newheelz", "New Heelz", "4.1.0")] // v4.1 破案修复版
    public class HeelzPlugin : BasePlugin
    {
        public static HeelzPlugin Instance;

        public ConfigEntry<bool> CfgEnabled;
        public ConfigEntry<bool> CfgEditMode;
        public ConfigEntry<float> CfgHeight;
        public ConfigEntry<float> CfgAngle;
        public ConfigEntry<float> CfgToeAngle;
        public ConfigEntry<bool> CfgSaveButton;
        public ConfigEntry<string> CfgStatus;

        private string ConfigDir => Path.Combine(Paths.ConfigPath, "NewHeelz");
        private string UserFile => Path.Combine(ConfigDir, "User_Settings.json");
        public static Dictionary<int, HeelConfigData> ConfigDatabase = new Dictionary<int, HeelConfigData>();

        // 运行时状态
        private string _guiStatus = "初始化...";
        private int _guiTargetID = -1;

        // 反射缓存 (提高性能)
        private PropertyInfo _propCoorde;

        public override void Load()
        {
            Instance = this;
            ClassInjector.RegisterTypeInIl2Cpp<HeelsController>(); // 注册控制器
            if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
            LoadAllConfigs();

            // 绑定配置
            CfgEnabled = Config.Bind("1. 系统", "启用插件", true, "总开关");
            CfgEditMode = Config.Bind("1. 系统", "🔴 编辑模式", false, "开启后强制应用滑块数据，方便调试保存");

            CfgHeight = Config.Bind("2. 姿态调整", "身高 (Height)", 0.05f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 0.20f)));
            CfgAngle = Config.Bind("2. 姿态调整", "脚踝 (Ankle)", 25f, new ConfigDescription("", new AcceptableValueRange<float>(-20f, 90f)));
            CfgToeAngle = Config.Bind("2. 姿态调整", "脚尖 (Toes)", 15f, new ConfigDescription("", new AcceptableValueRange<float>(-50f, 50f)));

            // 保存按钮
            CfgSaveButton = Config.Bind("3. 数据管理", "操作面板", false,
                new ConfigDescription("点击保存", null, new ConfigurationManagerAttributes
                {
                    HideDefaultButton = true,
                    HideSettingName = true,
                    CustomDrawer = DrawSaveUI
                }));

            CfgStatus = Config.Bind("4. 状态信息", "连接状态", "...", "显示当前状态");

            Harmony.CreateAndPatchAll(typeof(Hooks));
            Log.LogInfo("New Heelz (Fix: coorde) Loaded.");
        }

        // --- 核心修复：精准获取鞋子 ID ---
        private int GetShoeID(Character.Human human)
        {
            if (human == null) return -1;

            try
            {
                // 1. 使用反射获取 'coorde' 属性 (根据你的Log)
                if (_propCoorde == null) _propCoorde = human.GetType().GetProperty("coorde");

                // 2. 拿到 HumanCoordinate 对象
                // 注意：这里需要 cast 成 HumanCoordinate，如果没有引用 Assembly-CSharp 可能会报错
                // 但根据之前的编译情况，这个类应该是存在的
                var coordObj = _propCoorde?.GetValue(human) as HumanCoordinate;

                if (coordObj != null && coordObj.Now != null && coordObj.Now.Clothes != null)
                {
                    // 3. 读取鞋子 (索引 7 是室内鞋，索引 8 是外鞋)
                    var parts = coordObj.Now.Clothes.parts;
                    int id = -1;

                    // 优先检查 7号位 (Indoor Shoes)
                    if (parts.Length > 7) id = parts[7].id;

                    // 如果 7号位没穿(0)，或者不存在，检查 8号位 (Outdoor Shoes / Boots)
                    if ((id == 0 || id == -1) && parts.Length > 8) id = parts[8].id;

                    return id;
                }
            }
            catch (Exception)
            {
                // 静默失败，不要刷屏
            }
            return -1;
        }

        // --- 核心循环 ---
        public void ProcessCharacter(Character.Human human)
        {
            if (human == null || human.Pointer == IntPtr.Zero) return;
            GameObject go = null;
            try { go = human.gameObject; } catch { }
            if (go == null) return;

            var controller = go.GetComponent<HeelsController>();
            if (controller == null) controller = go.AddComponent<HeelsController>();

            // 1. 每隔 60 帧 (约1秒) 检查一次 ID，或者当控制器还没 ID 时检查
            // 这样既省性能，又能保证换鞋后能检测到
            if (Time.frameCount % 60 == 0 || controller.CurrentShoeID == -1)
            {
                int id = GetShoeID(human);

                // 如果拿到了有效 ID (非0且非-1)
                if (id != -1 && id != 0)
                {
                    // 查字典应用数据
                    if (ConfigDatabase.TryGetValue(id, out HeelConfigData data))
                    {
                        // 找到了配置：自动应用
                        controller.UpdateShoeData(id, data.height, data.angle, data.toe);

                        // 如果在编辑模式，顺便把 F1 滑块也同步过去
                        if (CfgEditMode.Value && Instance.CfgEditMode.Value) // 双重确认
                        {
                            CfgHeight.Value = data.height;
                            CfgAngle.Value = data.angle;
                            CfgToeAngle.Value = data.toe;
                        }
                    }
                    else
                    {
                        // 没找到配置：归零，但更新 ID 方便用户保存
                        controller.UpdateShoeData(id, 0, 0, 0);
                    }
                }
            }

            // 2. 实时将 F1 滑块数据推给控制器 (仅编辑模式)
            if (CfgEnabled.Value)
            {
                controller.RefreshFinalData(CfgEditMode.Value, CfgHeight.Value, CfgAngle.Value, CfgToeAngle.Value);
            }
        }

        // --- F1 UI 绘制 ---
        private void DrawSaveUI(ConfigEntryBase entry)
        {
            // 扫描当前活跃的控制器
            int validCount = 0;
            int targetID = -1;

            foreach (var ctrl in HeelsController.ActiveInstances)
            {
                if (ctrl != null && ctrl.CurrentShoeID != -1 && ctrl.CurrentShoeID != 0)
                {
                    validCount++;
                    targetID = ctrl.CurrentShoeID; // 取最后一个有效的
                }
            }
            _guiTargetID = targetID;

            // 状态文字
            if (targetID != -1) _guiStatus = $"✅ 已就绪 (ID: {targetID})";
            else _guiStatus = "⚠️ 未检测到有效鞋子 (请确保角色已加载且穿了鞋)";

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = (targetID != -1) ? Color.green : Color.yellow;
            GUILayout.Label(_guiStatus, labelStyle);

            // 按钮
            GUI.backgroundColor = (targetID != -1) ? Color.cyan : Color.gray;
            if (GUILayout.Button($"保存当前配置 (ID: {targetID})", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                if (targetID != -1) SaveCurrentConfig(targetID);
            }
            GUI.backgroundColor = Color.white;
        }

        private void SaveCurrentConfig(int id)
        {
            var newData = new HeelConfigData { id = id, height = CfgHeight.Value, angle = CfgAngle.Value, toe = CfgToeAngle.Value };
            ConfigDatabase[id] = newData;

            List<HeelConfigData> list = new List<HeelConfigData>();
            if (File.Exists(UserFile)) try { list = JsonSerializer.Deserialize<List<HeelConfigData>>(File.ReadAllText(UserFile)) ?? new List<HeelConfigData>(); } catch { }

            list.RemoveAll(x => x.id == id);
            list.Add(newData);

            try
            {
                File.WriteAllText(UserFile, JsonSerializer.Serialize(list.OrderBy(x => x.id), new JsonSerializerOptions { WriteIndented = true }));
                CfgStatus.Value = $"✅ 已保存 ID: {id}";
            }
            catch (Exception e) { CfgStatus.Value = $"保存错误: {e.Message}"; }
        }

        // --- Hook ---
        static class Hooks
        {
            [HarmonyPatch(typeof(Character.Human), "Update")]
            [HarmonyPostfix]
            public static void OnHumanUpdate(Character.Human __instance)
            {
                if (Instance != null) Instance.ProcessCharacter(__instance);
            }
        }

        private void LoadAllConfigs()
        {
            ConfigDatabase.Clear();
            if (!Directory.Exists(ConfigDir)) return;
            foreach (var file in Directory.GetFiles(ConfigDir, "*.json"))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<HeelConfigData>>(File.ReadAllText(file));
                    if (list != null) foreach (var data in list) ConfigDatabase[data.id] = data;
                }
                catch { }
            }
        }
    }

    [Serializable]
    public class HeelConfigData { public int id; public float height; public float angle; public float toe; }
}

namespace NewHeelz { public class ConfigurationManagerAttributes { public bool? IsAdvanced; public int? Order; public bool? HideDefaultButton; public bool? HideSettingName; public string DispName; public string Description; public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer; } }
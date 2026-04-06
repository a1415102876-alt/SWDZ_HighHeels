using System;
using System.Collections.Generic;

namespace SwdzHighheels
{
    public enum Language
    {
        English,
        Chinese
    }

    public static class Localization
    {
        private static Language _currentLanguage = Language.English;
        private static Dictionary<string, Dictionary<Language, string>> _translations = new Dictionary<string, Dictionary<Language, string>>();

        static Localization()
        {
            InitializeTranslations();
        }

        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                LoadLanguage(value);
            }
        }

        private static void InitializeTranslations()
        {
            // UI文本翻译
            _translations["UI_ReferenceLine"] = new Dictionary<Language, string>
            {
                [Language.English] = " Reference Line",
                [Language.Chinese] = " 参考线"
            };

            _translations["UI_Calibration"] = new Dictionary<Language, string>
            {
                [Language.English] = "Calibration: {0:F3}",
                [Language.Chinese] = "校准: {0:F3}"
            };

            _translations["UI_Close"] = new Dictionary<Language, string>
            {
                [Language.English] = "Close",
                [Language.Chinese] = "关闭"
            };

            _translations["UI_SaveConfig"] = new Dictionary<Language, string>
            {
                [Language.English] = "💾 Save Config",
                [Language.Chinese] = "💾 保存配置"
            };

            _translations["UI_EditMode"] = new Dictionary<Language, string>
            {
                [Language.English] = " 🔴 Edit Mode",
                [Language.Chinese] = " 🔴 编辑模式"
            };

            _translations["UI_Target"] = new Dictionary<Language, string>
            {
                [Language.English] = "Target",
                [Language.Chinese] = "目标"
            };

            _translations["UI_Height"] = new Dictionary<Language, string>
            {
                [Language.English] = "Height",
                [Language.Chinese] = "高度"
            };

            _translations["UI_Ankle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Ankle",
                [Language.Chinese] = "脚踝"
            };

            _translations["UI_Toe"] = new Dictionary<Language, string>
            {
                [Language.English] = "Toe",
                [Language.Chinese] = "脚趾"
            };

            _translations["UI_UIScale"] = new Dictionary<Language, string>
            {
                [Language.English] = "UI Scale: {0:F1}x",
                [Language.Chinese] = "界面缩放: {0:F1}x"
            };

            _translations["UI_Status_Saved"] = new Dictionary<Language, string>
            {
                [Language.English] = "Saved: {0}",
                [Language.Chinese] = "已保存: {0}"
            };

            _translations["UI_Status_NameEmpty"] = new Dictionary<Language, string>
            {
                [Language.English] = "Name Empty",
                [Language.Chinese] = "名称为空"
            };

            _translations["UI_Status_Error"] = new Dictionary<Language, string>
            {
                [Language.English] = "Err: {0}",
                [Language.Chinese] = "错误: {0}"
            };

            _translations["UI_HeightDisplay"] = new Dictionary<Language, string>
            {
                [Language.English] = "Height: {0:F3}",
                [Language.Chinese] = "身高: {0:F3}"
            };

            _translations["UI_None"] = new Dictionary<Language, string>
            {
                [Language.English] = "None",
                [Language.Chinese] = "无"
            };

            _translations["UI_DetailInfo"] = new Dictionary<Language, string>
            {
                [Language.English] = "Details",
                [Language.Chinese] = "详细信息"
            };

            _translations["UI_ShoeDetail"] = new Dictionary<Language, string>
            {
                [Language.English] = "ID: {0}, Vertices: {1}, Model: {2}",
                [Language.Chinese] = "ID: {0}, 面数: {1}, 模型: {2}"
            };

            _translations["UI_ShoeDetailNoID"] = new Dictionary<Language, string>
            {
                [Language.English] = "Vertices: {0}, Model: {1}",
                [Language.Chinese] = "面数: {0}, 模型: {1}"
            };

            _translations["UI_NoShoes"] = new Dictionary<Language, string>
            {
                [Language.English] = "No shoes detected",
                [Language.Chinese] = "未检测到鞋子"
            };

            _translations["UI_WindowTitle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Swdz Highheels",
                [Language.Chinese] = "高跟插件"
            };

            _translations["UI_CharacterID"] = new Dictionary<Language, string>
            {
                [Language.English] = "Character ID",
                [Language.Chinese] = "角色ID"
            };

            _translations["UI_CurrentPose"] = new Dictionary<Language, string>
            {
                [Language.English] = "Current Pose",
                [Language.Chinese] = "当前姿势"
            };

            _translations["UI_CurrentAnim"] = new Dictionary<Language, string>
            {
                [Language.English] = "Current Animation",
                [Language.Chinese] = "当前动画"
            };

            _translations["UI_EnablePoseAdjust"] = new Dictionary<Language, string>
            {
                [Language.English] = "Enable Pose Adjust",
                [Language.Chinese] = "启用姿势修正"
            };

            _translations["UI_PoseHipOffset"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose Hip Offset",
                [Language.Chinese] = "姿势髋骨偏移"
            };

            _translations["UI_PoseThighAngle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose Thigh Angle",
                [Language.Chinese] = "姿势大腿角度"
            };

            _translations["UI_PoseKneeAngle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose Knee Angle",
                [Language.Chinese] = "姿势膝盖角度"
            };

            _translations["UI_SavePoseConfig"] = new Dictionary<Language, string>
            {
                [Language.English] = "Save Pose Config",
                [Language.Chinese] = "保存姿势配置"
            };

            // 配置项名称翻译
            _translations["Config_System"] = new Dictionary<Language, string>
            {
                [Language.English] = "1. System",
                [Language.Chinese] = "1. 系统"
            };

            _translations["Config_Body"] = new Dictionary<Language, string>
            {
                [Language.English] = "3. Body",
                [Language.Chinese] = "3. 身体"
            };

            _translations["Config_Tools"] = new Dictionary<Language, string>
            {
                [Language.English] = "4. Tools",
                [Language.Chinese] = "4. 工具"
            };

            _translations["Config_Info"] = new Dictionary<Language, string>
            {
                [Language.English] = "5. Info",
                [Language.Chinese] = "5. 信息"
            };

            _translations["Config_Enable"] = new Dictionary<Language, string>
            {
                [Language.English] = "Enable",
                [Language.Chinese] = "启用"
            };

            _translations["Config_EditMode"] = new Dictionary<Language, string>
            {
                [Language.English] = "Edit Mode",
                [Language.Chinese] = "编辑模式"
            };

            _translations["Config_GamePath"] = new Dictionary<Language, string>
            {
                [Language.English] = "Game Path",
                [Language.Chinese] = "游戏路径"
            };

            _translations["Config_EnableGUI"] = new Dictionary<Language, string>
            {
                [Language.English] = "Enable GUI",
                [Language.Chinese] = "启用界面"
            };

            _translations["Config_GUIKey"] = new Dictionary<Language, string>
            {
                [Language.English] = "GUI Key",
                [Language.Chinese] = "界面按键"
            };

            _translations["Config_UIScale"] = new Dictionary<Language, string>
            {
                [Language.English] = "UI Scale",
                [Language.Chinese] = "界面缩放"
            };

            _translations["Config_Language"] = new Dictionary<Language, string>
            {
                [Language.English] = "Language",
                [Language.Chinese] = "语言"
            };

            _translations["Config_Height"] = new Dictionary<Language, string>
            {
                [Language.English] = "Height",
                [Language.Chinese] = "高度"
            };

            _translations["Config_Ankle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Ankle",
                [Language.Chinese] = "脚踝"
            };

            _translations["Config_Toe"] = new Dictionary<Language, string>
            {
                [Language.English] = "Toe",
                [Language.Chinese] = "脚趾"
            };

            _translations["Config_EnablePoseAdjust"] = new Dictionary<Language, string>
            {
                [Language.English] = "Enable Pose Adjust",
                [Language.Chinese] = "启用姿势修正"
            };

            _translations["Config_PoseHipOffset"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose Hip Offset",
                [Language.Chinese] = "姿势髋骨偏移"
            };

            _translations["Config_PoseThighAngle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose Thigh Angle",
                [Language.Chinese] = "姿势大腿角度"
            };

            _translations["Config_PoseKneeAngle"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose Knee Angle",
                [Language.Chinese] = "姿势膝盖角度"
            };

            _translations["Config_ShowPlane"] = new Dictionary<Language, string>
            {
                [Language.English] = "Show Plane",
                [Language.Chinese] = "显示参考线"
            };

            _translations["Config_FootPlaneFollowFeet"] = new Dictionary<Language, string>
            {
                [Language.English] = "Foot Plane At Feet",
                [Language.Chinese] = "参考线对齐脚底"
            };

            _translations["Config_HeadOffset"] = new Dictionary<Language, string>
            {
                [Language.English] = "Head Offset",
                [Language.Chinese] = "头部偏移"
            };

            _translations["Config_UIPanel"] = new Dictionary<Language, string>
            {
                [Language.English] = "UI Panel",
                [Language.Chinese] = "界面面板"
            };

            _translations["Config_Status"] = new Dictionary<Language, string>
            {
                [Language.English] = "Status",
                [Language.Chinese] = "状态"
            };

            _translations["UI_Status_CharIdInvalid"] = new Dictionary<Language, string>
            {
                [Language.English] = "Character ID invalid",
                [Language.Chinese] = "角色ID无效"
            };

            _translations["UI_Status_PoseSaved"] = new Dictionary<Language, string>
            {
                [Language.English] = "Pose config saved for ID: {0}",
                [Language.Chinese] = "已保存角色姿势配置，ID: {0}"
            };
        }

        public static string Get(string key, params object[] args)
        {
            if (_translations.TryGetValue(key, out var langDict))
            {
                if (langDict.TryGetValue(_currentLanguage, out var text))
                {
                    if (args != null && args.Length > 0)
                    {
                        return string.Format(text, args);
                    }
                    return text;
                }
            }
            return key;
        }

        public static void LoadLanguage(Language lang)
        {
            _currentLanguage = lang;
        }
    }
}


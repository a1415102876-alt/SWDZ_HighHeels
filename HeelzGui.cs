using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Linq;

namespace SwdzHighheels
{
    public class HeelzGui : MonoBehaviour
    {
        public static HeelzGui Instance;
        public HeelzGui(IntPtr ptr) : base(ptr) { }
        private Rect _windowRect = new Rect(50, 50, 360, 620);
        private bool _showUI = false;
        private GUI.WindowFunction _drawFunc;
        private Vector2 _scrollPos = Vector2.zero;
        private void Awake() { Instance = this; _drawFunc = (GUI.WindowFunction)(Action<int>)DrawWindow; }
        private void Update() { if (!HeelzPlugin.Instance.CfgEnableStandaloneUI.Value) return; if (Input.GetKeyDown(HeelzPlugin.Instance.CfgUIKey.Value)) _showUI = !_showUI; if (_showUI) { Vector2 mousePos = Input.mousePosition; mousePos.y = Screen.height - mousePos.y; float scale = HeelzPlugin.Instance.CfgUIScale.Value; if (scale > 0.1f) mousePos /= scale; if (_windowRect.Contains(mousePos)) { Input.ResetInputAxes(); Cursor.visible = true; Cursor.lockState = CursorLockMode.None; } } }
        private void OnGUI() { if (!_showUI || !HeelzPlugin.Instance.CfgEnableStandaloneUI.Value) return; float scale = HeelzPlugin.Instance.CfgUIScale.Value; if (scale < 0.1f) scale = 1.0f; Matrix4x4 oldMatrix = GUI.matrix; GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1)); GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f); _windowRect = GUI.Window(20001, _windowRect, _drawFunc, $"{Localization.Get("UI_WindowTitle")} (x{scale:F1})"); _windowRect.x = Mathf.Clamp(_windowRect.x, 0, (Screen.width / scale) - _windowRect.width); _windowRect.y = Mathf.Clamp(_windowRect.y, 0, (Screen.height / scale) - _windowRect.height); GUI.matrix = oldMatrix; }
        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            float viewportHeight = Mathf.Max(120f, _windowRect.height - 40f);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(viewportHeight));
            DrawContent(true);
            GUILayout.EndScrollView();
        }
        private static void CenterLabel(string text) { GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label(text); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); }

        public static void DrawContent(bool isStandalone)
        {
            GUILayout.BeginVertical();
            var plugin = HeelzPlugin.Instance;
            if (plugin == null) return;
            if (isStandalone) { GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUI.backgroundColor = Color.red; if (GUILayout.Button(Localization.Get("UI_Close"), GUILayout.Width(50))) HeelzGui.Instance._showUI = false; GUI.backgroundColor = Color.white; GUILayout.EndHorizontal(); }

            GUILayout.BeginVertical("box");

            string detailInfo = Localization.Get("UI_None");
            var focusCtrl = HeelsController.ActiveInstances.FirstOrDefault(c =>
                c != null && (c.CharacterName == HeelzPlugin.FocusCharName || HeelsController.ActiveInstances.Count == 1));

            if (focusCtrl == null)
            {
                focusCtrl = HeelsController.ActiveInstances.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.CharacterName));
            }

            if (focusCtrl != null && !string.IsNullOrEmpty(focusCtrl.CurrentShoeName))
            {
                if (focusCtrl.CurrentShoeID != -1 && focusCtrl.CurrentShoeID != 0)
                {
                    detailInfo = Localization.Get("UI_ShoeDetail",
                        focusCtrl.CurrentShoeID,
                        focusCtrl.CurrentShoeVertCount > 0 ? focusCtrl.CurrentShoeVertCount : (focusCtrl.ActiveRenderer?.sharedMesh != null ? focusCtrl.ActiveRenderer.sharedMesh.vertexCount : 0),
                        focusCtrl.CurrentShoeName);
                }
                else
                {
                    int vertCount = focusCtrl.CurrentShoeVertCount > 0
                        ? focusCtrl.CurrentShoeVertCount
                        : (focusCtrl.ActiveRenderer?.sharedMesh != null ? focusCtrl.ActiveRenderer.sharedMesh.vertexCount : 0);
                    detailInfo = Localization.Get("UI_ShoeDetailNoID", vertCount, focusCtrl.CurrentShoeName);
                }
            }
            else if (focusCtrl != null && focusCtrl.ActiveRenderer != null)
            {
                string modelName = focusCtrl.ActiveRenderer.name.Replace("(Clone)", "").Trim();
                int vertCount = focusCtrl.ActiveRenderer.sharedMesh != null ? focusCtrl.ActiveRenderer.sharedMesh.vertexCount : 0;
                if (vertCount > 0)
                {
                    detailInfo = Localization.Get("UI_ShoeDetailNoID", vertCount, modelName);
                }
                else
                {
                    detailInfo = Localization.Get("UI_NoShoes");
                }
            }
            else if (focusCtrl != null)
            {
                detailInfo = Localization.Get("UI_NoShoes");
            }

            GUILayout.Label($"{Localization.Get("UI_DetailInfo")}: <color=yellow><b>{detailInfo}</b></color>");
            GUILayout.Label($"Shoe Preset: <color=yellow><b>{HeelzPlugin.DebugShoePresetInfo}</b></color>");
            GUILayout.Label($"Pose Preset: <color=yellow><b>{HeelzPlugin.DebugPosePresetInfo}</b></color>");
            if (focusCtrl != null)
            {
                GUILayout.Label($"{Localization.Get("UI_CharacterID")}: <color=cyan><b>{focusCtrl.CharacterID}</b></color>");
                GUILayout.Label($"{Localization.Get("UI_CurrentPose")}: <color=cyan><b>{(string.IsNullOrEmpty(focusCtrl.CurrentPoseName) ? HeelzPlugin.DebugPoseInfo : focusCtrl.CurrentPoseName)}</b></color>");
                GUILayout.Label($"{Localization.Get("UI_CurrentAnim")}: <color=cyan><b>{(string.IsNullOrEmpty(focusCtrl.CurrentAnimationName) ? HeelzPlugin.DebugAnimInfo : focusCtrl.CurrentAnimationName)}</b></color>");
            }

            CenterLabel($"<color=cyan><b>{HeelzPlugin.DebugHeightInfo}</b></color>");
            GUILayout.EndVertical();

            GUILayout.Space(5);
            bool edit = GUILayout.Toggle(plugin.CfgEditMode.Value, $" <color=red><b>🔴{Localization.Get("UI_EditMode")}</b></color>");
            if (edit != plugin.CfgEditMode.Value) plugin.CfgEditMode.Value = edit;
            GUILayout.Space(5);
            GUILayout.Label($"{Localization.Get("UI_Height")}: {plugin.CfgHeight.Value:F3}"); plugin.CfgHeight.Value = GUILayout.HorizontalSlider(plugin.CfgHeight.Value, 0f, 0.20f);
            GUILayout.Label($"{Localization.Get("UI_Ankle")}: {plugin.CfgAngle.Value:F1}"); plugin.CfgAngle.Value = GUILayout.HorizontalSlider(plugin.CfgAngle.Value, -20f, 90f);
            GUILayout.Label($"{Localization.Get("UI_Toe")}: {plugin.CfgToeAngle.Value:F1}"); plugin.CfgToeAngle.Value = GUILayout.HorizontalSlider(plugin.CfgToeAngle.Value, -50f, 50f);
            GUILayout.Space(4);
            bool poseEnabled = GUILayout.Toggle(plugin.CfgEnablePoseAdjust.Value, Localization.Get("UI_EnablePoseAdjust"));
            if (poseEnabled != plugin.CfgEnablePoseAdjust.Value) plugin.CfgEnablePoseAdjust.Value = poseEnabled;
            if (plugin.CfgEnablePoseAdjust.Value)
            {
                GUILayout.Label($"{Localization.Get("UI_PoseHipOffset")}: {plugin.CfgHipOffset.Value:F3}"); plugin.CfgHipOffset.Value = GUILayout.HorizontalSlider(plugin.CfgHipOffset.Value, -0.10f, 0.10f);
                GUILayout.Label($"{Localization.Get("UI_PoseThighAngle")}: {plugin.CfgThighAngle.Value:F1}"); plugin.CfgThighAngle.Value = GUILayout.HorizontalSlider(plugin.CfgThighAngle.Value, -30f, 30f);
                GUILayout.Label($"{Localization.Get("UI_PoseKneeAngle")}: {plugin.CfgKneeAngle.Value:F1}"); plugin.CfgKneeAngle.Value = GUILayout.HorizontalSlider(plugin.CfgKneeAngle.Value, -45f, 45f);
            }
            GUILayout.Space(10);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(Localization.Get("UI_SaveConfig"), GUILayout.Height(30)))
            {
                var activeCtrl = HeelsController.ActiveInstances.FirstOrDefault(c => c.CharacterName == HeelzPlugin.FocusCharName);
                if (activeCtrl == null) activeCtrl = HeelsController.ActiveInstances.FirstOrDefault();

                if (activeCtrl != null) plugin.SaveCurrentConfig(activeCtrl.CurrentShoeID, activeCtrl.CurrentShoeName, activeCtrl.CurrentShoeVertCount);
            }
            GUI.backgroundColor = Color.white;
            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.9f, 1f);
            if (GUILayout.Button(Localization.Get("UI_SavePoseConfig"), GUILayout.Height(26)))
            {
                var activeCtrl = HeelsController.ActiveInstances.FirstOrDefault(c => c.CharacterName == HeelzPlugin.FocusCharName);
                if (activeCtrl == null) activeCtrl = HeelsController.ActiveInstances.FirstOrDefault();
                if (activeCtrl != null)
                {
                    string poseKey = string.IsNullOrEmpty(activeCtrl.CurrentPoseName) ? HeelzPlugin.DebugPoseInfo : activeCtrl.CurrentPoseName;
                    plugin.SaveCurrentPoseConfig(poseKey);
                }
            }
            GUI.backgroundColor = Color.white;
            if (!string.IsNullOrEmpty(plugin.CfgStatus.Value) && plugin.CfgStatus.Value != "...") GUILayout.Label($"<size=10><color=white>{plugin.CfgStatus.Value}</color></size>");

            GUILayout.Space(5);
            bool plane = GUILayout.Toggle(plugin.CfgShowPlane.Value, Localization.Get("UI_ReferenceLine"));
            if (plane != plugin.CfgShowPlane.Value) plugin.CfgShowPlane.Value = plane;
            if (plane) { GUILayout.Label(Localization.Get("UI_Calibration", plugin.CfgMeasureOffset.Value)); plugin.CfgMeasureOffset.Value = GUILayout.HorizontalSlider(plugin.CfgMeasureOffset.Value, 0f, 0.4f); }
            if (isStandalone) { GUILayout.Space(10); GUILayout.Label(Localization.Get("UI_UIScale", plugin.CfgUIScale.Value)); plugin.CfgUIScale.Value = GUILayout.HorizontalSlider(plugin.CfgUIScale.Value, 0.5f, 3.0f); }
            GUILayout.EndVertical();
        }
    }
}


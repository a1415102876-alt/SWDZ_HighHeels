using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SwdzHighheels
{
    public class HeelsController : MonoBehaviour
    {
        // 静态注册表
        public static HashSet<HeelsController> ActiveInstances = new HashSet<HeelsController>();

        // 公共属性
        public bool ShowDebugPlane = false;
        /// <summary>
        /// true：参考平面每帧对齐到双脚之间、取较低脚的世界 Y（仍挂在角色根下，用 InverseTransformPoint）。
        /// false：平面保持在角色根 localPosition=0（与旧版 NewHeelz 一致）。
        /// </summary>
        public bool FootPlaneFollowFeet = true;
        public float HeadOffsetVisual = 0.18f;
        public int CurrentShoeID = -1;
        public string CurrentShoeName = "";
        public string CharacterName = "";
        public int CharacterID = -1;
        public SkinnedMeshRenderer ActiveRenderer;
        public float CurrentHeadHeight = 0f;
        public int CurrentShoeVertCount = 0;
        public string PostureStatus = "Init";
        public string CurrentPoseName = "";
        public string CurrentAnimationName = "";

        // 姿态数据
        private float _autoHeight = 0f;
        private float _autoAngle = 0f;
        private float _autoToe = 0f;
        private float _finalHeight = 0f;
        private float _finalAngle = 0f;
        private float _finalToe = 0f;
        private float _lerpHeight = 0f;
        private float _lerpAngle = 0f;
        private float _poseFactor = 1f;
        private float _lerpPoseFactor = 1f;

        // 骨骼和组件
        private Transform _heightBone;
        private Transform _footL, _footR, _toesL, _toesR;
        private Transform _headBone;
        private Transform _hips;
        private Transform _thighL, _thighR;
        private Transform _kneeL, _kneeR;

        private float _extraHipOffset = 0f;
        private float _extraThighAngle = 0f;
        private float _extraKneeAngle = 0f;
        private bool _isInHScene = false;

        // 调试平面
        private GameObject _footPlane;
        private GameObject _headPlane;

        private bool _initialized = false;
        private int _lastPoseApplyFrame = -1;

        public HeelsController(IntPtr ptr) : base(ptr) { }

        void OnEnable() { ActiveInstances.Add(this); }

        void OnDisable()
        {
            CleanupPlane();
            ActiveInstances.Remove(this);
        }

        void OnDestroy()
        {
            CleanupPlane();
            ActiveInstances.Remove(this);
        }

        void Awake() { FindBones(); }

        public void UpdateShoeData(int id, float h, float a, float t)
        {
            CurrentShoeID = id;
            _autoHeight = h;
            _autoAngle = a;
            _autoToe = t;
            if (!_initialized)
            {
                FindBones();
            }
        }

        public void RefreshFinalData(bool isEditing, float debugH, float debugA, float debugT)
        {
            if (isEditing)
            {
                _finalHeight = debugH;
                _finalAngle = debugA;
                _finalToe = debugT;
            }
            else
            {
                _finalHeight = _autoHeight;
                _finalAngle = _autoAngle;
                _finalToe = _autoToe;
            }
        }

        public void UpdateCharacterRuntime(int characterId, string poseName, string animationName, float extraHipOffset, float extraThighAngle, float extraKneeAngle, bool isInHScene)
        {
            CharacterID = characterId;
            CurrentPoseName = poseName ?? "";
            CurrentAnimationName = animationName ?? "";
            _extraHipOffset = extraHipOffset;
            _extraThighAngle = extraThighAngle;
            _extraKneeAngle = extraKneeAngle;
            _isInHScene = isInHScene;
        }

        public void ForceApplyPoseAdjustments()
        {
            if (_lastPoseApplyFrame == Time.frameCount) return;
            if (!_initialized) FindBones();
            _lastPoseApplyFrame = Time.frameCount;
            ApplyPoseBoneAdjustments();
        }

        /// <summary>
        /// 在 Character.Human.LateUpdate 之后调用（见 HeelzPlugin Harmony 后置），
        /// 用当前脚骨骼世界坐标更新参考平面，避免与 Human 动画执行顺序导致错位。
        /// </summary>
        public void ApplyFootPlaneAlignAfterHuman()
        {
            if (!ShowDebugPlane || _footPlane == null) return;
            if (!_initialized) FindBones();
            ApplyFootPlaneLocalAlign();
        }

        void LateUpdate()
        {
            if (!_initialized)
            {
                FindBones();
            }

            // Fix: Use unscaledDeltaTime to support editing in pause mode
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) dt = Time.unscaledDeltaTime;
            float speed = dt * 10f;

            _lerpHeight = Mathf.Lerp(_lerpHeight, _finalHeight, speed);
            _lerpAngle = Mathf.Lerp(_lerpAngle, _finalAngle, speed);

            DetectPosture();
            _lerpPoseFactor = Mathf.Lerp(_lerpPoseFactor, _poseFactor, speed * 0.5f);

            ApplyBodyHeight();
            UpdateDebugPlane();
            ApplyAnkleRotation();
            // 放在最末尾：尽量覆盖动作/IK 对腿部的写回
            ApplyPoseBoneAdjustments();
            MeasureHeight();
        }

        private void DetectPosture()
        {
            // H 场景动作姿态复杂，旧的站/坐阈值会误判，直接按站立因子处理避免把高度压回 0。
            if (_isInHScene)
            {
                _poseFactor = 1f;
                PostureStatus = "HScene";
                return;
            }

            if (_hips == null || _footL == null) return;

            float hipY = _hips.position.y;
            float footY = Mathf.Min(_footL.position.y, _footR.position.y);
            float currentAppliedHeight = (_heightBone != null) ? _heightBone.localPosition.y : 0f;
            float rawHipHeight = hipY - currentAppliedHeight;
            float dist = rawHipHeight - footY;

            if (dist > 0.55f)
            {
                _poseFactor = 1f;
                PostureStatus = "Stand";
            }
            else if (dist < 0.45f)
            {
                _poseFactor = 0f;
                PostureStatus = "Sit";
            }
        }

        private void ApplyBodyHeight()
        {
            if (_heightBone != null)
            {
                Vector3 current = _heightBone.localPosition;
                float effectiveHeight = GetBaseHeightValue();
                if (effectiveHeight < 0.001f) effectiveHeight = 0f;
                _heightBone.localPosition = new Vector3(current.x, effectiveHeight, current.z);
            }
        }

        private float GetBaseHeightValue()
        {
            float baseHeight = _lerpHeight * _lerpPoseFactor;
            if (baseHeight < 0.001f) baseHeight = 0f;
            return baseHeight;
        }

        private void ApplyAnkleRotation()
        {
            if (Math.Abs(_lerpAngle) > 0.01f)
            {
                if (_footL != null) _footL.Rotate(Vector3.right, _lerpAngle);
                if (_footR != null) _footR.Rotate(Vector3.right, _lerpAngle);
            }

            if (Math.Abs(_finalToe) > 0.01f)
            {
                if (_toesL != null) _toesL.localRotation = Quaternion.Euler(_finalToe, 0f, 0f);
                if (_toesR != null) _toesR.localRotation = Quaternion.Euler(_finalToe, 0f, 0f);
            }
        }

        private void ApplyPoseBoneAdjustments()
        {
            ApplyHipOffsetOnly();
            ApplyThighAdjustments();
            ApplyKneeAdjustments();
        }

        private void ApplyHipOffsetOnly()
        {
            if (_heightBone != null)
            {
                Vector3 current = _heightBone.localPosition;
                float y = GetBaseHeightValue() + _extraHipOffset;
                _heightBone.localPosition = new Vector3(current.x, y, current.z);
            }
        }

        private void ApplyThighAdjustments()
        {
            if (Math.Abs(_extraThighAngle) > 0.01f)
            {
                Quaternion q = Quaternion.Euler(_extraThighAngle, 0f, 0f);
                if (_thighL != null) _thighL.localRotation = _thighL.localRotation * q;
                if (_thighR != null) _thighR.localRotation = _thighR.localRotation * q;
            }
        }

        private void ApplyKneeAdjustments()
        {
            if (Math.Abs(_extraKneeAngle) > 0.01f)
            {
                Quaternion q = Quaternion.Euler(_extraKneeAngle, 0f, 0f);
                if (_kneeL != null) _kneeL.localRotation = _kneeL.localRotation * q;
                if (_kneeR != null) _kneeR.localRotation = _kneeR.localRotation * q;
            }
        }

        private void MeasureHeight()
        {
            if (_headBone == null)
            {
                _headBone = RecursiveFind(transform, "cf_j_head");
            }
            if (_headBone != null)
            {
                CurrentHeadHeight = _headBone.position.y + HeadOffsetVisual;
            }
        }

        private void ApplyFootPlaneLocalAlign()
        {
            if (_footPlane == null) return;

            if (FootPlaneFollowFeet && (_footL != null || _footR != null))
            {
                Vector3 worldAtFeet;
                if (_footL != null && _footR != null)
                {
                    worldAtFeet = new Vector3(
                        (_footL.position.x + _footR.position.x) * 0.5f,
                        0f,
                        (_footL.position.z + _footR.position.z) * 0.5f);
                }
                else
                {
                    Vector3 basePos = _footL != null ? _footL.position : _footR.position;
                    worldAtFeet = new Vector3(basePos.x, 0f, basePos.z);
                }

                _footPlane.transform.localPosition = transform.InverseTransformPoint(worldAtFeet);
                _footPlane.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _footPlane.transform.localPosition = Vector3.zero;
                _footPlane.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateDebugPlane()
        {
            if (ShowDebugPlane)
            {
                if (_footPlane == null)
                {
                    _footPlane = CreatePlane("Heelz_Foot_Plane", new Vector3(0.6f, 0.005f, 0.6f), new Color(1f, 1f, 1f, 0.6f));
                    _footPlane.transform.SetParent(transform, false);
                    _footPlane.transform.localRotation = Quaternion.identity;
                    SyncLayer(_footPlane);
                }

                if (_footPlane != null)
                    ApplyFootPlaneLocalAlign();

                if (_headPlane == null)
                {
                    _headPlane = CreatePlane("Heelz_Head_Marker", new Vector3(0.3f, 0.002f, 0.3f), new Color(0f, 1f, 0f, 0.5f));
                }

                if (_headPlane != null && _headBone != null)
                {
                    _headPlane.transform.position = new Vector3(_headBone.position.x, _headBone.position.y + HeadOffsetVisual, _headBone.position.z);
                    _headPlane.transform.rotation = Quaternion.identity;
                    SyncLayer(_headPlane);
                }
            }
            else
            {
                CleanupPlane();
            }
        }

        private GameObject CreatePlane(string name, Vector3 scale, Color color)
        {
            try
            {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plane.name = name;
                plane.transform.localScale = scale;
                Collider collider = plane.GetComponent<Collider>();
                if (collider)
                {
                    UnityEngine.Object.Destroy(collider);
                }
                Renderer renderer = plane.GetComponent<Renderer>();
                if (renderer)
                {
                    Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                    if (shader)
                    {
                        renderer.material.shader = shader;
                        renderer.material.color = color;
                        renderer.material.renderQueue = 4000;
                    }
                }
                return plane;
            }
            catch
            {
                return null;
            }
        }

        private void SyncLayer(GameObject obj)
        {
            if (obj && (obj.layer == 0 || obj.layer != gameObject.layer))
            {
                int targetLayer = 0;
                Il2CppArrayBase<SkinnedMeshRenderer> renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    if (renderer.gameObject.layer != 0)
                    {
                        targetLayer = renderer.gameObject.layer;
                        break;
                    }
                }
                obj.layer = (targetLayer == 0) ? 10 : targetLayer;
            }
        }

        private void CleanupPlane()
        {
            if (_footPlane != null)
            {
                UnityEngine.Object.Destroy(_footPlane);
                _footPlane = null;
            }
            if (_headPlane != null)
            {
                UnityEngine.Object.Destroy(_headPlane);
                _headPlane = null;
            }
        }

        private void FindBones()
        {
            if (_initialized) return;

            _footL = RecursiveFind(transform, "cf_j_foot_L");
            _footR = RecursiveFind(transform, "cf_j_foot_R");
            if (_footL != null) _toesL = RecursiveFind(_footL, "cf_j_toes_L");
            if (_footR != null) _toesR = RecursiveFind(_footR, "cf_j_toes_R");
            _headBone = RecursiveFind(transform, "cf_j_head");
            _hips = RecursiveFind(transform, "cf_j_hips");
            _heightBone = RecursiveFind(transform, "cf_n_height");
            _thighL = RecursiveFind(transform, "cf_j_thigh00_L")
                   ?? RecursiveFind(transform, "cf_j_thigh_L")
                   ?? RecursiveFind(transform, "cf_j_leg01_L");
            _thighR = RecursiveFind(transform, "cf_j_thigh00_R")
                   ?? RecursiveFind(transform, "cf_j_thigh_R")
                   ?? RecursiveFind(transform, "cf_j_leg01_R");

            // 膝盖/小腿调整：优先绑定到 Humanoid 小腿骨（最接近真实关节骨），避免误用蒙皮修正骨导致“拉坏蒙皮”
            try
            {
                var animator = GetComponentInChildren<Animator>(true);
                if (animator != null && animator.isActiveAndEnabled)
                {
                    _kneeL = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                    _kneeR = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                }
            }
            catch { }

            // 兜底：如果 Humanoid 拿不到，再按传统 cf_j_* 关节骨骼名查找
            if (_kneeL == null || _kneeR == null)
            {
                Transform kneeNameL = RecursiveFind(transform, "cf_j_knee_L") ?? RecursiveFind(transform, "cf_j_knee00_L");
                Transform kneeNameR = RecursiveFind(transform, "cf_j_knee_R") ?? RecursiveFind(transform, "cf_j_knee00_R");
                Transform leg01L = RecursiveFind(transform, "cf_j_leg01_L");
                Transform leg01R = RecursiveFind(transform, "cf_j_leg01_R");
                Transform leg02L = RecursiveFind(transform, "cf_j_leg02_L");
                Transform leg02R = RecursiveFind(transform, "cf_j_leg02_R");
                Transform leg03L = RecursiveFind(transform, "cf_j_leg03_L");
                Transform leg03R = RecursiveFind(transform, "cf_j_leg03_R");
                Transform calfL = RecursiveFind(transform, "cf_j_calf_L");
                Transform calfR = RecursiveFind(transform, "cf_j_calf_R");

                if (_kneeL == null)
                {
                    // 你的观察：部分 AC 模型用 leg01 控制小腿弯曲，所以把它提到 leg03 之前
                    _kneeL = kneeNameL ?? leg02L ?? calfL ?? leg01L ?? leg03L;
                    if (_kneeL == _thighL) _kneeL = leg02L ?? calfL ?? leg03L ?? kneeNameL ?? leg01L;
                }
                if (_kneeR == null)
                {
                    _kneeR = kneeNameR ?? leg02R ?? calfR ?? leg01R ?? leg03R;
                    if (_kneeR == _thighR) _kneeR = leg02R ?? calfR ?? leg03R ?? kneeNameR ?? leg01R;
                }
            }

            _initialized = true;
        }

        private Transform RecursiveFind(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = RecursiveFind(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}

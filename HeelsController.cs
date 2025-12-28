using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;

namespace NewHeelz
{
    public class HeelsController : MonoBehaviour
    {
        // 静态注册表
        public static HashSet<HeelsController> ActiveInstances = new HashSet<HeelsController>();

        // 运行时数据
        public int CurrentShoeID = -1;

        // 姿态数据
        private float _autoHeight = 0f;
        private float _autoAngle = 0f;
        private float _autoToe = 0f;
        private float _finalHeight = 0f;
        private float _finalAngle = 0f;
        private float _finalToe = 0f;

        // 骨骼
        private Transform _heightBone;
        private Transform _footL, _footR, _toesL, _toesR;

        private bool _initialized = false;

        public HeelsController(IntPtr ptr) : base(ptr) { }

        void OnEnable() { ActiveInstances.Add(this); }
        void OnDisable() { ActiveInstances.Remove(this); }
        void OnDestroy() { ActiveInstances.Remove(this); }

        void Awake() { FindBones(); }

        public void UpdateShoeData(int id, float h, float a, float t)
        {
            CurrentShoeID = id;
            _autoHeight = h; _autoAngle = a; _autoToe = t;
            RefreshFinalData(false, 0, 0, 0);
        }

        public void RefreshFinalData(bool isEditing, float debugH, float debugA, float debugT)
        {
            if (isEditing) { _finalHeight = debugH; _finalAngle = debugA; _finalToe = debugT; }
            else { _finalHeight = _autoHeight; _finalAngle = _autoAngle; _finalToe = _autoToe; }
        }

        void LateUpdate()
        {
            if (!_initialized) return;

            // 1. 身高控制 (使用 localPosition 修复飞天问题)
            if (_heightBone != null)
            {
                Vector3 current = _heightBone.localPosition;
                float targetY = (_finalHeight > 0.001f) ? _finalHeight : 0f;
                // 锁定 Y 轴，保持 X Z 不变
                _heightBone.localPosition = new Vector3(current.x, targetY, current.z);
            }

            // 2. 脚踝角度 (叠加旋转)
            if (_footL != null && _footR != null && Math.Abs(_finalAngle) > 0.01f)
            {
                _footL.Rotate(Vector3.right, _finalAngle);
                _footR.Rotate(Vector3.right, _finalAngle);
            }

            // 3. 脚尖角度 (锁定局部旋转)
            if (_toesL != null && _toesR != null && Math.Abs(_finalToe) > 0.01f)
            {
                _toesL.localRotation = Quaternion.Euler(_finalToe, 0, 0);
                _toesR.localRotation = Quaternion.Euler(_finalToe, 0, 0);
            }
        }

        private void FindBones()
        {
            if (_initialized) return;
            _heightBone = RecursiveFind(transform, "cf_n_height");
            _footL = RecursiveFind(transform, "cf_j_foot_L");
            _footR = RecursiveFind(transform, "cf_j_foot_R");
            if (_footL != null) _toesL = RecursiveFind(_footL, "cf_j_toes_L");
            if (_footR != null) _toesR = RecursiveFind(_footR, "cf_j_toes_R");
            if (_heightBone != null && _footL != null) _initialized = true;
        }

        private Transform RecursiveFind(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var res = RecursiveFind(parent.GetChild(i), name);
                if (res != null) return res;
            }
            return null;
        }
    }
}
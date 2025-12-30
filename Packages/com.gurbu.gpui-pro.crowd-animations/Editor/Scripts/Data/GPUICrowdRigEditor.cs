// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro.CrowdAnimations
{
    [CustomEditor(typeof(GPUICrowdRig))]
    public class GPUICrowdRigEditor : GPUIEditor
    {
        private GPUICrowdRig _crowdRig;

        protected override void OnEnable()
        {
            base.OnEnable();

            _crowdRig = target as GPUICrowdRig;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            DrawCrowdRig(_crowdRig, serializedObject, contentElement, _helpBoxes);
        }

        public static void DrawCrowdRig(GPUICrowdRig crowdRig, SerializedObject serializedObject, VisualElement rootElement, List<GPUIHelpBox> helpBoxes)
        {
            if (crowdRig == null)
                return;

            rootElement.Add(GPUIEditorUtility.DrawSerializedProperty(serializedObject.FindProperty("skinWeights"), "skinWeights", helpBoxes, out var skinWeightsPF));
            if (Application.isPlaying)
            {
                skinWeightsPF.RegisterValueChangeCallback(evt =>
                {
                    if (GPUICrowdSkinningSystem.IsActive)
                        GPUICrowdSkinningSystem.Instance.ApplySkinWeightsKeywords();
                });
            }

            #region Skinned Mesh Data
            Foldout skinnedMeshDataVE = new Foldout();
            rootElement.Add(skinnedMeshDataVE);
            DrawSkinnedMeshData(crowdRig, serializedObject, helpBoxes, skinnedMeshDataVE);
            #endregion Skinned Mesh Data

            #region Baked Data
            VisualElement bakedDataVE = new VisualElement();
            rootElement.Add(bakedDataVE);
            EditorApplication.delayCall += () => DrawRigBakedData(crowdRig, bakedDataVE);
            #endregion Baked Data
        }

        private static void DrawSkinnedMeshData(GPUICrowdRig crowdRig, SerializedObject serializedObject, List<GPUIHelpBox> helpBoxes, Foldout skinnedMeshDataVE)
        {
            skinnedMeshDataVE.Clear();
            skinnedMeshDataVE.value = false;
            skinnedMeshDataVE.text = "Skinned Meshes [" + crowdRig.GetSkinnedMeshCount() + "]";
            skinnedMeshDataVE.contentContainer.SetEnabled(false);
            skinnedMeshDataVE.Add(GPUIEditorUtility.DrawSerializedProperty(serializedObject.FindProperty("skinnedMeshes"), "skinnedMeshes", helpBoxes, out _));
            skinnedMeshDataVE.Add(GPUIEditorUtility.DrawSerializedProperty(serializedObject.FindProperty("bindPoseDataList"), "bindPoseDataList", helpBoxes, out _));
            skinnedMeshDataVE.Add(GPUIEditorUtility.DrawSerializedProperty(serializedObject.FindProperty("bones"), "bones", helpBoxes, out _));
        }

        private static void DrawRigBakedData(GPUICrowdRig crowdRig, VisualElement bakedDataVE)
        {
            bakedDataVE.Clear();
            int bakedClipCount = crowdRig.GetBakedClipCount();

            if (bakedClipCount > 0)
            {
                Foldout bakedClipsFoldout = new Foldout();
                bakedClipsFoldout.value = false;
                bakedClipsFoldout.text = "Baked Clips [" + bakedClipCount + "]";
                bakedDataVE.Add(bakedClipsFoldout);

                var dict = crowdRig.GetBakedClipIndexDictionary();
                var arr = crowdRig.GetBakedClipDataArray();
                foreach ( var indexes in dict)
                {
                    VisualElement bakedClipVE = new VisualElement();
                    bakedClipVE.AddToClassList("gpui-border");
                    bakedClipVE.AddToClassList("gpui-bg-light");
                    bakedClipVE.SetEnabled(false);
                    bakedClipsFoldout.Add(bakedClipVE);

                    var clipField = new ObjectField("Clip");
                    clipField.objectType = typeof(AnimationClip);
                    clipField.value = crowdRig.GetBakedAnimationClip(indexes.Key);
                    bakedClipVE.Add(clipField);

                    GPUICrowdBakedClipData bakedClipData = arr[indexes.Value];

                    var frameCountField = new IntegerField("Frame Count");
                    frameCountField.value = bakedClipData.clipFrameCount;
                    bakedClipVE.Add(frameCountField);

                    if (bakedClipData.bakedRootMotionIndex >= 0)
                    {
                        var rootMotionIndexField = new IntegerField("Root Motion Index");
                        rootMotionIndexField.value = bakedClipData.bakedRootMotionIndex;
                        bakedClipVE.Add(rootMotionIndexField);
                    }
                }

                if (!Application.isPlaying)
                {
                    Button disposeBakedDataButton = new Button(() =>
                    {
                        crowdRig.Dispose();
                        DrawRigBakedData(crowdRig, bakedDataVE);
                    });
                    disposeBakedDataButton.text = "Dispose Baked Data";
                    disposeBakedDataButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                    disposeBakedDataButton.style.backgroundColor = GPUIEditorConstants.Colors.lightRed;
                    disposeBakedDataButton.focusable = false;
                    bakedClipsFoldout.Add(disposeBakedDataButton);
                }
            }
        }

        public override string GetTitleText() => "GPUI Crowd Rig";
        public override string GetVersionNoText() => GPUICrowdEditorConstants.GetVersionNoText();
        public override string GetWikiURLParams() => "title=GPU_Instancer_Pro-Crowd_Animations#GPUI_Crowd_Instance";
    }
}

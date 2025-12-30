// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.CrowdAnimations
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(-1000)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro-Crowd_Animations#GPUI_Crowd_Runtime_Settings")]
    public class GPUICrowdRuntimeSettingsOverwrite : MonoBehaviour
    {
        public GPUICrowdRuntimeSettings runtimeSettingsOverwrite;

        private void OnEnable()
        {
            GPUICrowdRuntimeSettings.OverwriteSettings(runtimeSettingsOverwrite);
        }
    }
}

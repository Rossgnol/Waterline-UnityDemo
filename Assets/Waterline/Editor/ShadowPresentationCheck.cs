using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Waterline.Editor
{
    public static class ShadowPresentationCheck
    {
        public static void CaptureSaved()
        {
            TidalRouteBuilder.Open();
            ValidateSettings();
            TidalRouteBuilder.Validate();
            Capture(Object.FindFirstObjectByType<FirstPersonController>().view, "TestResults/ShadowFinish/final");
            Debug.Log("[Waterline] SHADOW FINAL PASS: saved pipeline, soft shadows enabled; original resolution, cascade count, distance and scene light biases retained.");
        }

        public static void RunRuntime()
        {
            ValidateSettings();
            WaterVisualCheck.Start("shadow-final");
        }

        private static void ValidateSettings()
        {
            var settings = (UniversalRenderPipelineAsset)(QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline);
            if (!settings.supportsSoftShadows || settings.mainLightShadowmapResolution != 2048 || settings.shadowCascadeCount != 1 || Mathf.Abs(settings.shadowDistance - 50) > .01f)
                throw new InvalidOperationException("Unexpected final shadow pipeline settings");
        }

        public static void Compare() => CompareVariants(new[] { "hard", "soft", "soft-two-cascades" });
        public static void CompareBias() => CompareVariants(new[] { "soft-bias-original", "soft-bias-040", "soft-bias-060" });
        private static void CompareVariants(string[] variants)
        {
            TidalRouteBuilder.Open();
            var original = QualitySettings.renderPipeline;
            var source = (UniversalRenderPipelineAsset)(original ?? GraphicsSettings.defaultRenderPipeline);
            var camera = Object.FindFirstObjectByType<FirstPersonController>().view;
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l => l.type == LightType.Directional);
            float originalBias = sun.shadowBias;
            try
            {
                foreach (string variant in variants)
                {
                    var preview = Object.Instantiate(source);
                    try
                    {
                        var settings = new SerializedObject(preview);
                        settings.FindProperty("m_SoftShadowsSupported").boolValue = variant != "hard";
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        preview.shadowCascadeCount = variant == "soft-two-cascades" ? 2 : 1;
                        sun.shadowBias = variant == "soft-bias-040" ? .4f : variant == "soft-bias-060" ? .6f : originalBias;
                        QualitySettings.renderPipeline = preview;
                        Capture(camera, "TestResults/ShadowFinish/" + variant);
                        Debug.Log($"[Waterline] SHADOW COMPARE {variant}: soft={preview.supportsSoftShadows}, cascades={preview.shadowCascadeCount}, resolution={preview.mainLightShadowmapResolution}, distance={preview.shadowDistance}");
                    }
                    finally { QualitySettings.renderPipeline = original; Object.DestroyImmediate(preview); }
                }
                Debug.Log("[Waterline] SHADOW COMPARE PASS: temporary pipeline clones only; inspect matching camera images. No scene or project settings saved.");
            }
            finally { QualitySettings.renderPipeline = original; sun.shadowBias = originalBias; }
        }

        internal static void Capture(Camera camera, string folder)
        {
            Directory.CreateDirectory(folder);
            // Warm up a newly selected render pipeline before saving the fixed views.
            BoatPresentationBuilder.Shot(camera, folder + "/boat.png", new Vector3(4,-1.1f,-7), new Vector3(0,-1.8f,-1));
            BoatPresentationBuilder.Shot(camera, folder + "/boat.png", new Vector3(4,-1.1f,-7), new Vector3(0,-1.8f,-1));
            BoatPresentationBuilder.Shot(camera, folder + "/berth.png", new Vector3(9,1.7f,3), new Vector3(1,-.9f,-.2f));
            BoatPresentationBuilder.Shot(camera, folder + "/power.png", new Vector3(-8,1.7f,30), new Vector3(-12,1.3f,27));
            BoatPresentationBuilder.Shot(camera, folder + "/instruments.png", new Vector3(-3,1.7f,60), new Vector3(-9,1.8f,66));
            BoatPresentationBuilder.Shot(camera, folder + "/fuse-bench.png", new Vector3(-25,1.7f,27), new Vector3(-27,1.05f,29));
            BoatPresentationBuilder.Shot(camera, folder + "/calibration-bench.png", new Vector3(-7,1.7f,64), new Vector3(-9,1.05f,66));
        }
    }
}

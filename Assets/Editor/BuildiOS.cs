using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace KXGallery.Editor
{
    /// <summary>
    /// Exports the iOS Xcode project into export/ios.
    /// Command line: Unity -batchmode -quit -projectPath . -executeMethod KXGallery.Editor.BuildiOS.Export
    /// </summary>
    public static class BuildiOS
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string OutputPath = "export/ios";

        // Shaders that demos look up with Shader.Find at runtime; the player only contains shaders a build references.
        static readonly string[] RuntimeShaders =
        {
            "Standard", "Unlit/Color", "Sprites/Default", "Legacy Shaders/Particles/Additive", "Particles/Standard Surface",
        };

        [MenuItem("Gallery/Export iOS Project")]
        public static void Export()
        {
            EnsureScene();
            ConfigurePlayer();
            IncludeShaders();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Gallery] iOS export {report.summary.result}: {Path.GetFullPath(OutputPath)}");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>An empty scene: Gallery.Boot creates everything at runtime.</summary>
        static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "kxbuild";
            PlayerSettings.productName = "Unity Gallery";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.kxbuild.unitygallery");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.statusBarHidden = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SplashScreen.showUnityLogo = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, IconKind.Application);
                PlayerSettings.SetIcons(NamedBuildTarget.iOS, Enumerable.Repeat(icon, sizes.Length).ToArray(), IconKind.Application);
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }
        }

        static void IncludeShaders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in RuntimeShaders)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[Gallery] Shader not found: {name}");
                    continue;
                }
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}

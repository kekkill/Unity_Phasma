using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Phasma.Ghost.Editor
{
    internal static class PhasmaGhostSetup
    {
        private const string Directory = "Assets/PhasmaGhost";
        private const string ModelPath = Directory + "/Models/PhasmaGhost.fbx";
        private const string Generated = Directory + "/Generated";

        private static readonly string[] MaterialNames =
        {
            "Skin", "Clothes", "Shoes", "Eyes", "Hair"
        };

        private static readonly string[] ClipNames =
        {
            "Idle", "Walk_InPlace", "Chase_InPlace", "Walk_RootMotion", "Chase_RootMotion"
        };

        [MenuItem("Phasma/Set Up Ghost (URP)")]
        private static void SetUp()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Open a Unity URP project before setting up the ghost.");
            }

            var modelImporter = LoadImporter<ModelImporter>(ModelPath);
            if (!AssetDatabase.IsValidFolder(Generated))
            {
                AssetDatabase.CreateFolder(Directory, "Generated");
            }

            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.preserveHierarchy = true;
            modelImporter.importAnimation = false;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;

            foreach (var name in MaterialNames)
            {
                var material = GetOrCreateMaterial(name, shader);
                var source = new AssetImporter.SourceAssetIdentifier(typeof(Material), name);
                modelImporter.AddRemap(source, material);
            }

            modelImporter.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Avatar>().FirstOrDefault(item => item.isValid && item.isHuman);
            if (avatar == null)
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(ModelPath);
                Debug.LogError("PhasmaGhost needs a valid Humanoid Avatar. Open Rig > Configure, check the mapping, then run Phasma > Set Up Ghost (URP) again.");
                return;
            }

            var clips = ClipNames.Select(name => ImportClip(name, avatar)).ToArray();
            var controller = GetOrCreatePreviewController(clips);
            var prefab = GetOrCreatePreviewPrefab(controller, avatar);
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        private static Material GetOrCreateMaterial(string name, Shader shader)
        {
            var materialPath = Generated + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                return material;
            }

            var cutout = name == "Hair";
            var baseColor = ImportTexture(name + "_BaseColor", TextureImporterType.Default, true, cutout);
            var normal = ImportTexture(name + "_Normal", TextureImporterType.NormalMap, false, false);
            var mask = ImportTexture(name + "_Mask", TextureImporterType.Default, false, true);

            material = new Material(shader);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1);
            material.SetFloat("_WorkflowMode", 1);
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.SetTexture("_OcclusionMap", mask);
            material.SetFloat("_OcclusionStrength", .76f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");

            if (cutout)
            {
                material.SetFloat("_AlphaClip", 1);
                material.SetFloat("_Cutoff", .36f);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }

            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static Texture2D ImportTexture(string name, TextureImporterType type, bool srgb, bool alpha)
        {
            var path = Directory + "/Textures/" + name + ".png";
            var importer = LoadImporter<TextureImporter>(path);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha && srgb;
            importer.mipmapEnabled = true;
            if (alpha && srgb)
            {
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = .36f;
            }

            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static AnimationClip ImportClip(string name, Avatar avatar)
        {
            var path = Directory + "/Animations/PhasmaGhost@" + name + ".fbx";
            var importer = LoadImporter<ModelImporter>(path);
            var rootMotion = name.EndsWith("_RootMotion", StringComparison.Ordinal);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.preserveHierarchy = true;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;

            var rootPath = importer.transformPaths.FirstOrDefault(item =>
                item == "Root" || item.EndsWith("/Root", StringComparison.Ordinal));
            importer.motionNodeName = rootMotion ? rootPath ?? string.Empty : string.Empty;

            var source = importer.defaultClipAnimations.Single();
            source.name = name;
            source.loopTime = true;
            source.loopPose = false;
            source.lockRootRotation = true;
            source.lockRootHeightY = true;
            source.lockRootPositionXZ = !rootMotion;
            source.keepOriginalOrientation = true;
            source.keepOriginalPositionY = true;
            source.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { source };
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(clip => clip.name == name);
        }

        private static AnimatorController GetOrCreatePreviewController(AnimationClip[] clips)
        {
            var path = Generated + "/PhasmaGhostPreview.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
            {
                return controller;
            }

            const string parameter = "Motion";
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(parameter, AnimatorControllerParameterType.Int);
            var machine = controller.layers[0].stateMachine;

            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                var state = machine.AddState(clip.name);
                state.motion = clip;
                if (i == 0)
                {
                    machine.defaultState = state;
                }

                var transition = machine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, i, parameter);
                transition.canTransitionToSelf = false;
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = .12f;
            }

            return controller;
        }

        private static GameObject GetOrCreatePreviewPrefab(AnimatorController controller, Avatar avatar)
        {
            var path = Generated + "/PhasmaGhostPreview.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return prefab;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = "PhasmaGhostPreview";
                var animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = true;
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static T LoadImporter<T>(string path) where T : AssetImporter
        {
            var importer = AssetImporter.GetAtPath(path) as T;
            return importer ?? throw new InvalidOperationException("Unable to import " + path);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Phasma.Izlom.Editor
{
    internal static class IzlomAssetSetup
    {
        private const string Folder = "Assets/Phasma/Izlom";
        private const string ModelPath = Folder + "/Izlom.fbx";
        private const string MaterialPath = Folder + "/Izlom_Surface.mat";
        private static readonly string[] ClipNames = { "Idle", "Walk_InPlace", "Walk_RootMotion" };

        [MenuItem("Tools/Phasma/Configure Izlom")]
        private static void Configure()
        {
            try
            {
                ConfigureAsset();
            }
            catch (InvalidOperationException error)
            {
                Debug.LogError(error.Message);
            }
        }

        private static void ConfigureAsset()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Izlom asset was not found at " + ModelPath);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Izlom requires the Universal Render Pipeline Lit shader.");

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.preserveHierarchy = true;
            importer.optimizeGameObjects = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            using (var settings = new SerializedObject(importer))
            {
                var rigRoot = settings.FindProperty("m_HumanDescription.m_RootMotionBoneName");
                if (rigRoot != null)
                {
                    rigRoot.stringValue = "Root";
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            importer.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var root = model == null ? null : model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "Root");
            if (root == null)
                throw new InvalidOperationException("Izlom's Root bone was not found in the imported model.");

            var clips = new List<ModelImporterClipAnimation>();
            foreach (var name in ClipNames)
            {
                var clip = importer.defaultClipAnimations.FirstOrDefault(candidate =>
                    ClipLabel(candidate.takeName) == name || ClipLabel(candidate.name) == name);
                if (clip == null)
                    throw new InvalidOperationException("Izlom animation take is missing: " + name);

                clip.name = name;
                clip.loopTime = true;
                clip.loopPose = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = name != "Walk_RootMotion";
                clips.Add(clip);
            }

            var material = CreateMaterial(shader);
            importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.motionNodeName = AnimationUtility.CalculateTransformPath(root, model.transform);
            importer.clipAnimations = clips.ToArray();
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Izlom_Surface"), material);
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Debug.Log("Izlom is configured with a Generic rig, three looped clips and a URP Lit material.");
        }

        private static string ClipLabel(string take)
        {
            if (string.IsNullOrEmpty(take))
                return string.Empty;

            return take.Substring(take.LastIndexOf('|') + 1);
        }

        private static Material CreateMaterial(Shader shader)
        {
            var color = LoadTexture("Izlom_BaseColor.png", TextureImporterType.Default, true, false);
            var normal = LoadTexture("Izlom_Normal.png", TextureImporterType.NormalMap, false, false);
            var packed = LoadTexture("Izlom_URP_Mask.png", TextureImporterType.Default, false, true);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Izlom_Surface" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetFloat("_WorkflowMode", 1f);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_MetallicGlossMap", packed);
            material.SetTexture("_OcclusionMap", packed);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_OcclusionStrength", 0.8f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            material.DisableKeyword("_SPECULAR_SETUP");
            material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadTexture(string name, TextureImporterType type, bool srgb, bool useAlpha)
        {
            var path = Folder + "/Textures/" + name;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Izlom texture was not found: " + Path.GetFileName(path));

            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.alphaSource = useAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}

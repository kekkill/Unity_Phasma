using System;
using UnityEditor;
using UnityEngine;

internal sealed class PhasmaGhostModelImport : AssetPostprocessor
{
    private const string AssetRoot = "Assets/PhasmaGhost/";
    private const string IzlomModel = "Assets/Izlom/Resources/Izlom/Izlom.fbx";

    public override uint GetVersion() => 2;

    private void OnPreprocessModel()
    {
        if (!IsSkrutka && !IsIzlom) return;

        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.preserveHierarchy = true;
        importer.importAnimation = IsIzlom || IsSkrutkaClip;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.addCollider = false;
        importer.importCameras = false;
        importer.importLights = false;
    }

    private void OnPostprocessAnimation(GameObject model, AnimationClip clip)
    {
        if (!IsSkrutkaClip && !IsIzlom) return;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private bool IsSkrutka => assetPath.StartsWith(AssetRoot, StringComparison.Ordinal);
    private bool IsSkrutkaClip => IsSkrutka && assetPath.Contains("PhasmaGhost@");
    private bool IsIzlom => assetPath == IzlomModel;
}

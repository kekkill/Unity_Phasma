using System;
using UnityEditor;
using UnityEngine;

internal sealed class PhasmaGhostModelImport : AssetPostprocessor
{
    private const string AssetRoot = "Assets/PhasmaGhost/";

    public override uint GetVersion() => 1;

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(AssetRoot, StringComparison.Ordinal)) return;

        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.preserveHierarchy = true;
        importer.importAnimation = IsClip();
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.addCollider = false;
        importer.importCameras = false;
        importer.importLights = false;
    }

    private void OnPostprocessAnimation(GameObject model, AnimationClip clip)
    {
        if (!assetPath.StartsWith(AssetRoot, StringComparison.Ordinal) || !IsClip()) return;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private bool IsClip() => assetPath.Contains("PhasmaGhost@");
}

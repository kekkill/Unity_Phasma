using System;
using UnityEditor;
using UnityEngine;

internal sealed class PhasmaGhostModelImport : AssetPostprocessor
{
    private const string AssetRoot = "Assets/PhasmaGhost/";
    private const string IzlomModel = "Assets/Izlom/Resources/Izlom/Izlom.fbx";
    private const string InvestigatorModel = "Assets/PhasmaInvestigator/Resources/PhasmaInvestigator/Phasma_Investigator.fbx";

    public override uint GetVersion() => 3;

    private void OnPreprocessModel()
    {
        bool isInvestigator = assetPath == InvestigatorModel;
        if (!IsSkrutka && !IsIzlom && !isInvestigator) return;

        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.preserveHierarchy = true;
        importer.importAnimation = IsIzlom || IsSkrutkaClip || isInvestigator;
        importer.materialImportMode = isInvestigator ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None;
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

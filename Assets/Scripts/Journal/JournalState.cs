using System;
using System.Collections.Generic;
using UnityEngine;

public enum EvidenceMark { Unknown, Confirmed, Excluded }
public enum SaltObservation { Unknown, AvoidedOutsideHunt, Footprints, Ignored }
public enum CrossObservation { Unknown, Repelled, Ignored }

// Personal notes and a stable catalog ID. Networking can replicate these separately later.
public class JournalState : MonoBehaviour
{
    public const int EvidenceCount = 4;
    [SerializeField] private JournalCatalog catalog;
    [SerializeField] private EvidenceMark freezing;
    [SerializeField] private EvidenceMark emf;
    [SerializeField] private EvidenceMark ultraviolet;
    [SerializeField] private EvidenceMark writing;
    [SerializeField] private SaltObservation saltObservation;
    [SerializeField] private CrossObservation crossObservation;
    [SerializeField] private string selectedGhostId;
    public event Action<JournalState> Changed;
    public JournalCatalog Catalog => catalog;
    public SaltObservation Salt => saltObservation;
    public CrossObservation Cross => crossObservation;
    public string SelectedGhostId => selectedGhostId;
    public JournalGhostEntry SelectedGhost => catalog != null ? catalog.Find(selectedGhostId) : null;
    public bool SelectionMatches => SelectedGhost != null && IsCompatible(SelectedGhost.Profile);

    private static void ValidateEvidence(EvidenceType type)
    {
        if ((int)type < 0 || (int)type >= EvidenceCount) throw new ArgumentOutOfRangeException(nameof(type));
    }
    public EvidenceMark GetMark(EvidenceType type)
    {
        ValidateEvidence(type);
        switch (type)
        {
            case EvidenceType.Freezing: return freezing;
            case EvidenceType.Emf: return emf;
            case EvidenceType.Ultraviolet: return ultraviolet;
            case EvidenceType.Writing: return writing;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
    public bool SetMark(EvidenceType type, EvidenceMark value)
    {
        ValidateEvidence(type);
        if (!Enum.IsDefined(typeof(EvidenceMark), value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (GetMark(type) == value) return false;
        switch (type)
        {
            case EvidenceType.Freezing: freezing = value; break;
            case EvidenceType.Emf: emf = value; break;
            case EvidenceType.Ultraviolet: ultraviolet = value; break;
            case EvidenceType.Writing: writing = value; break;
        }
        Changed?.Invoke(this);
        return true;
    }
    public bool SetSalt(SaltObservation value)
    {
        if (!Enum.IsDefined(typeof(SaltObservation), value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (saltObservation == value) return false;
        saltObservation = value;
        Changed?.Invoke(this);
        return true;
    }
    public bool SetCross(CrossObservation value)
    {
        if (!Enum.IsDefined(typeof(CrossObservation), value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (crossObservation == value) return false;
        crossObservation = value;
        Changed?.Invoke(this);
        return true;
    }
    public bool IsCompatible(GhostProfile profile)
    {
        if (profile == null) return false;
        for (int i = 0; i < EvidenceCount; i++)
        {
            EvidenceMark mark = GetMark((EvidenceType)i);
            bool has = profile.HasEvidence((EvidenceType)i);
            if ((mark == EvidenceMark.Confirmed && !has) || (mark == EvidenceMark.Excluded && has)) return false;
        }
        if (saltObservation == SaltObservation.AvoidedOutsideHunt && profile.SaltResponse != GhostSaltResponse.AvoidUntilHunt) return false;
        if (saltObservation == SaltObservation.Footprints && profile.SaltResponse != GhostSaltResponse.LeaveFootprints) return false;
        if (saltObservation == SaltObservation.Ignored && profile.SaltResponse != GhostSaltResponse.Ignore) return false;
        if (crossObservation == CrossObservation.Repelled && profile.IgnoresCross) return false;
        if (crossObservation == CrossObservation.Ignored && !profile.IgnoresCross) return false;
        return true;
    }
    public IEnumerable<JournalGhostEntry> MatchingEntries()
    {
        if (catalog == null) yield break;
        foreach (JournalGhostEntry entry in catalog.Entries)
            if (entry != null && IsCompatible(entry.Profile)) yield return entry;
    }
    public bool SelectGhost(string id)
    {
        JournalGhostEntry entry = catalog != null ? catalog.Find(id) : null;
        if (entry == null || !IsCompatible(entry.Profile)) return false;
        if (selectedGhostId == id) return true;
        selectedGhostId = id;
        Changed?.Invoke(this);
        return true;
    }
    public void ClearSelection()
    {
        if (string.IsNullOrEmpty(selectedGhostId)) return;
        selectedGhostId = string.Empty;
        Changed?.Invoke(this);
    }
    public void ResetNotes()
    {
        freezing = emf = ultraviolet = writing = EvidenceMark.Unknown;
        saltObservation = SaltObservation.Unknown;
        crossObservation = CrossObservation.Unknown;
        selectedGhostId = string.Empty;
        Changed?.Invoke(this);
    }
    public void BindCatalog(JournalCatalog value)
    {
        catalog = value;
        ResetNotes();
    }
}

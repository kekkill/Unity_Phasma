using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JournalGhostEntry
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private GhostProfile profile;
    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) && profile != null ? profile.GhostName : displayName;
    public string Description => description;
    public GhostProfile Profile => profile;
}

// Public reference book, never a reference to the actual ghost in a running round.
[CreateAssetMenu(fileName = "JournalCatalog", menuName = "Phasma/Journal Catalog")]
public class JournalCatalog : ScriptableObject
{
    [SerializeField] private JournalGhostEntry[] entries = new JournalGhostEntry[0];
    public IReadOnlyList<JournalGhostEntry> Entries => entries ?? Array.Empty<JournalGhostEntry>();
    public JournalGhostEntry Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (JournalGhostEntry entry in Entries)
            if (entry != null && entry.Id == id) return entry;
        return null;
    }
    public bool TryValidate(out string reason)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (Entries.Count == 0) { reason = "Journal catalog is empty."; return false; }
        foreach (JournalGhostEntry entry in Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || entry.Profile == null || !ids.Add(entry.Id))
            { reason = "Each journal entry needs a unique ID and a ghost profile."; return false; }
        }
        reason = null;
        return true;
    }
}

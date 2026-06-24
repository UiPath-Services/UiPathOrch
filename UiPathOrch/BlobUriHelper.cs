// Compares two pre-signed blob URIs (as returned by GetBucketReadUri / GetBucketWriteUri) to decide
// whether they reference the same physical storage object. Used by Copy-OrchBucketItem to skip a copy
// when the source and destination buckets -- even different Orchestrator buckets in different tenants --
// resolve to the same external storage object, which would otherwise stream a file onto itself.
public static class BlobUriHelper
{
    /// <summary>
    /// True when two pre-signed blob URIs point to the same physical object: same scheme, host, port,
    /// and (decoded) path. The query string (signature, expiry, verb-specific params) differs between a
    /// GET and a PUT for the same object and is deliberately ignored. The comparison is conservative --
    /// a false negative (e.g. two Orchestrators emitting different URL forms for the same object) just
    /// means the file is copied as before, while a false positive needs scheme+host+port+path to all
    /// collide, which is the same object.
    /// </summary>
    public static bool SamePhysicalObject(string? uriA, string? uriB)
    {
        if (string.IsNullOrWhiteSpace(uriA) || string.IsNullOrWhiteSpace(uriB)) return false;
        if (!Uri.TryCreate(uriA, UriKind.Absolute, out var a)) return false;
        if (!Uri.TryCreate(uriB, UriKind.Absolute, out var b)) return false;

        // Scheme / host / port are case-insensitive (DNS and scheme are). Uri.Port returns the default
        // port for the scheme when none is given, so https with and without an explicit :443 still match.
        if (!string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)) return false;
        if (a.Port != b.Port) return false;

        // The object key (path) is case-sensitive (S3 keys / Azure blob names are), but decoded so that
        // percent-encoding differences (e.g. "%20" vs a literal space) do not cause a spurious mismatch.
        string pathA = a.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        string pathB = b.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        return string.Equals(pathA, pathB, StringComparison.Ordinal);
    }
}

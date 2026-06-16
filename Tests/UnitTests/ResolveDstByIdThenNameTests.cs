using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Characterization tests for ResolveDstByIdThenName -- the pure decision core
// extracted from the simple name-based FindDst* family (FindDstBucket /
// FindDstQueue / FindDstRelease / FindDstMachine / FindDstCalendar /
// FindDstCredentialStore / FindDstTestSet).
//
// These pin the SHARED logic (id guard -> src lookup by id -> dst match by
// name) so that, if the wrappers are later consolidated onto this core, the
// decision behavior is provably unchanged. The per-wrapper IO variance
// (WriteWarning vs WriteError, ErrorIds, messages, try/catch presence) lives in
// the wrappers, NOT here, and is characterized separately for the consolidation
// decision -- it is intentionally out of scope for this pure core.
public class ResolveDstByIdThenNameTests
{
    private sealed record Ent(long? Id, string? Name);

    private static (Ent? dst, FindDstByNameResult result) Run(
        Ent[]? src, long? id, Ent[]? dst) =>
        ResolveDstByIdThenName(src, id, e => e.Id, dst, e => e.Name, e => e.Name);

    [Fact]
    public void NullId_ReturnsNullOrZeroId()
    {
        var (dst, result) = Run([new Ent(5, "A")], null, [new Ent(9, "A")]);
        Assert.Equal(FindDstByNameResult.NullOrZeroId, result);
        Assert.Null(dst);
    }

    [Fact]
    public void ZeroId_ReturnsNullOrZeroId()
    {
        // Every wrapper guards `srcId == 0` the same way as null.
        var (dst, result) = Run([new Ent(5, "A")], 0, [new Ent(9, "A")]);
        Assert.Equal(FindDstByNameResult.NullOrZeroId, result);
        Assert.Null(dst);
    }

    [Fact]
    public void SrcIdNotPresent_ReturnsSrcNotFound()
    {
        var (dst, result) = Run([new Ent(5, "A")], 7, [new Ent(9, "A")]);
        Assert.Equal(FindDstByNameResult.SrcNotFound, result);
        Assert.Null(dst);
    }

    [Fact]
    public void SrcFoundButNoDstWithSameName_ReturnsDstNotFound()
    {
        var (dst, result) = Run([new Ent(5, "A")], 5, [new Ent(9, "B")]);
        Assert.Equal(FindDstByNameResult.DstNotFound, result);
        Assert.Null(dst);
    }

    [Fact]
    public void SrcAndDstMatch_ReturnsResolvedDst()
    {
        var dstA = new Ent(9, "A");
        var (dst, result) = Run([new Ent(5, "A")], 5, [dstA, new Ent(10, "B")]);
        Assert.Equal(FindDstByNameResult.Resolved, result);
        Assert.Same(dstA, dst);
    }

    [Fact]
    public void NameMatchIsCaseInsensitive()
    {
        // Inherited from ResolveDstByName (OrdinalIgnoreCase), shared by the family.
        var dstA = new Ent(9, "myBucket");
        var (dst, result) = Run([new Ent(5, "MYBUCKET")], 5, [dstA]);
        Assert.Equal(FindDstByNameResult.Resolved, result);
        Assert.Same(dstA, dst);
    }

    [Fact]
    public void NullSrcEntities_ReturnsSrcNotFound()
    {
        var (dst, result) = Run(null, 5, [new Ent(9, "A")]);
        Assert.Equal(FindDstByNameResult.SrcNotFound, result);
        Assert.Null(dst);
    }

    [Fact]
    public void NullDstEntities_ReturnsDstNotFound()
    {
        var (dst, result) = Run([new Ent(5, "A")], 5, null);
        Assert.Equal(FindDstByNameResult.DstNotFound, result);
        Assert.Null(dst);
    }

    [Fact]
    public void SrcWithNullName_CannotMatch_ReturnsDstNotFound()
    {
        // ResolveDstByName returns null on a null/empty srcName, so a src entity
        // with no name never resolves -- pinned so consolidation preserves it.
        var (dst, result) = Run([new Ent(5, null)], 5, [new Ent(9, "A")]);
        Assert.Equal(FindDstByNameResult.DstNotFound, result);
        Assert.Null(dst);
    }
}

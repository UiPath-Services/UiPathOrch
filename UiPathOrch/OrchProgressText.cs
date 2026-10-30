using System.Text;

namespace UiPath.PowerShell.Core;

// =============================================================================
// OrchProgressText.cs -- Width-safety helpers for Write-Progress StatusDescription
// =============================================================================
//
// PowerShell issue #21293: the console host sizes the progress bar by its string.Length
// (UTF-16 code units), not by display cells. A character that is one code unit but two
// cells (a BMP East Asian Wide / Fullwidth character) makes the rendered line longer than
// PowerShell computed, pushing the bar's trailing ']' onto the next line, where it then
// sticks. Fix tracked by PR #26185.
//
// Only those characters are mis-sized by the count. A supplementary-plane character (emoji,
// CJK Ext B such as U+20BB7) is two code units AND two cells, so the old count is right.
//
// The fixed host (7.7+) counts cells with ConsoleControl.LengthInBufferCells, a fixed
// wcwidth table, which is still wrong for two groups: EAW Ambiguous symbols (° × ※ → ■ ...),
// which conhost renders two cells with a CJK font while the table says one, and the W
// emoji/symbols newer than that table (⌚ ⭐ ...). Those are unsafe on EVERY host version.
// ProgressReporter consults these helpers and collapses the unsafe runs.

// True/false: does a string contain any code point that the buggy renderer mis-sizes?
public static class EastAsianWidth
{
    // Inclusive code-point ranges that are one UTF-16 code unit but two cells: the BMP
    // ranges of East Asian Width W and F, from Unicode 16.0 EastAsianWidth.txt (adjacent
    // ranges merged). Supplementary planes are deliberately absent (see the file header).
    // Sorted ascending, non-overlapping -> binary search.
    private static readonly (int Lo, int Hi)[] WideRanges =
    [
        (0x1100, 0x115F), // Hangul Jamo leading consonants
        (0x231A, 0x231B), // ⌚ ⌛
        (0x2329, 0x232A), // 〈 〉 angle brackets
        (0x23E9, 0x23EC), // ⏩ .. ⏬
        (0x23F0, 0x23F0), // ⏰
        (0x23F3, 0x23F3), // ⏳
        (0x25FD, 0x25FE), // ◽ ◾
        (0x2614, 0x2615), // ☔ ☕
        (0x2630, 0x2637), // ☰ .. ☷ trigrams
        (0x2648, 0x2653), // ♈ .. ♓ zodiac
        (0x267F, 0x267F), // ♿
        (0x268A, 0x268F), // ⚊ .. ⚏ monograms / digrams
        (0x2693, 0x2693), // ⚓
        (0x26A1, 0x26A1), // ⚡
        (0x26AA, 0x26AB), // ⚪ ⚫
        (0x26BD, 0x26BE), // ⚽ ⚾
        (0x26C4, 0x26C5), // ⛄ ⛅
        (0x26CE, 0x26CE), // ⛎
        (0x26D4, 0x26D4), // ⛔
        (0x26EA, 0x26EA), // ⛪
        (0x26F2, 0x26F3), // ⛲ ⛳
        (0x26F5, 0x26F5), // ⛵
        (0x26FA, 0x26FA), // ⛺
        (0x26FD, 0x26FD), // ⛽
        (0x2705, 0x2705), // ✅
        (0x270A, 0x270B), // ✊ ✋
        (0x2728, 0x2728), // ✨
        (0x274C, 0x274C), // ❌
        (0x274E, 0x274E), // ❎
        (0x2753, 0x2755), // ❓ ❔ ❕
        (0x2757, 0x2757), // ❗
        (0x2795, 0x2797), // ➕ ➖ ➗
        (0x27B0, 0x27B0), // ➰
        (0x27BF, 0x27BF), // ➿
        (0x2B1B, 0x2B1C), // ⬛ ⬜
        (0x2B50, 0x2B50), // ⭐
        (0x2B55, 0x2B55), // ⭕
        (0x2E80, 0x2E99), // CJK Radicals Supplement
        (0x2E9B, 0x2EF3), // CJK Radicals Supplement (cont.)
        (0x2F00, 0x2FD5), // Kangxi Radicals
        (0x2FF0, 0x303E), // Ideographic Description .. CJK Symbols & Punctuation (incl. U+3000)
        (0x3041, 0x3096), // Hiragana
        (0x3099, 0x30FF), // kana voicing marks, Katakana
        (0x3105, 0x312F), // Bopomofo
        (0x3131, 0x318E), // Hangul Compatibility Jamo
        (0x3190, 0x31E5), // Kanbun, Bopomofo Ext, CJK Strokes
        (0x31EF, 0x321E), // Katakana Phonetic Ext, Enclosed CJK
        (0x3220, 0x3247), // Enclosed CJK (cont.)
        (0x3250, 0xA48C), // Enclosed CJK .. CJK Ext A .. CJK Unified .. Yi Syllables
        (0xA490, 0xA4C6), // Yi Radicals
        (0xA960, 0xA97C), // Hangul Jamo Extended-A
        (0xAC00, 0xD7A3), // Hangul Syllables
        (0xF900, 0xFAFF), // CJK Compatibility Ideographs
        (0xFE10, 0xFE19), // Vertical Forms
        (0xFE30, 0xFE52), // CJK Compatibility Forms, Small Form Variants
        (0xFE54, 0xFE66), // Small Form Variants (cont.)
        (0xFE68, 0xFE6B), // Small Form Variants (cont.)
        (0xFF01, 0xFF60), // Fullwidth Forms (excludes halfwidth katakana FF61-FFDC)
        (0xFFE0, 0xFFE6), // Fullwidth signs
    ];
    // Inclusive code-point ranges that even the fixed host mis-sizes, so they are unsafe on
    // every host version:
    //   * A curated set of EAW Ambiguous symbols that commonly appear in CJK-authored names.
    //     conhost with a CJK font renders them two cells (measured: ° × " … ← ※ ■ ○), while
    //     LengthInBufferCells counts one. Windows Terminal renders them one cell, so there
    //     this only over-hides. Letters are deliberately EXCLUDED so that accented Latin
    //     names (Ambiguous, but one cell on western terminals) still show.
    //   * BMP W code points that LengthInBufferCells' table (Unicode 5 era) lacks: the
    //     emoji / symbols in U+231A..U+2B55 and Hangul Jamo Extended-A.
    // Sorted ascending, non-overlapping -> binary search. Over-inclusion only hides a
    // name (safe); under-inclusion would let ']' wrap, so the table errs wide.
    private static readonly (int Lo, int Hi)[] HostMiscountedRanges =
    [
        (0x00B0, 0x00B1), // ° ±
        (0x00D7, 0x00D7), // × multiply
        (0x00F7, 0x00F7), // ÷ divide
        (0x2014, 0x2014), // — em dash
        (0x2018, 0x2019), // ' ' curly single quotes
        (0x201C, 0x201D), // " " curly double quotes
        (0x2025, 0x2026), // ‥ … ellipses
        (0x203B, 0x203B), // ※ reference mark
        (0x2103, 0x2103), // ℃
        (0x2109, 0x2109), // ℉
        (0x2116, 0x2116), // № numero
        (0x2190, 0x2199), // ← ↑ → ↓ ↔ ↕ ↖ ↗ ↘ ↙ arrows
        (0x231A, 0x231B), // ⌚ ⌛ (W, not in the host table)
        (0x23E9, 0x23EC), // ⏩ .. ⏬ (W, not in the host table)
        (0x23F0, 0x23F0), // ⏰ (W, not in the host table)
        (0x23F3, 0x23F3), // ⏳ (W, not in the host table)
        (0x25A0, 0x27BF), // geometric shapes, misc symbols, dingbats (conservative block)
        (0x2B1B, 0x2B1C), // ⬛ ⬜ (W, not in the host table)
        (0x2B50, 0x2B50), // ⭐ (W, not in the host table)
        (0x2B55, 0x2B55), // ⭕ (W, not in the host table)
        (0xA960, 0xA97C), // Hangul Jamo Extended-A (W, not in the host table)
    ];



    // Returns true if any code point in s is unsafe on a host that has not got the #21293 fix.
    public static bool ContainsWideChar(string? s) => FirstWideCharIndex(s) >= 0;

    // UTF-16 (char) offset of the first unsafe code point, or -1 if there is none. With
    // hostCountsWide (the host has the #21293 fix) only HostMiscountedRanges are unsafe;
    // otherwise WideRanges are too. Enumerates by Rune so surrogate-paired characters are
    // weighed once; the returned offset always lands on a rune boundary, so s[..offset] is
    // a valid substring of the safe prefix.
    public static int FirstWideCharIndex(string? s, bool hostCountsWide = false)
    {
        if (string.IsNullOrEmpty(s))
        {
            return -1;
        }

        int offset = 0;
        foreach (Rune rune in s.EnumerateRunes())
        {
            if (IsUnsafe(rune.Value, hostCountsWide))
            {
                return offset;
            }
            offset += rune.Utf16SequenceLength;
        }

        return -1;
    }

    // Returns s with every maximal run of unsafe code points (see FirstWideCharIndex)
    // replaced by "[N]" where N is the number of characters in that run, leaving the safe
    // segments intact. The result is therefore safe for the host while preserving the
    // readable parts and signalling how much was hidden where:
    // "請求書" -> "[3]", "Invoice請求Folder" -> "Invoice[2]Folder", "A請B求C" -> "A[1]B[1]C".
    // Returns the input unchanged when it is null/empty or already all-safe.
    public static string? CollapseWide(string? s, bool hostCountsWide = false)
    {
        if (string.IsNullOrEmpty(s) || FirstWideCharIndex(s, hostCountsWide) < 0)
        {
            return s;
        }

        var sb = new StringBuilder(s.Length);
        int runWidth = 0; // unsafe characters seen in the current run
        int offset = 0;
        foreach (Rune rune in s.EnumerateRunes())
        {
            int len = rune.Utf16SequenceLength;
            if (IsUnsafe(rune.Value, hostCountsWide))
            {
                runWidth++;
            }
            else
            {
                if (runWidth > 0)
                {
                    sb.Append('[').Append(runWidth).Append(']');
                    runWidth = 0;
                }
                sb.Append(s, offset, len);
            }
            offset += len;
        }
        if (runWidth > 0)
        {
            sb.Append('[').Append(runWidth).Append(']');
        }

        return sb.ToString();
    }

    private static bool IsUnsafe(int cp, bool hostCountsWide)
        => InRanges(HostMiscountedRanges, cp) || (!hostCountsWide && InRanges(WideRanges, cp));

    private static bool InRanges((int Lo, int Hi)[] ranges, int cp)
    {
        int lo = 0, hi = ranges.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            (int rLo, int rHi) = ranges[mid];
            if (cp < rLo)
            {
                hi = mid - 1;
            }
            else if (cp > rHi)
            {
                lo = mid + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }
}

// Decides whether the current host renders wide Write-Progress text correctly.
public static class ProgressRendering
{
    // The PowerShell version that first carries the #21293 fix (PR #26185, a ConsoleHost
    // change). It merged to master on 2026-06-26 and first shipped in v7.7.0-preview.3; it
    // was not backported to the 7.4 / 7.5 / 7.6 servicing lines, so a single >= threshold
    // is correct. Hosts at or above it show full wide names; older hosts get CollapseWide.
    //
    // Host.Version drops the prerelease label, so 7.7.0-preview.1/.2 (which lack the fix)
    // also read as 7.7.0 and are treated as fixed -- accepted, those previews are obsolete.
    // If the fix is ever backported to an older line, make this branch-aware.
    public static readonly Version? Pwsh26185FixedVersion = new(7, 7, 0);

    public static bool HostRendersWideProgress(Version? hostVersion)
        => Pwsh26185FixedVersion is { } fixedVersion
            && hostVersion is not null
            && hostVersion >= fixedVersion;
}

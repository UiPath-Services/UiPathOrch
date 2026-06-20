using System.Text;

namespace UiPath.PowerShell.Core;

// =============================================================================
// OrchProgressText.cs -- Width-safety helpers for Write-Progress StatusDescription
// =============================================================================
//
// PowerShell issue #21293: the console host sizes the progress bar by COUNTING
// CHARACTERS, not display cells. A character that occupies two cells (East Asian
// Wide / Fullwidth, and -- in CJK terminals -- many "Ambiguous" symbols) makes the
// rendered line longer than PowerShell computed, pushing the bar's trailing ']'
// onto the next line, where it then sticks. Fix tracked by PR #26185.
//
// Until that fix ships we must keep such characters OUT of StatusDescription. The
// rest of the record (the ASCII "index/total" prefix) is always safe. ProgressReporter
// consults these helpers and drops the name when the host can't render it safely.

// True/false: does a string contain any code point that the buggy renderer mis-sizes?
public static class EastAsianWidth
{
    // Inclusive code-point ranges treated as "not safe to show on a buggy host":
    //   * East Asian Wide (W) and Fullwidth (F) blocks  -- always two cells everywhere.
    //   * A curated set of symbols that are EAW Ambiguous but render two cells in CJK
    //     terminals AND commonly appear in CJK-authored names (e.g. U+203B '*'-mark,
    //     U+2026 ellipsis, fullwidth arrows). Letters are deliberately EXCLUDED so that
    //     accented Latin names (Ambiguous but one cell on western terminals) still show.
    // Sorted ascending, non-overlapping -> binary search. Over-inclusion only hides a
    // name (safe); under-inclusion would let ']' wrap (the bug we are avoiding), so the
    // table errs wide.
    private static readonly (int Lo, int Hi)[] WideRanges =
    [
        (0x000B0, 0x000B0), // ° degree
        (0x000B1, 0x000B1), // ± plus-minus
        (0x000D7, 0x000D7), // × multiply
        (0x000F7, 0x000F7), // ÷ divide
        (0x02014, 0x02014), // — em dash
        (0x02018, 0x02019), // ' ' curly single quotes
        (0x0201C, 0x0201D), // " " curly double quotes
        (0x02025, 0x02026), // ‥ … ellipses
        (0x02103, 0x02103), // ℃
        (0x02109, 0x02109), // ℉
        (0x02116, 0x02116), // № numero
        (0x02190, 0x02199), // ← ↑ → ↓ ↔ ↕ ↖ ↗ ↘ ↙ arrows
        (0x0203B, 0x0203B), // ※ reference mark
        (0x02329, 0x0232A), // 〈 〉 angle brackets (Wide)
        (0x025A0, 0x025FF), // geometric shapes ■ □ ● ○ ▲ △ ...
        (0x02600, 0x027BF), // misc symbols + dingbats (conservative block)
        (0x02E80, 0x0303E), // CJK Radicals .. CJK Symbols & Punctuation (incl. U+3000)
        (0x03041, 0x033FF), // Hiragana, Katakana, Hangul Compat Jamo, Enclosed CJK, ...
        (0x03400, 0x04DBF), // CJK Unified Ideographs Extension A
        (0x04E00, 0x09FFF), // CJK Unified Ideographs
        (0x0A000, 0x0A4CF), // Yi Syllables / Radicals
        (0x0A960, 0x0A97F), // Hangul Jamo Extended-A
        (0x0AC00, 0x0D7A3), // Hangul Syllables
        (0x0F900, 0x0FAFF), // CJK Compatibility Ideographs
        (0x0FE10, 0x0FE19), // Vertical Forms
        (0x0FE30, 0x0FE4F), // CJK Compatibility Forms
        (0x0FF01, 0x0FF60), // Fullwidth Forms (excludes halfwidth katakana FF61-FFDC)
        (0x0FFE0, 0x0FFE6), // Fullwidth signs
        (0x16FE0, 0x18AFF), // Ideographic symbols / Tangut
        (0x1B000, 0x1B2FF), // Kana Supplement / Extended
        (0x1F200, 0x1FAFF), // Enclosed Ideographic Supplement + emoji + symbols
        (0x20000, 0x3FFFD), // CJK Unified Ideographs Extensions B-G (supplementary)
    ];

    // Returns true if any code point in s would be mis-sized by the #21293 renderer.
    public static bool ContainsWideChar(string? s) => FirstWideCharIndex(s) >= 0;

    // UTF-16 (char) offset of the first code point that the buggy renderer mis-sizes, or
    // -1 if the string is entirely narrow. Enumerates by Rune so surrogate-paired
    // characters (emoji, CJK Ext B+) are weighed once; the returned offset always lands on
    // a rune boundary, so s[..offset] is a valid substring of the narrow prefix.
    public static int FirstWideCharIndex(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return -1;
        }

        int offset = 0;
        foreach (Rune rune in s.EnumerateRunes())
        {
            if (IsWide(rune.Value))
            {
                return offset;
            }
            offset += rune.Utf16SequenceLength;
        }

        return -1;
    }

    // Returns s with every maximal run of wide code points replaced by a single ASCII "...",
    // leaving the narrow (one-cell) segments intact. The result is therefore ENTIRELY narrow
    // -- safe for the #21293 renderer -- while preserving the readable ASCII parts of a mixed
    // name: "Invoice請求Folder" -> "Invoice...Folder", "請求書" -> "...", "A請B求C" -> "A...B...C".
    // Returns the input unchanged when it is null/empty or already all-narrow.
    public static string? CollapseWide(string? s)
    {
        if (string.IsNullOrEmpty(s) || FirstWideCharIndex(s) < 0)
        {
            return s;
        }

        var sb = new StringBuilder(s.Length);
        bool inWideRun = false;
        int offset = 0;
        foreach (Rune rune in s.EnumerateRunes())
        {
            int len = rune.Utf16SequenceLength;
            if (IsWide(rune.Value))
            {
                if (!inWideRun)
                {
                    sb.Append("...");
                    inWideRun = true;
                }
            }
            else
            {
                sb.Append(s, offset, len);
                inWideRun = false;
            }
            offset += len;
        }

        return sb.ToString();
    }

    private static bool IsWide(int cp)
    {
        int lo = 0, hi = WideRanges.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            (int rLo, int rHi) = WideRanges[mid];
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
    // The PowerShell version that first carries the #21293 fix (PR #26185). The PR is
    // still open, so no released build is known-good yet -> null means "treat every host
    // as buggy" and ProgressReporter suppresses wide names everywhere. Once the fix ships
    // and the version is known, set this (e.g. new Version(7, 6, 0)) and hosts at or above
    // it will show full names again.
    //
    // CAVEAT: if the fix is backported to multiple release lines (7.4.x AND 7.6.0) at
    // different version numbers, a single >= threshold is wrong across lines. Revisit this
    // and make it branch-aware when the PR actually merges.
    public static readonly Version? Pwsh26185FixedVersion = null;

    public static bool HostRendersWideProgress(Version? hostVersion)
        => Pwsh26185FixedVersion is { } fixedVersion
            && hostVersion is not null
            && hostVersion >= fixedVersion;
}

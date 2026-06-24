#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    MimeTypeHelper.GetMimeType maps a file extension to a MIME type; unknown
    extensions fall back to application/octet-stream. Import-OrchBucketItem uses
    this to set each uploaded bucket file's content-type, so a missing mapping
    means the file is stored as application/octet-stream.

.NOTES
    Pure unit test -- no Orchestrator tenant required.
    Run with: Invoke-Pester -Path Tests\MimeType.Tests.ps1 -Output Detailed
#>

Describe 'MimeTypeHelper.GetMimeType' {
    It 'maps <File> to <Expected>' -ForEach @(
        # Added in 1.10.0 (previously fell back to application/octet-stream)
        # Text / code / config
        @{ File = 'README.md';      Expected = 'text/markdown' }
        @{ File = 'NOTES.markdown'; Expected = 'text/markdown' }
        @{ File = 'config.yaml';    Expected = 'application/yaml' }
        @{ File = 'config.yml';     Expected = 'application/yaml' }
        @{ File = 'run.log';        Expected = 'text/plain' }
        @{ File = 'data.tsv';       Expected = 'text/tab-separated-values' }
        @{ File = 'app.ini';        Expected = 'text/plain' }
        @{ File = 'srv.conf';       Expected = 'text/plain' }
        @{ File = 'a.properties';   Expected = 'text/plain' }
        @{ File = 'settings.toml';  Expected = 'application/toml' }
        @{ File = 'query.sql';      Expected = 'application/sql' }
        @{ File = 'app.js';         Expected = 'text/javascript' }
        @{ File = 'module.mjs';     Expected = 'text/javascript' }
        @{ File = 'Main.xaml';      Expected = 'application/xaml+xml' }
        # Images
        @{ File = 'pic.jfif';       Expected = 'image/jpeg' }
        @{ File = 'image.avif';     Expected = 'image/avif' }
        @{ File = 'photo.heic';     Expected = 'image/heic' }
        @{ File = 'photo.heif';     Expected = 'image/heif' }
        # Audio / video
        @{ File = 'song.m4a';       Expected = 'audio/mp4' }
        @{ File = 'sound.aac';      Expected = 'audio/aac' }
        @{ File = 'track.ogg';      Expected = 'audio/ogg' }
        @{ File = 'voice.opus';     Expected = 'audio/ogg' }
        @{ File = 'old.wma';        Expected = 'audio/x-ms-wma' }
        @{ File = 'clip.webm';      Expected = 'video/webm' }
        @{ File = 'movie.mkv';      Expected = 'video/x-matroska' }
        @{ File = 'video.m4v';      Expected = 'video/mp4' }
        @{ File = 'old.mpeg';       Expected = 'video/mpeg' }
        @{ File = 'old.mpg';        Expected = 'video/mpeg' }
        @{ File = 'mobile.3gp';     Expected = 'video/3gpp' }
        # Archives
        @{ File = 'a.tgz';          Expected = 'application/gzip' }
        @{ File = 'a.bz2';          Expected = 'application/x-bzip2' }
        @{ File = 'a.xz';           Expected = 'application/x-xz' }
        # Documents
        @{ File = 'doc.odt';        Expected = 'application/vnd.oasis.opendocument.text' }
        @{ File = 'sheet.ods';      Expected = 'application/vnd.oasis.opendocument.spreadsheet' }
        @{ File = 'deck.odp';       Expected = 'application/vnd.oasis.opendocument.presentation' }
        @{ File = 'book.epub';      Expected = 'application/epub+zip' }
        # Fonts
        @{ File = 'f.woff';         Expected = 'font/woff' }
        @{ File = 'f.woff2';        Expected = 'font/woff2' }
        @{ File = 'f.ttf';          Expected = 'font/ttf' }
        @{ File = 'f.otf';          Expected = 'font/otf' }
        @{ File = 'f.eot';          Expected = 'application/vnd.ms-fontobject' }
        # Regression guards for pre-existing mappings
        @{ File = 'notes.txt';      Expected = 'text/plain' }
        @{ File = 'data.csv';       Expected = 'text/csv' }
        @{ File = 'data.json';      Expected = 'application/json' }
        @{ File = 'doc.pdf';        Expected = 'application/pdf' }
    ) {
        [MimeTypeHelper]::GetMimeType($File) | Should -Be $Expected
    }

    It 'is case-insensitive on the extension' {
        [MimeTypeHelper]::GetMimeType('README.MD') | Should -Be 'text/markdown'
    }

    It 'falls back to application/octet-stream for genuinely binary / unknown extensions' -ForEach @(
        @{ File = 'blob.weirdext' }
        @{ File = 'raw.bin' }
        @{ File = 'data.dat' }
        @{ File = 'pkg.nupkg' }
    ) {
        [MimeTypeHelper]::GetMimeType($File) | Should -Be 'application/octet-stream'
    }
}



public static class MimeTypeHelper
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Image files
        { ".gif",  "image/gif" },
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png",  "image/png" },
        { ".bmp",  "image/bmp" },
        { ".webp", "image/webp" },
        { ".svg",  "image/svg+xml" },
        { ".ico",  "image/x-icon" },
        { ".tiff", "image/tiff" },
        { ".tif",  "image/tiff" },
        { ".jfif", "image/jpeg" },
        { ".avif", "image/avif" },
        { ".heic", "image/heic" },
        { ".heif", "image/heif" },

        // Documents
        { ".pdf",  "application/pdf" },
        { ".doc",  "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls",  "application/vnd.ms-excel" },
        { ".xlm",  "application/vnd.ms-excel" },
        { ".xla",  "application/vnd.ms-excel" },
        { ".xlc",  "application/vnd.ms-excel" },
        { ".xlt",  "application/vnd.ms-excel" },
        { ".xlw",  "application/vnd.ms-excel" },
        { ".xlam", "application/vnd.ms-excel.addin.macroenabled.12" },
        { ".xlsb", "application/vnd.ms-excel.sheet.binary.macroenabled.12" },
        { ".xlsm", "application/vnd.ms-excel.sheet.macroenabled.12" },
        { ".xltm", "application/vnd.ms-excel.template.macroenabled.12" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt",  "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".odt",  "application/vnd.oasis.opendocument.text" },
        { ".ods",  "application/vnd.oasis.opendocument.spreadsheet" },
        { ".odp",  "application/vnd.oasis.opendocument.presentation" },
        { ".epub", "application/epub+zip" },

        // Text
        { ".txt",  "text/plain" },
        { ".html", "text/html" },
        { ".htm",  "text/html" },
        { ".css",  "text/css" },
        { ".js",   "text/javascript" },     // RFC 9239 (application/javascript is deprecated)
        { ".json", "application/json" },
        { ".xml",  "application/xml" },
        { ".csv",  "text/csv" },
        { ".md",       "text/markdown" },    // RFC 7763
        { ".markdown", "text/markdown" },
        { ".yaml",     "application/yaml" },  // RFC 9512
        { ".yml",      "application/yaml" },
        { ".log",      "text/plain" },
        { ".tsv",      "text/tab-separated-values" },
        { ".ini",      "text/plain" },
        { ".conf",     "text/plain" },
        { ".properties", "text/plain" },
        { ".toml",     "application/toml" },
        { ".sql",      "application/sql" },     // RFC 6922
        { ".mjs",      "text/javascript" },     // RFC 9239
        { ".xaml",     "application/xaml+xml" },
        { ".rtf",      "application/rtf" },

        // Archives
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".7z",  "application/x-7z-compressed" },
        { ".tar", "application/x-tar" },
        { ".gz",  "application/gzip" },
        { ".tgz", "application/gzip" },
        { ".bz2", "application/x-bzip2" },
        { ".xz",  "application/x-xz" },

        // Audio and video
        { ".mp3",  "audio/mpeg" },
        { ".wav",  "audio/wav" },
        { ".flac", "audio/flac" },
        { ".mp4",  "video/mp4" },
        { ".avi",  "video/x-msvideo" },
        { ".mov",  "video/quicktime" },
        { ".wmv",  "video/x-ms-wmv" },
        { ".flv",  "video/x-flv" },
        { ".m4a",  "audio/mp4" },
        { ".aac",  "audio/aac" },
        { ".ogg",  "audio/ogg" },
        { ".opus", "audio/ogg" },
        { ".wma",  "audio/x-ms-wma" },
        { ".webm", "video/webm" },
        { ".mkv",  "video/x-matroska" },
        { ".m4v",  "video/mp4" },
        { ".mpeg", "video/mpeg" },
        { ".mpg",  "video/mpeg" },
        { ".3gp",  "video/3gpp" },

        // Fonts
        { ".woff",  "font/woff" },
        { ".woff2", "font/woff2" },
        { ".ttf",   "font/ttf" },
        { ".otf",   "font/otf" },
        { ".eot",   "application/vnd.ms-fontobject" },

        // Other
        { ".exe", "application/x-msdownload" },
        //{ ".msi", "application/octet-stream" },
        { ".dll", "application/x-msdownload" }
    };

    public static string GetMimeType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "application/octet-stream";

        var extension = Path.GetExtension(fileName);

        return MimeTypes.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }
}

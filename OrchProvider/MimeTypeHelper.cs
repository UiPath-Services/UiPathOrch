

public static class MimeTypeHelper
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // 画像ファイル
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

        // ドキュメント
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

        // テキスト
        { ".txt",  "text/plain" },
        { ".html", "text/html" },
        { ".htm",  "text/html" },
        { ".css",  "text/css" },
        { ".js",   "application/javascript" },
        { ".json", "application/json" },
        { ".xml",  "application/xml" },
        { ".csv",  "text/csv" },
        { ".rtf",  "application/rtf" },

        // アーカイブ
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".7z",  "application/x-7z-compressed" },
        { ".tar", "application/x-tar" },
        { ".gz",  "application/gzip" },

        // 音声・動画
        { ".mp3",  "audio/mpeg" },
        { ".wav",  "audio/wav" },
        { ".flac", "audio/flac" },
        { ".mp4",  "video/mp4" },
        { ".avi",  "video/x-msvideo" },
        { ".mov",  "video/quicktime" },
        { ".wmv",  "video/x-ms-wmv" },
        { ".flv",  "video/x-flv" },

        // その他
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

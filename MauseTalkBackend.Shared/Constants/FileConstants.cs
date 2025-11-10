namespace MauseTalkBackend.Shared.Constants;

public static class FileConstants
{
    public static class Images
    {
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public static readonly string[] AllowedMimeTypes = 
        {
            "image/jpeg", 
            "image/png", 
            "image/gif", 
            "image/webp"
        };
        public const long MaxSizeBytes = 10 * 1024 * 1024; // 10MB
    }
    
    public static class Audio
    {
        public static readonly string[] AllowedExtensions = { ".mp3", ".wav", ".m4a", ".ogg", ".aac" };
        public static readonly string[] AllowedMimeTypes = 
        {
            "audio/mpeg", 
            "audio/wav", 
            "audio/mp4", 
            "audio/ogg",
            "audio/aac"
        };
        public const long MaxSizeBytes = 25 * 1024 * 1024; // 25MB
    }
    
    public static class General
    {
        public static readonly string[] AllowedExtensions = { ".pdf", ".txt", ".doc", ".docx" };
        public static readonly string[] AllowedMimeTypes = 
        {
            "application/pdf",
            "text/plain",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        public const long MaxSizeBytes = 50 * 1024 * 1024; // 50MB
    }
}
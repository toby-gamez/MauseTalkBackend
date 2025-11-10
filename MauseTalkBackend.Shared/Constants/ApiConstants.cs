namespace MauseTalkBackend.Shared.Constants;

public static class ApiConstants
{
    public const string ApiVersion = "v1";
    public const string ApiPrefix = $"/api/{ApiVersion}";
    
    public static class Routes
    {
        public const string Auth = "/auth";
        public const string Users = "/users";
        public const string Chats = "/chats";
        public const string Messages = "/messages";
        public const string Files = "/files";
        public const string Hub = "/hub";
    }
    
    public static class Policies
    {
        public const string RequireAuthenticated = "RequireAuthenticated";
        public const string RequireAdmin = "RequireAdmin";
    }
    
    public static class ClaimTypes
    {
        public const string UserId = "user_id";
        public const string Username = "username";
        public const string Email = "email";
    }
}
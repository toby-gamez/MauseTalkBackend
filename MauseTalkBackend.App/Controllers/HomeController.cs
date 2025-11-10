using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MauseTalkBackend.Shared.Models;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route("/")]
[AllowAnonymous] // Allow access without authentication
public class HomeController : ControllerBase
{
    [HttpGet("health")]
    public ActionResult<ApiResponse> Health()
    {
        return Ok(ApiResponse.SuccessResult("MauseTalkBackend API is running!"));
    }

    [HttpGet("api")]
    public ActionResult<object> ApiInfo()
    {
        return Ok(new
        {
            Name = "MauseTalkBackend API",
            Version = "1.0.0",
            Status = "Running",
            Timestamp = DateTime.UtcNow,
            Instructions = new
            {
                Step1 = "Register a new user at POST /api/v1/auth/register",
                Step2 = "Login at POST /api/v1/auth/login to get JWT token", 
                Step3 = "Copy the AccessToken from login response",
                Step4 = "In Swagger UI, click 'Authorize' button and paste: Bearer <your-token>",
                Step5 = "Now you can test all protected endpoints!"
            },
            Endpoints = new
            {
                Swagger = "/swagger",
                Auth = "/api/v1/auth",
                Users = "/api/v1/users", 
                Chats = "/api/v1/chats",
                Messages = "/api/v1/messages",
                Files = "/api/v1/files",
                SignalR = "/hub/chat"
            }
        });
    }
}
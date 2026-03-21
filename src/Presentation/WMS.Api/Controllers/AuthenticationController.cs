// ---- File: src/Presentation/WMS.Api/Controllers/AuthenticationController.cs ----
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WMS.Application.Features.Users.Commands;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Api.Controllers;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, UserRole Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token);

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;

    public AuthenticationController(UserManager<User> userManager, IConfiguration configuration, IMediator mediator)
    {
        _userManager = userManager;
        _configuration = configuration;
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet("hash")]
    public IActionResult GetPasswordHash([FromQuery] string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return BadRequest("Please provide a password in the query string, e.g., /api/Authentication/hash?password=YourPassword");
        }
        var hashedPassword = _userManager.PasswordHasher.HashPassword((User)null!, password);
        return Ok(hashedPassword);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = new User(request.Email, request.Email, request.FirstName, request.LastName, request.Role);
        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            return Ok(new { Message = "User created successfully" });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
        return BadRequest(ModelState);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { Message = "Invalid credentials" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { Message = "Account is blocked. Please contact administrator." });
        }

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponse(token));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { Message = "Profile updated successfully" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { Message = "Password changed successfully" });
    }

    private string GenerateJwtToken(User user)
    {
        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
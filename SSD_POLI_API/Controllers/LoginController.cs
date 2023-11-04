using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace SSD_POLI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public LoginController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string email, [FromQuery] string password)
        {
            var user = _context.LoginUser.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                try
                {
                    if (IsBase64String(user.Password))
                    {
                        byte[] storedSaltAndHash = Convert.FromBase64String(user.Password);
                        byte[] storedSalt = new byte[16];
                        byte[] storedHash = new byte[32];
                        Array.Copy(storedSaltAndHash, 0, storedSalt, 0, 16);
                        Array.Copy(storedSaltAndHash, 16, storedHash, 0, 32);

                        int iterations = 10000;
                        using (var pbkdf2 = new Rfc2898DeriveBytes(password, storedSalt, iterations))
                        {
                            byte[] newHash = pbkdf2.GetBytes(32);

                            if (newHash.SequenceEqual(storedHash))
                            {
                                var claims = new[]
                                {
                                    new Claim(ClaimTypes.Name, email),
                                };

                                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));

                                var tokenDescriptor = new SecurityTokenDescriptor
                                {
                                    Subject = new ClaimsIdentity(claims),
                                    Expires = DateTime.UtcNow.AddHours(1),
                                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
                                };

                                var tokenHandler = new JwtSecurityTokenHandler();
                                var token = tokenHandler.CreateToken(tokenDescriptor);

                                var tokenString = tokenHandler.WriteToken(token);

                                return Ok(new { Token = tokenString});
                            }
                            else
                            {
                                return NotFound("Invalid password.");
                            }
                        }
                    }
                    else
                    {
                        return BadRequest("Invalid password format in the database.");
                    }
                }
                catch (FormatException ex)
                {
                    return BadRequest($"Error: Invalid Base64 format for stored password. {ex.Message}");
                }
            }

            return NotFound("User not found.");
        }

        private bool IsBase64String(string base64)
        {
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        [HttpGet("/api/CheckUserIfAdmin")]
        public IActionResult GetIsAdmin()
        {

            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {

                return Unauthorized("No token provided.");
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);


                var email = principal.Identity.Name;


                var user = _context.LoginUser.FirstOrDefault(u => u.Email == email);

                if (user != null)
                {
                    return Ok(user.isAdmin);
                }
                else
                {
                    return NotFound("User not found.");
                }
            }
            catch (Exception ex)
            {

                return BadRequest($"JWT validation failed: {ex.Message}");
            }
        }


        [HttpPost]
        public IActionResult Post([FromQuery] string email, [FromQuery] string password)
        {
            try
            {
                byte[] salt = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }


                int iterations = 10000;
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    byte[] hash = pbkdf2.GetBytes(32);
                    byte[] saltAndHash = new byte[48];
                    Array.Copy(salt, 0, saltAndHash, 0, 16);
                    Array.Copy(hash, 0, saltAndHash, 16, 32);

                    var newUser = new LoginModel
                    {
                        Email = email,
                        Password = Convert.ToBase64String(saltAndHash)
                    };

                    _context.LoginUser.Add(newUser);
                    _context.SaveChanges();

                    return Ok("User created successfully.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

    }
}

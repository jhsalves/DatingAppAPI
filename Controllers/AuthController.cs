using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dto;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        public readonly IAuthRepository _repository;
        public readonly IConfiguration _configuration;
        public AuthController(IAuthRepository authRepository, IConfiguration configuration)
        {
            _repository = authRepository;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody]UserForRegisterDto userForRegister){
            
            if(!String.IsNullOrEmpty(userForRegister.Username))
                userForRegister.Username = userForRegister.Username.ToLower();
            
            if(await _repository.UserExists(userForRegister.Username)){
                ModelState.AddModelError("Username","Username already exists.");
            }
            
            if(!ModelState.IsValid){
                return BadRequest(ModelState);
            }

            var userToCreate = new User{
                Username = userForRegister.Username
            };

            var createUser = await _repository.Register(userToCreate,userForRegister.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] UserForLoginDto userForLogin){

            var userFromRepo = await _repository.Login(userForLogin.Username,userForLogin.Password);
            
            if(userFromRepo == null){
                return Unauthorized();
            }
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("AppSettings:Token").Value);
            var tokenDescriptor = new SecurityTokenDescriptor{
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name,userFromRepo.Username)
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return Ok(new {tokenString});
        }
    }
}
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dto;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public UsersController(IDatingRepository datingRepository, IMapper mapper)
        {
            _repo = datingRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult> GetUsers(){
            var users = await _repo.GetUsers();
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);
            return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser(int id){
            var user = await _repo.GetUser(id);
            var userToReturn = _mapper.Map<UserForDetailDto>(user);
            return Ok(userToReturn);
        }

        //api/users/1 PUT
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id,[FromBody] UserForUpdateDto userForUpdateDto){
            if(!ModelState.IsValid){
                return BadRequest(ModelState);
            }

            var userClaims = User.FindFirst(ClaimTypes.NameIdentifier);

            var currentUser = int.Parse(userClaims.Value);
            
            var userFromRepo = await _repo.GetUser(id);

            if(userFromRepo == null)
                return NotFound($"Coud not find user with ID of {id}");
            
            if(currentUser != userFromRepo.Id){
                return Unauthorized();
            }

            _mapper.Map(userForUpdateDto,userFromRepo);

            if(await _repo.SaveAll())
                return NoContent();

            throw new Exception($"Updating user {id} failed on save");
        }
    }
}
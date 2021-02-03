using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _mapper = mapper;
            _userRepository = userRepository;

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers() //IEnumerable jis the same as list - list has more functions
        {
            var users = await _userRepository.GetMembersAsync();
            return Ok(users);
        }

        //api/users/3
        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username) //IEnumerable is the same as list - list has more functions
        {
            return await _userRepository.GetMemberAsync(username); 
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;  // It gets us the username token that the API uses to auth. the users -> This is the user we will be updating 
            var user = await _userRepository.GetUserByUsernameAsync(username);

            _mapper.Map(memberUpdateDto, user); // Automatically update properties in user;  example --> (user.City = memberUpdateDto.City) 

            _userRepository.Update(user); //This guarantees to not cause exceptions, even if we update any propertiees or not

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }



    }
}
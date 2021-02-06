using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _photoService = photoService;
            _mapper = mapper;
            _userRepository = userRepository;

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery]UserParams userParams) //IEnumerable is the same as list - list has more functions
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            userParams.CurrentUsername = user.UserName;

            if (string.IsNullOrEmpty(userParams.Gender))
                userParams.Gender = user.Gender == "male" ? "female": "male";

            var users = await _userRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        //api/users/3
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username) //IEnumerable is the same as list - list has more functions
        {
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername()); // It gets us the username token that the API uses to auth. the users -> This is the user we will be updating; Then gets the user by its username

            _mapper.Map(memberUpdateDto, user); // Automatically update properties in user;  example --> (user.City = memberUpdateDto.City) 

            _userRepository.Update(user); //This guarantees to not cause exceptions, even if we update any propertiees or not

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            // Method adds photo to database and saves it. If user has no photos, it sets this photo as main
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message); // If result is null then we have an error --> returns badrequest
            
            var photo = new Photo // We create a new photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) // If they dont have any photo, then we want to set it to main 
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);  // Add the photo

            if (await _userRepository.SaveAllAsync()) 
            {
                return CreatedAtRoute("GetUser", new {Username = user.UserName}, _mapper.Map<PhotoDto>(photo)); // we return the photo if True
            }
               
            
            return BadRequest("Problem adding photo"); // else we return BadRequest
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            // Method sets photo as main through photoId
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Faileed to set main photo");

        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo == null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await _userRepository.SaveAllAsync()) return Ok();

            return BadRequest("Failed to delete the photo");

        }



    }
}
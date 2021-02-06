using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
        {
            _mapper = mapper;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {

            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken"); //Checks if username already exists

            var user = _mapper.Map<AppUser>(registerDto);

            using var hmac = new HMACSHA512();

            // We generate password hash and salt from password and store it to user variable
            user.UserName = registerDto.Username.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
            user.PasswordSalt = hmac.Key;
           


            _context.Users.Add(user); // Add user to context - database
            await _context.SaveChangesAsync(); // Save user to database

            return new UserDto
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                KnowAs = user.KnownAs,
                Gender = user.Gender
            }; // Return registered user
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users.Include(p => p.Photos).SingleOrDefaultAsync(x => x.UserName == loginDto.Username); // Check for username in database and gets user

            if (user == null) return Unauthorized("Invalid username"); // If user not found returns error

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++) // Checks if password hash fits password save in database
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }

            return new UserDto
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnowAs = user.KnownAs,
                Gender = user.Gender
            }; // We return the user
        }


        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower()); // We return any user found in database
        }


    }
}
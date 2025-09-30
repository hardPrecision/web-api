using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private IUserRepository _userRepository;
    private IMapper _mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }
    
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null) 
            return NotFound();
        
        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreateDto user)
    {
        if (user is null)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }
        
        var createdUser = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(createdUser);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UserUpdateDto userInfo, [FromRoute] Guid userId)
    {
        if (userInfo is null)
        {
            return BadRequest();
        }
        if (userInfo.Login is null || userInfo.FirstName is null || userInfo.LastName is null)
        {
            return UnprocessableEntity(ModelState);
        }
        userInfo.Id = userId;
        var isInserted = false;
        var user = _mapper.Map<UserEntity>(userInfo);
        try
        {
            _userRepository.UpdateOrInsert(user, out isInserted);
        }
        catch (Exception ex) 
        {
            return BadRequest();
        }
        if (!ModelState.IsValid) 
        {
            return UnprocessableEntity(ModelState);
        }
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }
        return NoContent();
    }
}
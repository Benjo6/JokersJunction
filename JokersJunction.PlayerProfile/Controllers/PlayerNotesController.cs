using AutoMapper;
using JokersJunction.PlayerProfile.Repositories.Interfaces;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.PlayerProfile.Controllers
{
    [Route("api/player-note")]
    //[Authorize(Roles = "User")]
    [ApiController]
    public class PlayerNoteController : ControllerBase
    {
        private readonly IPlayerNotesRepository _playerNotesRepository;
        private readonly IMapper _mapper;

        public PlayerNoteController(IPlayerNotesRepository playerNotesRepository, IMapper mapper)
        {
            _playerNotesRepository = playerNotesRepository;
            _mapper = mapper;
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(GetNotesResult), 200)] // Specify the response type
        public async Task<IActionResult> GetNotes(string userId)
        {
            try
            {
                return Ok(await _playerNotesRepository.GetAsync(userId));
            }
            catch (Exception ex)
            {
                return BadRequest(new GetNotesResult
                {
                    Successful = false,
                    Error = $"Error processing request: {ex}"
                });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateNoteResult), 201)] // Specify the response type
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteModel model)
        {
            try
            {
                var createdNote = await _playerNotesRepository.AddAsync(model);
                return CreatedAtAction(nameof(CreateNote), createdNote);
            }
            catch (Exception ex)
            {
                return BadRequest(new CreateNoteResult
                {
                    Successful = false,
                    Errors = new List<string> { $"Unexpected error occurred. {ex}" }
                });
            }
        }

        [HttpDelete("{userId}/{notedPlayerName}")]
        [ProducesResponseType(typeof(DeleteTableResult), 200)] // Specify the response type
        public async Task<IActionResult> DeleteNote(string userId, string notedPlayerName)
        {
            try
            {
                return Ok(_mapper.Map<DeleteTableResult>(await _playerNotesRepository.DeleteAsync(userId, notedPlayerName)));
            }
            catch (Exception ex)
            {
                return BadRequest(new DeleteTableResult
                {
                    Successful = false,
                    Error = $"Error processing request: {ex}"
                });
            }
        }

        [HttpGet("by-name/{userId}/{notePlayerName}")]
        [ProducesResponseType(typeof(PlayerNote), 200)] // Specify the response type
        public async Task<IActionResult> GetNoteByName(string userId, string notePlayerName)
        {
            try
            {
                var note = await _playerNotesRepository.GetByNameAsync(userId, notePlayerName);
                if (note == null)
                    return NotFound(); // Return 404 if note not found
                return Ok(note);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(PlayerNote), 200)] // Specify the response type
        public async Task<IActionResult> UpdateNote([FromBody] PlayerNote? model)
        {
            try
            {
                var updatedNote = await _playerNotesRepository.UpdateAsync(model);
                if (updatedNote == null)
                    return NotFound(); // Return 404 if note not found
                return Ok(updatedNote);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }
    }
}
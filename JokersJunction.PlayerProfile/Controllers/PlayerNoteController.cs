using AutoMapper;
using JokersJunction.PlayerProfile.Repositories.Interfaces;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.PlayerProfile.Controllers
{
    [Route("api/player-note")]
    [Authorize(Roles = "User")]
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

        [HttpGet]
        [ProducesResponseType(typeof(GetNotesResult), 200)] // Specify the response type
        public async Task<ActionResult<GetNotesResult>> GetNotes(string userId)
        {
            try
            {
                return Ok(await _playerNotesRepository.GetAsync(userId));
            }
            catch (Exception ex)
            {
                return new GetNotesResult
                {
                    Successful = false,
                    Error = $"Error processing request: {ex}"
                };
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateNoteResult), 201)] // Specify the response type
        public async Task<ActionResult<CreateNoteResult>> CreateNote([FromBody] CreatePlayerNote? model)
        {
            try
            {
                var createdNote = await _playerNotesRepository.AddAsync(model);
                return CreatedAtAction(nameof(CreateNote), createdNote);
            }
            catch (Exception ex)
            {
                return new CreateNoteResult
                {
                    Successful = false,
                    Errors = new List<string> { $"Unexpected error occurred. {ex}" }
                };
            }
        }

        [HttpPost("{userId}/{notePlayerName}")]
        [ProducesResponseType(typeof(DeleteTableResult), 200)] // Specify the response type
        public async Task<ActionResult<DeleteTableResult>> DeleteNote(string userId, string notedPlayerName)
        {
            try
            {
                return _mapper.Map<DeleteTableResult>(await _playerNotesRepository.DeleteAsync(userId, notedPlayerName));
            }
            catch (Exception ex)
            {
                return new DeleteTableResult
                {
                    Successful = false,
                    Error = $"Error processing request: {ex}"
                };
            }
        }

        [HttpGet("{userId}/{notePlayerName}")]
        [ProducesResponseType(typeof(PlayerNote), 200)] // Specify the response type
        public async Task<ActionResult<PlayerNote>> GetNoteByName(string userId, string notePlayerName)
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
        public async Task<ActionResult<PlayerNote>> UpdateNote([FromBody] PlayerNote? model)
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

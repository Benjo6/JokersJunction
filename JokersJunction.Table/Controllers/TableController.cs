using AutoMapper;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Shared.Requests;
using JokersJunction.Shared.Responses;
using JokersJunction.Table.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Table.Controllers
{
    [Route("api/table")]
    [ApiController]
    public class TableController : ControllerBase
    {
        private readonly ITableRepository _tableRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TableController> _logger;

        public TableController(ITableRepository tableRepository, IMapper mapper, ILogger<TableController> logger)
        {
            _tableRepository = tableRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<GetTablesResult>> GetTables()
        {
            try
            {
                var pokerTables = await _tableRepository.GetTables();
                return new GetTablesResult { Successful = true, PokerTables = pokerTables };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching poker tables.");
                return new GetTablesResult { Successful = false, Error = "Error processing request" };
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<CreateTableResponse>> Create([FromBody] CreateTableRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return new CreateTableResponse
                    {
                        Successful = false,
                        Errors = new List<string> { "Invalid table model" }
                    };
                }

                var existingTable = await _tableRepository.GetTableByName(request.Name);
                if (existingTable != null)
                {
                    return Ok(new CreateTableResponse
                    {
                        Successful = false,
                        Errors = new List<string> { "Table with this name already exists" }
                    });
                }

                await _tableRepository.AddTable(_mapper.Map<PokerTable>(request));

                return Ok(new CreateTableResponse { Successful = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating poker table.");
                return new CreateTableResponse
                {
                    Successful = false,
                    Errors = new List<string> { "Unexpected error occurred. Try again or contact support" }
                };
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PokerTable>> GetTableById(int id)
        {
            try
            {
                var table = await _tableRepository.GetTableById(id);

                return table;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching poker table with ID {id}.");
                return NotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public async Task<ActionResult<DeleteTableResult>> DeleteTable([FromBody] int tableId)
        {
            try
            {
                await _tableRepository.DeleteTable(tableId);
                return new DeleteTableResult { Successful = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting poker table with ID {tableId}.");
                return new DeleteTableResult { Successful = false, Error = "Error processing request" };
            }
        }
    }
}

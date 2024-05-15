using AutoMapper;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Table.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace JokersJunction.Table.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TableController : ControllerBase
{
    private readonly ITableRepository _tableRepository;
    private readonly ILogger<TableController> _logger;

    public TableController(ITableRepository tableRepository, ILogger<TableController> logger)
    {
        _tableRepository = tableRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTables()
    {
        try
        {
            var pokerTables = await _tableRepository.GetTables();
            var mappedPokerTables = pokerTables.Select(pt => new PokerTable
            {
                Id = pt.Id.ToString(),
                Name = pt.Name,
                MaxPlayers = pt.MaxPlayers,
                SmallBlind = pt.SmallBlind
            }).ToList();

            return Ok(mappedPokerTables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching poker tables.");
            return BadRequest();
        }
    }

    //[Authorize(Roles = "Admin")]
    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateTableModel? request)
    {
        try
        {
            if (request == null)
            {
                return NotFound(new CreateTableResult
                {
                    Successful = false,
                    Errors = new List<string> { "Invalid table model" }
                });
            }

            var existingTable = await _tableRepository.GetTableByName(request.Name);
            if (existingTable != null)
            {
                return Ok(new CreateTableResult
                {
                    Successful = false,
                    Errors = new List<string> { "Table with this name already exists" }
                });
            }

            var newTable = new Common.Databases.Models.PokerTable
            {
                Name = request.Name,
                MaxPlayers = request.MaxPlayers,
                SmallBlind = request.SmallBlind
            };

            await _tableRepository.AddTable(newTable);

            return Ok(new CreateTableResult { Successful = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating poker table.");
            return BadRequest(new CreateTableResult()
            {
                Successful = false,
                Errors = new List<string> { "Unexpected error occurred. Try again or contact support" }
            });
        }
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetTableById(string id)
    {
        try
        {
            var table = await _tableRepository.GetTableById(id);

            return Ok(table);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching poker table with ID {id}.");
            return NotFound();
        }
    }

    //[Authorize(Roles = "Admin")]
    [HttpDelete("delete/{tableId}")]
    public async Task<IActionResult> DeleteTable(string tableId)
    {
        try
        {
            await _tableRepository.DeleteTable(tableId);
            return Ok(new DeleteTableResult { Successful = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting poker table with ID {tableId}.");
            return BadRequest(new DeleteTableResult { Successful = false, Error = "Error processing request" });
        }
    }
}
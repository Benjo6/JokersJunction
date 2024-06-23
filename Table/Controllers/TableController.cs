using JokersJunction.Server;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using JokersJunction.Table.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace JokersJunction.Table.Controllers;

[Route("api/")]
[ApiController]
public class TableController : ControllerBase
{
    private readonly ITableRepository _tableRepository;

    public TableController(ITableRepository tableRepository)
    {
        _tableRepository = tableRepository;
    }

    [HttpGet("p-table")]
    public async Task<ActionResult<GetPokerTablesResult>> GetPokerTables()
    {
        try
        {
            return (new GetPokerTablesResult
            {
                Successful = true,
                Tables = await _tableRepository.GetPokerTables()
            });
        }
        catch (Exception)
        {
            return (new GetPokerTablesResult
            {
                Successful = false,
                Error = "Error processing request"
            });
        }
    }

    [HttpGet("b-table")]
    public async Task<ActionResult<GetBlackjackTablesResult>> GetBlackjackTables()
    {
        try
        {
            return (new GetBlackjackTablesResult
            {
                Successful = true,
                Tables = await _tableRepository.GetBlackjackTables()
            });
        }
        catch (Exception)
        {
            return (new GetBlackjackTablesResult
            {
                Successful = false,
                Error = "Error processing request"
            });
        }
    }

    //[Authorize(Roles = "Admin")]
    [HttpPost("p-table")]
    public async Task<ActionResult<CreateTableResult>> CreatePoker([FromBody] CreateTableModel model)
    {
        try
        {
            if (model == null)
            {
                return new CreateTableResult
                {
                    Successful = false,
                    Errors = new List<string>() { "Invalid table model" }
                };
            }

            var table = await _tableRepository.GetPokerTableByName(model.Name);

            if (table != null)
            {
                return Ok(new CreateTableResult
                {
                    Successful = false,
                    Errors = new List<string>() { "Table with this name already exists" }
                });
            }

            await _tableRepository.AddPokerTable(new PokerTable()
            {
                MaxPlayers = model.MaxPlayers,
                Name = model.Name,
                SmallBlind = model.SmallBlind
            });

            return Ok(new CreateTableResult
            {
                Successful = true
            });
        }
        catch (Exception)
        {
            return new CreateTableResult
            {
                Successful = false,
                Errors = new List<string>() { "Unexpected error occured. Try again or contact support" }
            };
        }
    }

    [HttpPost("b-table")]
    public async Task<ActionResult<CreateTableResult>> CreateBlackjack([FromBody] CreateTableModel model)
    {
        try
        {
            if (model == null)
            {
                return new CreateTableResult
                {
                    Successful = false,
                    Errors = new List<string>() { "Invalid table model" }
                };
            }

            var table = await _tableRepository.GetBlackjackTableByName(model.Name);

            if (table != null)
            {
                return Ok(new CreateTableResult
                {
                    Successful = false,
                    Errors = new List<string>() { "Table with this name already exists" }
                });
            }

            await _tableRepository.AddBlackjackTable(new BlackjackTable()
            {
                MaxPlayers = model.MaxPlayers,
                Name = model.Name
            });

            return Ok(new CreateTableResult
            {
                Successful = true
            });
        }
        catch (Exception)
        {
            return new CreateTableResult
            {
                Successful = false,
                Errors = new List<string>() { "Unexpected error occured. Try again or contact support" }
            };
        }
    }

    [HttpGet("p-table/{id:int}")]
    public async Task<ActionResult<PokerTable>> GetPokerTableById(int id)
    {
        try
        {
            return await _tableRepository.GetPokerTableById(id) ?? throw new InvalidOperationException();
        }
        catch (Exception)
        {
            return NotFound();
            // ignored
        }
    }

    [HttpGet("b-table/{id:int}")]
    public async Task<ActionResult<BlackjackTable>> GetBlackjackTableById(int id)
    {
        try
        {
            return await _tableRepository.GetBlackjackTableById(id) ?? throw new InvalidOperationException();
        }
        catch (Exception)
        {
            return NotFound();
            // ignored
        }
    }

    //[Authorize(Roles = "Admin")]
    [HttpPost("p-table/delete")]
    public async Task<ActionResult<DeleteTableResult>> DeletePokerTable([FromBody] int tableId)
    {
        try
        {
            await _tableRepository.DeletePokerTable(tableId);
            return (new DeleteTableResult
            {
                Successful = true
            });

        }
        catch (Exception)
        {
            return (new DeleteTableResult
            {
                Successful = false,
                Error = "Error processing request"
            });
        }
    }

    [HttpPost("´b-table/delete")]
    public async Task<ActionResult<DeleteTableResult>> DeleteBlackjackTable([FromBody] int tableId)
    {
        try
        {
            await _tableRepository.DeleteBlackjackTable(tableId);
            return (new DeleteTableResult
            {
                Successful = true
            });

        }
        catch (Exception)
        {
            return (new DeleteTableResult
            {
                Successful = false,
                Error = "Error processing request"
            });
        }
    }

}
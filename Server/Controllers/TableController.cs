using AutoMapper;
using JokersJunction.Server.Repositories.Contracts;
using JokersJunction.Shared;
using JokersJunction.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableController : ControllerBase
    {
        private readonly ITableRepository _tableRepository;
        private readonly IMapper _mapper;

        public TableController(ITableRepository tableRepository,
            IMapper mapper)
        {
            _tableRepository = tableRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<GetTablesResult<T>>> GetTables<T>() where T : Table
        {
            try
            {
                return (new GetTablesResult<T>
                {
                    Successful = true,
                    Tables = await _tableRepository.GetTables<T>()
                });
            }
            catch (Exception)
            {
                return (new GetTablesResult<T>
                {
                    Successful = false,
                    Error = "Error processing request"
                });
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<CreateTableResult>> Create<T>([FromBody] CreateTableModel model) where T : Table
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

                var table = await _tableRepository.GetTableByName<T>(model.Name);

                if (table != null)
                {
                    return Ok(new CreateTableResult
                    {
                        Successful = false,
                        Errors = new List<string>() { "Table with this name already exists" }
                    });
                }

                await _tableRepository.AddTable(_mapper.Map<PokerTable>(model));

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

        [HttpGet("{id:int}")]
        public async Task<ActionResult<T>> GetTableById<T>(int id) where T : Table
        {
            try
            {
                return await _tableRepository.GetTableById<T>(id);
            }
            catch (Exception)
            {
                return NotFound();
                // ignored
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost("delete")]
        public async Task<ActionResult<DeleteTableResult>> DeleteTable<T>([FromBody]int tableId) where T: Table
        {
            try
            {
                await _tableRepository.DeleteTable<T>(tableId);
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
}

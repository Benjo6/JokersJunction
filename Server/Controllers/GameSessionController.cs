using JokersJunction.Server.Repositories.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameSessionController : ControllerBase
    {
        private readonly IGameSessionRepository _gameSessionRepository;

        public GameSessionController(IGameSessionRepository gameSessionRepository)
        {
            _gameSessionRepository = gameSessionRepository;
        }
    }
}

using BattleSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace BattleSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StartController : ControllerBase
    {
        private readonly IBattleSimulator _simulator;
        public StartController(IBattleSimulator simulator)
        {
            _simulator = simulator;
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (!_simulator.StartSimulation())
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "The battle simulation could not start.",
                    Detail = "Battle simulation must have enough contenders and only one battle may be simulated at a time."
                };
                return BadRequest(problemDetails);
            }
            return Ok();
        }
    }
}
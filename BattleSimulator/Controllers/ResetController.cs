using BattleSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace BattleSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetController : ControllerBase
    {
        private readonly IBattleSimulator _simulator;
        public ResetController(IBattleSimulator simulator)
        {
            _simulator = simulator;
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (!_simulator.ResetSimulation())
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "The battle simulation could not reset.",
                    Detail = "Battle simulation must be started and not completed to be able to reset."
                };
                return BadRequest(problemDetails);
            }
            return Ok();
        }
    }
}
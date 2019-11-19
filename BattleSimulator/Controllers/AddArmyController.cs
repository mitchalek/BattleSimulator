using BattleSimulator.Models;
using BattleSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace BattleSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddArmyController : ControllerBase
    {
        private readonly IBattleSimulator _simulator;
        public AddArmyController(IBattleSimulator simulator)
        {
            _simulator = simulator;
        }

        [HttpGet]
        public IActionResult Get([FromQuery][Bind("Name,Units,Strategy")] Army army)
        {
            if (!_simulator.AddArmy(army))
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "The army could not join the battle for simulation.",
                    Detail = "Armies cannot join the battle that is being simulated or have a contender with the same name."
                };
                return BadRequest(problemDetails);
            }
            return Ok();
        }
    }
}
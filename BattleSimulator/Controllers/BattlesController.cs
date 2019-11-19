using BattleSimulator.Models;
using BattleSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;

namespace BattleSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BattlesController : ControllerBase
    {
        private readonly IBattleSimulator _simulator;
        public BattlesController(IBattleSimulator simulator)
        {
            _simulator = simulator;
        }

        // GET api/battles
        [HttpGet]
        public ActionResult<IEnumerable<Battle>> Get()
        {
            return Ok(_simulator.GetBattles());
        }

        // GET api/battles/5
        [HttpGet("{id}")]
        public ActionResult<Battle> Get(int id)
        {
            var battle = _simulator.GetBattle(id);
            if (battle == null)
            {
                return NotFound();
            }
            return Ok(battle);
        }
    }
}
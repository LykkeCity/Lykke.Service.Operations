using System;
using System.Threading.Tasks;
using Common;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Operations.Controllers
{
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IOperationsRepository _operationsRepository;

        public OperationsController(IOperationsRepository operationsRepository)
        {
            _operationsRepository = operationsRepository;
        }

        [HttpGet]        
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            var operation = await _operationsRepository.Get(id.Value);

            if (operation == null)
                return NotFound(new { message = "Operation not found" });

            return Ok(new
            {
                Created = operation.Created.ToIsoDateTime(),
                Type = operation.Type.ToString(),
                Status = operation.Status.ToString(),
                operation.ClientId,
                Context = new
                {
                    operation.AssetId,
                    operation.Amount,
                    operation.WalletId
                }
            });
        }
        
        [HttpPost]
        [Route("transfer/{id}")]
        public async Task<IActionResult> Transfer([FromBody]CreateTransferCommand cmd, Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var operation = await _operationsRepository.Get(id.Value);

            if (operation != null)
                return BadRequest(new { message = "Operation with the id already exists." });

            await _operationsRepository.CreateTransfer(id.Value, cmd.ClientId, cmd.AssetId, cmd.Amount, cmd.WalletId);
            
            return Created(Url.Action("Get", new { id }), id);
        }                
    }
}

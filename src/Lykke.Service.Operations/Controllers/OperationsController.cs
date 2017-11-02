using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
        [ProducesResponseType(typeof(OperationModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            var operation = await _operationsRepository.Get(id.Value);

            if (operation == null)
                return NotFound(new { message = "Operation not found" });

            return Ok(new OperationModel
            {
                Id = id.Value,
                Created = operation.Created,
                Type = operation.Type,
                Status = operation.Status,
                ClientId = operation.ClientId,
                Context = JObject.FromObject(new
                {
                    operation.AssetId,
                    operation.Amount,
                    operation.WalletId
                })
            });
        }
        
        [HttpGet]
        [Route("list/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OperationModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(OperationStatus status)
        {
            var operations = await _operationsRepository.Get(status);

            var result = operations.Select(o => new OperationModel
            {
                Id = o.Id,
                Created = o.Created,
                Type = o.Type,
                Status = o.Status,
                ClientId = o.ClientId,
                Context = JObject.FromObject(new
                {
                    o.AssetId,
                    o.Amount,
                    o.WalletId
                })
            });

            return Ok(result);
        }
        
        [HttpPost]
        [Route("transfer/{id}")]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Guid?), (int)HttpStatusCode.Created)]
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

        [HttpPost]
        [Route("cancel/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Cancel(Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            var operation = await _operationsRepository.Get(id.Value);

            if (operation == null)
                return NotFound();

            if (operation.Status != OperationStatus.Created)
                return BadRequest(new { message = "An operation in created status could be canceled" });

            await _operationsRepository.Cancel(id.Value);

            return Ok();
        }
    }
}

using BLL.Services.Interfaces.Common;
using DAL.Models.Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_API.Controllers;

[Authorize]
[ApiController]
public abstract class CrudControllerBase<TEntity> : ControllerBase where TEntity : BaseEntity
{
    private readonly ICrudService<TEntity> _service;

    protected CrudControllerBase(ICrudService<TEntity> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<TEntity>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TEntity>> GetById(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<TEntity>> Create([FromBody] TEntity entity)
    {
        var created = await _service.CreateAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TEntity>> Update(Guid id, [FromBody] TEntity entity)
    {
        var updated = await _service.UpdateAsync(id, entity);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}

using faturamento_api.Data;
using faturamento_api.Dtos;
using faturamento_api.Entities;
using faturamento_api.Integrations.Estoque;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace faturamento_api.Controllers;

[ApiController]
[Route("api/notas")]
public class NotasController : ControllerBase
{
    private readonly FaturamentoDbContext _context;
    private readonly EstoqueApiClient _estoqueApiClient;
    private readonly IMemoryCache _memoryCache;

    public NotasController(
        FaturamentoDbContext context,
        EstoqueApiClient estoqueApiClient,
        IMemoryCache memoryCache)
    {
        _context = context;
        _estoqueApiClient = estoqueApiClient;
        _memoryCache = memoryCache;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Nota>>> GetAll()
    {
        var notas = await _context.Notas
            .Include(i => i.Items)
            .OrderBy(i => i.Id)
            .ToListAsync();

        return Ok(notas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Nota>> GetById(int id)
    {
        var nota = await _context.Notas
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (nota is null)
            return NotFound(new { message = "Nota fiscal não encontrada." });

        return Ok(nota);
    }

    [HttpGet("produtos/{productId:int}/vinculo")]
    public async Task<ActionResult> CheckProductLink(int productId)
    {
        var vinculado = await _context.Notas
            .Include(n => n.Items)
            .AnyAsync(n => n.Items.Any(i => i.ProductId == productId));

        if (!vinculado)
        {
            return Ok(new
            {
                linked = false,
                message = "Produto sem vínculo com notas fiscais."
            });
        }

        var quantidadeNotas = await _context.Notas
            .Include(n => n.Items)
            .CountAsync(n => n.Items.Any(i => i.ProductId == productId));

        return Ok(new
        {
            linked = true,
            message = "Produto vinculado a notas fiscais.",
            invoicesCount = quantidadeNotas
        });
    }

    [HttpPost]
    public async Task<ActionResult<Nota>> Create(NotaCreateDto dto)
    {
        var validacao = ValidarDto(dto);
        if (validacao is not null)
            return validacao;

        var nota = new Nota
        {
            Number = await ObterProximoNumeroAsync(),
            Status = "Aberta",
            Items = NormalizarItens(dto)
        };

        _context.Notas.Add(nota);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nota.Id }, nota);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Nota>> Update(int id, NotaCreateDto dto)
    {
        var nota = await _context.Notas
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (nota is null)
            return NotFound(new { message = "Nota fiscal não encontrada." });

        if (nota.Status != "Aberta")
            return BadRequest(new { message = "Só é possível editar notas com status Aberta." });

        var validacao = ValidarDto(dto);
        if (validacao is not null)
            return validacao;

        _context.RemoveRange(nota.Items);
        nota.Items = NormalizarItens(dto);

        await _context.SaveChangesAsync();

        return Ok(nota);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var nota = await _context.Notas
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (nota is null)
            return NotFound(new { message = "Nota fiscal não encontrada." });

        if (nota.Status != "Aberta")
            return BadRequest(new { message = "Só é possível excluir notas com status Aberta." });

        _context.RemoveRange(nota.Items);
        _context.Notas.Remove(nota);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Nota fiscal excluída com sucesso." });
    }

    [HttpPost("{id:int}/impressao")]
    public async Task<ActionResult> Print(int id)
    {
        var idempotencyKeyHeader = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKeyHeader)
            ? $"print-note-{id}"
            : idempotencyKeyHeader.Trim();

        var cacheKey = $"invoice-print:{id}:{idempotencyKey}";

        if (_memoryCache.TryGetValue(cacheKey, out PrintNotaResultadoDto? cachedResult) && cachedResult is not null)
            return Ok(cachedResult);

        var nota = await _context.Notas
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (nota is null)
            return NotFound(new { message = "Nota fiscal não encontrada." });

        if (nota.Status != "Aberta")
            return BadRequest(new { message = "Somente notas com status Aberta podem ser impressas." });

        var requisicaoBaixa = new BaixaProdutoRequestDto
        {
            Items = nota.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new BaixaProdutoDto
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToList()
        };

        var respostaEstoque = await _estoqueApiClient.DecreaseStockAsync(requisicaoBaixa);

        if (!respostaEstoque.Success)
        {
            var payload = new
            {
                message = "A impressão da nota não foi concluída.",
                details = respostaEstoque.Message
            };

            if (respostaEstoque.IsClientError)
                return BadRequest(payload);

            if (respostaEstoque.StatusCode == StatusCodes.Status503ServiceUnavailable)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, payload);

            if (respostaEstoque.StatusCode == StatusCodes.Status504GatewayTimeout)
                return StatusCode(StatusCodes.Status504GatewayTimeout, payload);

            return StatusCode(StatusCodes.Status502BadGateway, payload);
        }

        nota.Status = "Fechada";
        await _context.SaveChangesAsync();

        var result = new PrintNotaResultadoDto
        {
            Message = "Nota fiscal impressa com sucesso.",
            InvoiceId = nota.Id,
            Status = nota.Status,
            IdempotencyKey = idempotencyKey
        };

        _memoryCache.Set(
            cacheKey,
            result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

        return Ok(result);
    }

    private ActionResult? ValidarDto(NotaCreateDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { message = "Inclua pelo menos um item na nota fiscal." });

        if (dto.Items.Any(i => i.ProductId <= 0))
            return BadRequest(new { message = "Todos os itens precisam informar um produto válido." });

        if (dto.Items.Any(i => string.IsNullOrWhiteSpace(i.ProductCode)))
            return BadRequest(new { message = "Todos os itens precisam informar o código do produto." });

        if (dto.Items.Any(i => string.IsNullOrWhiteSpace(i.ProductDescription)))
            return BadRequest(new { message = "Todos os itens precisam informar a descrição do produto." });

        if (dto.Items.Any(i => i.Quantity <= 0))
            return BadRequest(new { message = "A quantidade dos itens deve ser maior que zero." });

        return null;
    }

    private List<ItemNota> NormalizarItens(NotaCreateDto dto)
    {
        return dto.Items
            .GroupBy(item => item.ProductId)
            .Select(group =>
            {
                var primeiroItem = group.First();

                return new ItemNota
                {
                    ProductId = group.Key,
                    ProductCode = primeiroItem.ProductCode.Trim(),
                    ProductDescription = primeiroItem.ProductDescription.Trim(),
                    Quantity = group.Sum(item => item.Quantity)
                };
            })
            .ToList();
    }

    private async Task<int> ObterProximoNumeroAsync()
    {
        var ultimoNumero = await _context.Notas
            .OrderByDescending(i => i.Number)
            .Select(i => i.Number)
            .FirstOrDefaultAsync();

        return ultimoNumero + 1;
    }
}

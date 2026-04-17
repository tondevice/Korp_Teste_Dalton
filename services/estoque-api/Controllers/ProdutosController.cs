using estoque_api.Data;
using estoque_api.Dtos;
using estoque_api.Entities;
using estoque_api.Integrations.Faturamento;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace estoque_api.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _context;
    private readonly FaturamentoApiClient _faturamentoApiClient;

    public ProdutosController(EstoqueDbContext context, FaturamentoApiClient faturamentoApiClient)
    {
        _context = context;
        _faturamentoApiClient = faturamentoApiClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produto>>> GetAll()
    {
        var produtos = await _context.Produtos
            .OrderBy(p => p.Id)
            .ToListAsync();

        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Produto>> GetById(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);

        if (produto is null)
            return NotFound(new { message = "Produto não encontrado." });

        return Ok(produto);
    }

    [HttpPost]
    public async Task<ActionResult<Produto>> Create(ProdutoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "Informe o código do produto." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Informe a descrição do produto." });

        if (dto.Stock < 0)
            return BadRequest(new { message = "O saldo do produto deve ser zero ou maior." });

        var codigoNormalizado = dto.Code.Trim();

        var produtoComMesmoCodigo = await _context.Produtos
            .AnyAsync(p => p.Code.ToLower() == codigoNormalizado.ToLower());

        if (produtoComMesmoCodigo)
            return BadRequest(new { message = "Já existe um produto com esse código." });

        var produto = new Produto
        {
            Code = codigoNormalizado,
            Description = dto.Description.Trim(),
            Stock = dto.Stock
        };

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = produto.Id }, produto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Produto>> Update(int id, ProdutoCreateDto dto)
    {
        var produto = await _context.Produtos.FindAsync(id);

        if (produto is null)
            return NotFound(new { message = "Produto não encontrado." });

        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "Informe o código do produto." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Informe a descrição do produto." });

        if (dto.Stock < 0)
            return BadRequest(new { message = "O saldo do produto deve ser zero ou maior." });

        var codigoNormalizado = dto.Code.Trim();

        var produtoComMesmoCodigo = await _context.Produtos
            .AnyAsync(p => p.Id != id && p.Code.ToLower() == codigoNormalizado.ToLower());

        if (produtoComMesmoCodigo)
            return BadRequest(new { message = "Já existe outro produto com esse código." });

        produto.Code = codigoNormalizado;
        produto.Description = dto.Description.Trim();
        produto.Stock = dto.Stock;

        await _context.SaveChangesAsync();

        return Ok(produto);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);

        if (produto is null)
            return NotFound(new { message = "Produto não encontrado." });

        var vinculo = await _faturamentoApiClient.CheckProductLinkAsync(id);

        if (vinculo.Linked)
        {
            return BadRequest(new
            {
                message = string.IsNullOrWhiteSpace(vinculo.Message)
                    ? "Este produto não pode ser excluído porque já está em uso em notas fiscais."
                    : vinculo.Message
            });
        }

        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Produto excluído com sucesso." });
    }

    [HttpPost("baixas")]
    public async Task<ActionResult> BaixarEstoque(BaixaProdutoRequestDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Nenhum item foi enviado para baixar do estoque.",
                statusCode = StatusCodes.Status400BadRequest
            });
        }

        if (dto.Items.Any(item => item.ProductId <= 0))
        {
            return BadRequest(new
            {
                success = false,
                message = "Todos os itens precisam informar um produto válido.",
                statusCode = StatusCodes.Status400BadRequest
            });
        }

        if (dto.Items.Any(item => item.Quantity <= 0))
        {
            return BadRequest(new
            {
                success = false,
                message = "A quantidade informada para baixa deve ser maior que zero.",
                statusCode = StatusCodes.Status400BadRequest
            });
        }

        var itensAgrupados = dto.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        var productIds = itensAgrupados
            .Select(item => item.ProductId)
            .ToList();

        var produtos = await _context.Produtos
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in itensAgrupados)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProductId);

            if (produto is null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Um dos produtos informados não foi encontrado (id {item.ProductId}).",
                    statusCode = StatusCodes.Status400BadRequest
                });
            }

            if (produto.Stock < item.Quantity)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Estoque insuficiente para {produto.Description}. Saldo disponível: {produto.Stock}. Quantidade solicitada: {item.Quantity}.",
                    statusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        foreach (var item in itensAgrupados)
        {
            var produto = produtos.First(p => p.Id == item.ProductId);
            produto.Stock -= item.Quantity;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Estoque atualizado com sucesso.",
            statusCode = StatusCodes.Status200OK
        });
    }
}

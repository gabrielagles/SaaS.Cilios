using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Controllers.Api;

// Endpoint JSON usado pelo offline-sync.js para reenviar, quando a conexão
// voltar, os agendamentos que foram criados enquanto o celular estava sem
// internet (ficam guardados no IndexedDB do navegador até esse momento).
[Authorize]
[ApiController]
[Route("api/agendamentos")]
public class AgendamentoApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public AgendamentoApiController(AppDbContext context)
    {
        _context = context;
    }

    public class AgendamentoInput
    {
        public string ClienteNome { get; set; } = string.Empty;
        public string? ClienteTelefone { get; set; }
        public int ServicoId { get; set; }
        public DateTime DataHora { get; set; }
        public decimal Valor { get; set; }
        public bool Concluido { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AgendamentoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ClienteNome) || input.ServicoId <= 0)
        {
            return BadRequest(new { erro = "Dados incompletos." });
        }

        var servicoExiste = await _context.Servicos.AnyAsync(s => s.Id == input.ServicoId);
        if (!servicoExiste)
        {
            return BadRequest(new { erro = "Serviço não encontrado." });
        }

        var agendamento = new Agendamento
        {
            ClienteNome = input.ClienteNome,
            ClienteTelefone = input.ClienteTelefone,
            ServicoId = input.ServicoId,
            DataHora = input.DataHora,
            Valor = input.Valor,
            Concluido = input.Concluido,
            CriadoOffline = true
        };

        _context.Agendamentos.Add(agendamento);
        await _context.SaveChangesAsync();

        return Ok(new { id = agendamento.Id });
    }

    [HttpGet("servicos")]
    public async Task<IActionResult> ListarServicosAtivos()
    {
        var servicos = await _context.Servicos
            .Where(s => s.Ativo)
            .OrderBy(s => s.Nome)
            .Select(s => new { s.Id, s.Nome, s.Preco, s.Cor })
            .ToListAsync();

        return Ok(servicos);
    }
}

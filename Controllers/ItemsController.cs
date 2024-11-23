using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ListaTarefas.Areas.Identity.Data;
using ListaTarefas.Models;
using System.Security.Claims;
using PagedList.Core;


namespace ListaTarefas.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public ItemsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: Items
        public IActionResult Index(string nome, string categoria, string prioridade, bool? completa, int page = 1, int pageSize = 8)
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var item = _context.Items
                                .Where(a => a.iduser == userId);

            // Aplica filtros
            if (!string.IsNullOrEmpty(nome))
                item = item.Where(a => a.Nome.Contains(nome));

            if (!string.IsNullOrEmpty(categoria))
                item = item.Where(a => a.Categoria == categoria);

            if (!string.IsNullOrEmpty(prioridade))
                item = item.Where(a => a.Prioridade == prioridade);

            if (completa.HasValue)
                item = item.Where(a => a.Completa == completa);

            // Data limite para o alerta de tarefas próximas do prazo (ex.: 3 dias a partir de hoje)
            DateTime dataLimite = DateTime.Now.AddDays(3);

            // Data atual
            DateTime dataAtual = DateTime.Now;

            // Tarefas próximas do prazo
            var tarefasProximas = item
                .Where(t => t.DataVencimento <= dataLimite && t.DataVencimento > dataAtual && !t.Completa)
                .ToList();

            // Tarefas já vencidas
            var tarefasVencidas = item
                .Where(t => t.DataVencimento <= dataAtual && !t.Completa)
                .ToList();

            // Aplica paginação
            var pagedList = item.ToPagedList(page, pageSize);

            // Passa as listas de tarefas para o ViewData
            ViewData["TarefasProximas"] = tarefasProximas;
            ViewData["TarefasVencidas"] = tarefasVencidas;
            ViewData["PagedList"] = pagedList;

            // Passa a lista paginada para a view
            return View(pagedList);
        }




        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Items/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Items/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nome,Descricao,Categoria,Prioridade,DataVencimento,Completa,iduser")] Item item, IFormFile arquivo)
        {
            if (ModelState.IsValid)
            {
                // Verifica se um arquivo foi enviado
                if (arquivo != null && arquivo.Length > 0)
                {
                    // Define o tamanho máximo permitido (exemplo: 10 MB)
                    const long tamanhoMaximo = 10 * 1024 * 1024;

                    // Verifica se o arquivo excede o tamanho permitido
                    if (arquivo.Length > tamanhoMaximo)
                    {
                        ModelState.AddModelError("CaminhoArquivo", "O arquivo é muito grande. O tamanho máximo permitido é 10 MB.");
                        return View(item);
                    }

                    // Lista de extensões permitidas
                    var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".txt", ".docx", ".pdf", ".zip", ".rar" };

                    // Obtém a extensão do arquivo
                    var extensaoArquivo = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

                    // Valida a extensão do arquivo
                    if (!extensoesPermitidas.Contains(extensaoArquivo))
                    {
                        ModelState.AddModelError("CaminhoArquivo", "Tipo de arquivo não permitido. Apenas imagens, textos e arquivos compactados são aceitos.");
                        return View(item);
                    }

                    // Define o caminho para a pasta "files" dentro de wwwroot
                    var caminhoPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");

                    // Garante que a pasta exista
                    if (!Directory.Exists(caminhoPasta))
                    {
                        Directory.CreateDirectory(caminhoPasta);
                    }

                    // Usa o nome original do arquivo, com segurança para evitar problemas com nomes inválidos
                    var nomeArquivoOriginal = Path.GetFileName(arquivo.FileName);

                    // Define o caminho completo para salvar o arquivo
                    var caminhoArquivo = Path.Combine(caminhoPasta, nomeArquivoOriginal);

                    // Garante que o nome do arquivo seja único para evitar substituição
                    var contador = 1;
                    while (System.IO.File.Exists(caminhoArquivo))
                    {
                        // Se o arquivo já existir, adiciona um sufixo para garantir um nome único
                        var nomeArquivoNovo = Path.GetFileNameWithoutExtension(nomeArquivoOriginal) + $"_{contador}{extensaoArquivo}";
                        caminhoArquivo = Path.Combine(caminhoPasta, nomeArquivoNovo);
                        contador++;
                    }

                    // Salva o arquivo no caminho especificado
                    using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
                    {
                        await arquivo.CopyToAsync(stream);
                    }

                    // Associa o caminho do arquivo ao item
                    item.CaminhoArquivo = Path.Combine("files", Path.GetFileName(caminhoArquivo));  // Salva apenas o nome do arquivo na base de dados
                }

                // Adiciona o item ao banco de dados
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }



        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao,Categoria,Prioridade,DataVencimento,Completa,iduser,CaminhoArquivo")] Item item, IFormFile arquivo, bool removerArquivo)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            // Remove o campo "arquivo" do ModelState, pois não faz parte do modelo Item
            ModelState.Remove("arquivo");

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtem o item atual do banco de dados para verificar o estado anterior
                    var itemAtual = await _context.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    if (itemAtual == null)
                    {
                        return NotFound();
                    }

                    // Verifica se o usuário optou por remover o arquivo
                    if (removerArquivo && !string.IsNullOrEmpty(itemAtual.CaminhoArquivo))
                    {
                        var caminhoArquivoExistente = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", itemAtual.CaminhoArquivo);
                        if (System.IO.File.Exists(caminhoArquivoExistente))
                        {
                            System.IO.File.Delete(caminhoArquivoExistente);
                        }

                        // Remove o caminho do arquivo no banco de dados
                        item.CaminhoArquivo = null;
                    }
                    else
                    {
                        item.CaminhoArquivo = itemAtual.CaminhoArquivo;
                    }

                    // Se um novo arquivo foi enviado
                    if (arquivo != null && arquivo.Length > 0)
                    {
                        // Define o tamanho máximo permitido (exemplo: 10 MB)
                        const long tamanhoMaximo = 10 * 1024 * 1024;

                        // Verifica se o arquivo excede o tamanho permitido
                        if (arquivo.Length > tamanhoMaximo)
                        {
                            ModelState.AddModelError("CaminhoArquivo", "O arquivo é muito grande. O tamanho máximo permitido é 10 MB.");
                            return View(item);
                        }

                        // Lista de extensões permitidas
                        var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".txt", ".docx", ".pdf", ".zip", ".rar" };

                        // Obtém a extensão do arquivo
                        var extensaoArquivo = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

                        // Valida a extensão do arquivo
                        if (!extensoesPermitidas.Contains(extensaoArquivo))
                        {
                            ModelState.AddModelError("CaminhoArquivo", "Tipo de arquivo não permitido. Apenas imagens, textos e arquivos compactados são aceitos.");
                            return View(item);
                        }

                        // Exclui o arquivo anterior (se houver)
                        if (!string.IsNullOrEmpty(itemAtual.CaminhoArquivo))
                        {
                            var caminhoArquivoAnterior = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", itemAtual.CaminhoArquivo);
                            if (System.IO.File.Exists(caminhoArquivoAnterior))
                            {
                                System.IO.File.Delete(caminhoArquivoAnterior);
                            }
                        }

                        // Define o caminho para a pasta "files" dentro de wwwroot
                        var caminhoPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");

                        // Garante que a pasta exista
                        if (!Directory.Exists(caminhoPasta))
                        {
                            Directory.CreateDirectory(caminhoPasta);
                        }

                        // Usa o nome original do arquivo para salvar
                        var nomeArquivo = Path.GetFileName(arquivo.FileName);

                        // Define o caminho completo do arquivo
                        var caminhoArquivo = Path.Combine(caminhoPasta, nomeArquivo);

                        // Salva o arquivo no caminho especificado
                        using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
                        {
                            await arquivo.CopyToAsync(stream);
                        }

                        // Atualiza o campo CaminhoArquivo com o novo caminho
                        item.CaminhoArquivo = Path.Combine("files", nomeArquivo);
                    }

                    // Atualiza o item no banco de dados
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }




        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                // Verifica se a tarefa tem um anexo e exclui o arquivo
                if (!string.IsNullOrEmpty(item.CaminhoArquivo))
                {
                    // Define o caminho completo para o arquivo
                    var caminhoArquivo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.CaminhoArquivo);

                    // Verifica se o arquivo existe e, em caso afirmativo, exclui
                    if (System.IO.File.Exists(caminhoArquivo))
                    {
                        System.IO.File.Delete(caminhoArquivo);
                    }
                }

                // Exclui a tarefa do banco de dados
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }
    }
}

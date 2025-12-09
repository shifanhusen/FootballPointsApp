using FootballPointsApp.Data;
using FootballPointsApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPointsApp.Controllers;

public class PlayersController : Controller
{
    private readonly AppDbContext _context;

    public PlayersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var players = await _context.Players
            .Include(p => p.PointsLogs)
            .ToListAsync();
            
        // Calculate total points for display
        // We can use a ViewModel, but ViewBag or dynamic is quicker for now. 
        // Let's use a simple ViewModel approach or just pass the model.
        // The user asked for "total points".
        
        return View(players);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Create(Player player)
    {
        if (ModelState.IsValid)
        {
            _context.Add(player);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(player);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var player = await _context.Players.FindAsync(id);
        if (player == null) return NotFound();
        return View(player);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Edit(int id, Player player)
    {
        if (id != player.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(player);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlayerExists(player.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(player);
    }

    private bool PlayerExists(int id)
    {
        return _context.Players.Any(e => e.Id == id);
    }
}

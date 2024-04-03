using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinPort.Data;
using FinPort.Models;
using FinPort.Services;

namespace FinPort.Controllers
{
    public class PortfolioPositionsController : Controller
    {
        private readonly DataBaseContext _context;
        private readonly JustEtfWebSocketClient _wsClient;

        public PortfolioPositionsController(DataBaseContext context, JustEtfWebSocketClient wsClient)
        {
            _context = context;
            _wsClient = wsClient;
        }

        // GET: PortfolioPositions/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portfolioPosition = await _context.PortfolioPositions
                .Include(p => p.Portfolio)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (portfolioPosition == null)
            {
                return NotFound();
            }

            return View(portfolioPosition);
        }

        // GET: PortfolioPositions/Create
        public IActionResult Create(string id)
        {
            return View(new PortfolioPosition()
            {
                PortfolioId = id
            });
        }

        // POST: PortfolioPositions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ISIN,Quantity,PurchasePrice,PortfolioId")] PortfolioPosition portfolioPosition)
        {
            if (ModelState.IsValid)
            {
                portfolioPosition.PurchasePrice /= portfolioPosition.Quantity;
                _context.Add(portfolioPosition);
                await _context.SaveChangesAsync();
                _wsClient.AddISIN(portfolioPosition?.ISIN ?? "");
                return RedirectToAction(nameof(Edit), "Portfolios", new { id = portfolioPosition?.PortfolioId ?? "" });
            }
            return View(portfolioPosition);
        }

        // GET: PortfolioPositions/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portfolioPosition = await _context.PortfolioPositions.FindAsync(id);
            if (portfolioPosition == null)
            {
                return NotFound();
            }
            portfolioPosition.PurchasePrice *= portfolioPosition.Quantity;
            return View(portfolioPosition);
        }

        // POST: PortfolioPositions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,ISIN,Quantity,PurchasePrice,PortfolioId")] PortfolioPosition portfolioPosition)
        {
            if (id != portfolioPosition.Id)
            {
                return NotFound();
            }

            var model = await _context.PortfolioPositions.FirstOrDefaultAsync(p => p.Id == id);
            if (model == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    model.Name = portfolioPosition.Name;
                    model.Quantity = portfolioPosition.Quantity;
                    if(model.ISIN != portfolioPosition.ISIN)
                    {
                        _wsClient.RemoveISIN(model.ISIN ?? "");
                        _wsClient.AddISIN(portfolioPosition.ISIN ?? "");
                    }
                    model.ISIN = portfolioPosition.ISIN;
                    model.PurchasePrice = portfolioPosition.PurchasePrice / portfolioPosition.Quantity;
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PortfolioPositionExists(portfolioPosition.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Edit), "Portfolios", new { id = portfolioPosition.PortfolioId });
            }
            return View(portfolioPosition);
        }

        // GET: PortfolioPositions/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portfolioPosition = await _context.PortfolioPositions
                .Include(p => p.Portfolio)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (portfolioPosition == null)
            {
                return NotFound();
            }

            return View(portfolioPosition);
        }

        // POST: PortfolioPositions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var portfolioPosition = await _context.PortfolioPositions.FindAsync(id);
            if (portfolioPosition != null)
            {
                _context.PortfolioPositions.Remove(portfolioPosition);
            }

            await _context.SaveChangesAsync();
            if(!_context.PortfolioPositions.Any(p => p.ISIN == portfolioPosition.ISIN))
            {
                _wsClient.RemoveISIN(portfolioPosition.ISIN);
            }
            return RedirectToAction(nameof(Edit), "Portfolios", new { id = portfolioPosition.PortfolioId });
        }

        private bool PortfolioPositionExists(string id)
        {
            return _context.PortfolioPositions.Any(e => e.Id == id);
        }
    }
}

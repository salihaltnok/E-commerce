using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Al_Gotur2.Controllers
{
    public class AdresController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public AdresController(Al_Gotur2Context context)
        {
            _context = context;
        }

        // GET: Adres
        public async Task<IActionResult> Index()
        {
            var adresler = await _context.Adresler
                .Include(a => a.Kullanici)
                .ToListAsync();
            return View(adresler);
        }

        // GET: Adres/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adres = await _context.Adresler
                .Include(a => a.Kullanici)
                .FirstOrDefaultAsync(m => m.AdresID == id);

            if (adres == null)
            {
                return NotFound();
            }

            return View(adres);
        }

        // GET: Adres/Create
        public IActionResult Create()
        {
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad");
            return View();
        }

        // POST: Adres/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AdresID,KullaniciID,AdresSatiri,Sehir,PostaKodu,Ulke")] Adres adres)
        {
            if (ModelState.IsValid)
            {
                _context.Add(adres);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", adres.KullaniciID);
            return View(adres);
        }

        // GET: Adres/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adres = await _context.Adresler.FindAsync(id);
            if (adres == null)
            {
                return NotFound();
            }
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", adres.KullaniciID);
            return View(adres);
        }

        // POST: Adres/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdresID,KullaniciID,AdresSatiri,Sehir,PostaKodu,Ulke")] Adres adres)
        {
            if (id != adres.AdresID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(adres);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdresExists(adres.AdresID))
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
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", adres.KullaniciID);
            return View(adres);
        }

        // GET: Adres/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adres = await _context.Adresler
                .Include(a => a.Kullanici)
                .FirstOrDefaultAsync(m => m.AdresID == id);
            if (adres == null)
            {
                return NotFound();
            }

            return View(adres);
        }

        // POST: Adres/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var adres = await _context.Adresler.FindAsync(id);
            if (adres != null)
            {
                _context.Adresler.Remove(adres);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AdresExists(int id)
        {
            return _context.Adresler.Any(e => e.AdresID == id);
        }
    }
}
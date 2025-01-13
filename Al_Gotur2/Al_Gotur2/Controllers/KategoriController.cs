using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;

namespace Al_Gotur2.Controllers
{
    public class KategoriController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public KategoriController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kategoriler = await _context.Kategoriler
                .Include(k => k.Urunler)
                .ToListAsync();
            return View(kategoriler);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler
                .Include(k => k.Urunler)
                .FirstOrDefaultAsync(m => m.KategoriID == id);

            if (kategori == null)
            {
                return NotFound();
            }

            return View(kategori);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KategoriID,KategoriAdi")] Kategori kategori)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Kategoriler.AnyAsync(k => k.KategoriAdi == kategori.KategoriAdi))
                {
                    ModelState.AddModelError("KategoriAdi", "Bu kategori adı zaten kullanılıyor.");
                    return View(kategori);
                }

                _context.Add(kategori);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(kategori);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler.FindAsync(id);
            if (kategori == null)
            {
                return NotFound();
            }
            return View(kategori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KategoriID,KategoriAdi")] Kategori kategori)
        {
            if (id != kategori.KategoriID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (await _context.Kategoriler.AnyAsync(k => k.KategoriAdi == kategori.KategoriAdi && k.KategoriID != kategori.KategoriID))
                {
                    ModelState.AddModelError("KategoriAdi", "Bu kategori adı zaten kullanılıyor.");
                    return View(kategori);
                }

                try
                {
                    _context.Update(kategori);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KategoriExists(kategori.KategoriID))
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
            return View(kategori);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler
                .Include(k => k.Urunler)
                .FirstOrDefaultAsync(m => m.KategoriID == id);

            if (kategori == null)
            {
                return NotFound();
            }

            if (kategori.Urunler != null && kategori.Urunler.Any())
            {
                ViewBag.HataMesaji = "Bu kategoride ürünler bulunduğu için silinemez.";
            }

            return View(kategori);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kategori = await _context.Kategoriler
                .Include(k => k.Urunler)
                .FirstOrDefaultAsync(m => m.KategoriID == id);

            if (kategori == null)
            {
                return NotFound();
            }

            if (kategori.Urunler != null && kategori.Urunler.Any())
            {
                ModelState.AddModelError("", "Bu kategoride ürünler bulunduğu için silinemez.");
                return View(kategori);
            }

            _context.Kategoriler.Remove(kategori);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> KategoriUrunleri(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler
                .Include(k => k.Urunler)
                .FirstOrDefaultAsync(m => m.KategoriID == id);

            if (kategori == null)
            {
                return NotFound();
            }

            return View(kategori);
        }

        [HttpGet]
        public async Task<JsonResult> GetKategoriler()
        {
            var kategoriler = await _context.Kategoriler
                .Select(k => new { k.KategoriID, k.KategoriAdi })
                .ToListAsync();
            return Json(kategoriler);
        }

        public async Task<IActionResult> Istatistikler()
        {
            var kategoriler = await _context.Kategoriler
                .Include(k => k.Urunler)
                .Select(k => new
                {
                    KategoriAdi = k.KategoriAdi,
                    UrunSayisi = k.Urunler.Count,
                    ToplamStok = k.Urunler.Sum(u => u.StokMiktari),
                    OrtalamaFiyat = k.Urunler.Any() ? k.Urunler.Average(u => u.Fiyat) : 0
                })
                .ToListAsync();

            return View(kategoriler);
        }

        private bool KategoriExists(int id)
        {
            return _context.Kategoriler.Any(e => e.KategoriID == id);
        }
    }
}
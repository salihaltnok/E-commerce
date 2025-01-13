using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;

namespace Al_Gotur2.Controllers
{
    public class IndirimController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public IndirimController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var indirimler = await _context.Indirimler
                .Include(i => i.Urun)
                .OrderByDescending(i => i.GecerlilikTarihi)
                .ToListAsync();
            return View(indirimler);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indirim = await _context.Indirimler
                .Include(i => i.Urun)
                .FirstOrDefaultAsync(m => m.IndirimID == id);

            if (indirim == null)
            {
                return NotFound();
            }

            return View(indirim);
        }

        public IActionResult Create()
        {
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IndirimID,UrunID,IndirimOrani,GecerlilikTarihi")] Indirim indirim)
        {
            if (ModelState.IsValid)
            {
                var mevcutIndirim = await _context.Indirimler
                    .Where(i => i.UrunID == indirim.UrunID && i.GecerlilikTarihi > DateTime.Now)
                    .FirstOrDefaultAsync();

                if (mevcutIndirim != null)
                {
                    ModelState.AddModelError("", "Bu ürün için zaten aktif bir indirim bulunmaktadır.");
                }
                else
                {
                    _context.Add(indirim);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", indirim.UrunID);
            return View(indirim);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indirim = await _context.Indirimler.FindAsync(id);
            if (indirim == null)
            {
                return NotFound();
            }

            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", indirim.UrunID);
            return View(indirim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IndirimID,UrunID,IndirimOrani,GecerlilikTarihi")] Indirim indirim)
        {
            if (id != indirim.IndirimID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(indirim);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IndirimExists(indirim.IndirimID))
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

            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", indirim.UrunID);
            return View(indirim);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indirim = await _context.Indirimler
                .Include(i => i.Urun)
                .FirstOrDefaultAsync(m => m.IndirimID == id);

            if (indirim == null)
            {
                return NotFound();
            }

            return View(indirim);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var indirim = await _context.Indirimler.FindAsync(id);
            if (indirim != null)
            {
                _context.Indirimler.Remove(indirim);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AktifIndirimler()
        {
            var aktifIndirimler = await _context.Indirimler
                .Include(i => i.Urun)
                .Where(i => i.GecerlilikTarihi > DateTime.Now)
                .OrderByDescending(i => i.IndirimOrani)
                .ToListAsync();

            return View(aktifIndirimler);
        }

        public async Task<IActionResult> GecmisIndirimler()
        {
            var gecmisIndirimler = await _context.Indirimler
                .Include(i => i.Urun)
                .Where(i => i.GecerlilikTarihi <= DateTime.Now)
                .OrderByDescending(i => i.GecerlilikTarihi)
                .ToListAsync();

            return View(gecmisIndirimler);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluIndirimUygula(int kategoriId, decimal indirimOrani, DateTime gecerlilikTarihi)
        {
            var urunler = await _context.Urunler
                .Where(u => u.KategoriID == kategoriId)
                .ToListAsync();

            foreach (var urun in urunler)
            {
                var yeniIndirim = new Indirim
                {
                    UrunID = urun.UrunID,
                    IndirimOrani = indirimOrani,
                    GecerlilikTarihi = gecerlilikTarihi
                };

                _context.Indirimler.Add(yeniIndirim);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IndirimExists(int id)
        {
            return _context.Indirimler.Any(e => e.IndirimID == id);
        }

        private decimal IndirimliFiyatHesapla(decimal normalFiyat, decimal indirimOrani)
        {
            return normalFiyat - (normalFiyat * indirimOrani / 100);
        }
    }
}
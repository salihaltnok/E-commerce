using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;

namespace Al_Gotur2.Controllers
{
    public class SiparisDetayiController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public SiparisDetayiController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var siparisDetaylari = await _context.SiparisDetaylari
                .Include(s => s.Siparis)
                .Include(s => s.Urun)
                .Where(s => !s.IsDeleted)
                .ToListAsync();
            return View(siparisDetaylari);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var siparisDetayi = await _context.SiparisDetaylari
                .Include(s => s.Siparis)
                .Include(s => s.Urun)
                .FirstOrDefaultAsync(m => m.SiparisDetayID == id && !m.IsDeleted);

            if (siparisDetayi == null)
            {
                return NotFound();
            }

            return View(siparisDetayi);
        }
        public IActionResult Create()
        {
            ViewBag.SiparisID = new SelectList(_context.Siparisler.Where(s => !s.IsDeleted), "SiparisID", "SiparisID");
            ViewBag.UrunID = new SelectList(_context.Urunler.Where(u => !u.IsDeleted), "UrunID", "UrunAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SiparisDetayID,SiparisID,UrunID,Miktar,Fiyat")] SiparisDetayi siparisDetayi)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var urun = await _context.Urunler.FindAsync(siparisDetayi.UrunID);
                    if (urun == null || urun.IsDeleted)
                    {
                        throw new Exception("Ürün bulunamadı!");
                    }

                    if (urun.StokMiktari < siparisDetayi.Miktar)
                    {
                        throw new Exception("Yetersiz stok!");
                    }

                    siparisDetayi.Fiyat = urun.Fiyat;
                    siparisDetayi.IsDeleted = false;
                    urun.StokMiktari -= siparisDetayi.Miktar;

                    _context.Update(urun);
                    _context.Add(siparisDetayi);
                    await _context.SaveChangesAsync();

                    var siparis = await _context.Siparisler.FindAsync(siparisDetayi.SiparisID);
                    if (siparis != null && !siparis.IsDeleted)
                    {
                        siparis.ToplamTutar += (siparisDetayi.Fiyat * siparisDetayi.Miktar);
                        _context.Update(siparis);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Hata: " + ex.Message);
                }
            }

            ViewBag.SiparisID = new SelectList(_context.Siparisler.Where(s => !s.IsDeleted), "SiparisID", "SiparisID", siparisDetayi.SiparisID);
            ViewBag.UrunID = new SelectList(_context.Urunler.Where(u => !u.IsDeleted), "UrunID", "UrunAdi", siparisDetayi.UrunID);
            return View(siparisDetayi);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var siparisDetayi = await _context.SiparisDetaylari
                .FirstOrDefaultAsync(sd => sd.SiparisDetayID == id && !sd.IsDeleted);
            if (siparisDetayi == null)
            {
                return NotFound();
            }

            ViewBag.SiparisID = new SelectList(_context.Siparisler.Where(s => !s.IsDeleted), "SiparisID", "SiparisID", siparisDetayi.SiparisID);
            ViewBag.UrunID = new SelectList(_context.Urunler.Where(u => !u.IsDeleted), "UrunID", "UrunAdi", siparisDetayi.UrunID);
            return View(siparisDetayi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SiparisDetayID,SiparisID,UrunID,Miktar,Fiyat,IsDeleted")] SiparisDetayi siparisDetayi)
        {
            if (id != siparisDetayi.SiparisDetayID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var mevcutDetay = await _context.SiparisDetaylari
                        .FirstOrDefaultAsync(sd => sd.SiparisDetayID == id && !sd.IsDeleted);
                    if (mevcutDetay == null)
                    {
                        throw new Exception("Sipariş detayı bulunamadı!");
                    }

                    var urun = await _context.Urunler
                        .FirstOrDefaultAsync(u => u.UrunID == siparisDetayi.UrunID && !u.IsDeleted);
                    if (urun == null)
                    {
                        throw new Exception("Ürün bulunamadı!");
                    }

                    int stokFarki = siparisDetayi.Miktar - mevcutDetay.Miktar;
                    if (urun.StokMiktari < stokFarki)
                    {
                        throw new Exception("Yetersiz stok!");
                    }

                    urun.StokMiktari -= stokFarki;
                    _context.Update(urun);

                    mevcutDetay.Miktar = siparisDetayi.Miktar;
                    mevcutDetay.Fiyat = urun.Fiyat;
                    _context.Update(mevcutDetay);

                    var siparis = await _context.Siparisler
                        .Include(s => s.SiparisDetaylari.Where(sd => !sd.IsDeleted))
                        .FirstOrDefaultAsync(s => s.SiparisID == siparisDetayi.SiparisID && !s.IsDeleted);

                    if (siparis != null)
                    {
                        siparis.ToplamTutar = siparis.SiparisDetaylari.Sum(sd => sd.Fiyat * sd.Miktar);
                        _context.Update(siparis);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Hata: " + ex.Message);
                }
            }

            ViewBag.SiparisID = new SelectList(_context.Siparisler.Where(s => !s.IsDeleted), "SiparisID", "SiparisID", siparisDetayi.SiparisID);
            ViewBag.UrunID = new SelectList(_context.Urunler.Where(u => !u.IsDeleted), "UrunID", "UrunAdi", siparisDetayi.UrunID);
            return View(siparisDetayi);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var siparisDetayi = await _context.SiparisDetaylari
                .Include(s => s.Siparis)
                .Include(s => s.Urun)
                .FirstOrDefaultAsync(m => m.SiparisDetayID == id && !m.IsDeleted);

            if (siparisDetayi == null)
            {
                return NotFound();
            }

            return View(siparisDetayi);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var siparisDetayi = await _context.SiparisDetaylari
                    .Include(s => s.Urun)
                    .FirstOrDefaultAsync(s => s.SiparisDetayID == id && !s.IsDeleted);

                if (siparisDetayi != null)
                {
                    siparisDetayi.IsDeleted = true;
                    _context.Update(siparisDetayi);

                    siparisDetayi.Urun.StokMiktari += siparisDetayi.Miktar;
                    _context.Update(siparisDetayi.Urun);

                    var siparis = await _context.Siparisler
                        .Include(s => s.SiparisDetaylari.Where(sd => !sd.IsDeleted))
                        .FirstOrDefaultAsync(s => s.SiparisID == siparisDetayi.SiparisID && !s.IsDeleted);

                    if (siparis != null)
                    {
                        siparis.ToplamTutar = siparis.SiparisDetaylari.Sum(sd => sd.Fiyat * sd.Miktar);
                        _context.Update(siparis);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Silme işlemi sırasında hata: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSiparisDetaylari(int siparisId)
        {
            var detaylar = await _context.SiparisDetaylari
                .Include(s => s.Urun)
                .Where(s => s.SiparisID == siparisId && !s.IsDeleted)
                .Select(s => new
                {
                    s.SiparisDetayID,
                    UrunAdi = s.Urun.UrunAdi,
                    s.Miktar,
                    s.Fiyat,
                    ToplamTutar = s.Miktar * s.Fiyat
                })
                .ToListAsync();

            return Json(detaylar);
        }

        private bool SiparisDetayiExists(int id)
        {
            return _context.SiparisDetaylari.Any(e => e.SiparisDetayID == id && !e.IsDeleted);
        }
    }
}
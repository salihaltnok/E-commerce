using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Al_Gotur2.Controllers
{
    public class SepetUrunuController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public SepetUrunuController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sepetUrunleri = await _context.SepetUrunleri
                .Include(s => s.Sepet)
                .Include(s => s.Urun)
                .OrderByDescending(s => s.SepetUrunuID)
                .ToListAsync();
            return View(sepetUrunleri);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sepetUrunu = await _context.SepetUrunleri
                .Include(s => s.Sepet)
                .Include(s => s.Urun)
                .FirstOrDefaultAsync(m => m.SepetUrunuID == id);

            if (sepetUrunu == null)
            {
                return NotFound();
            }

            return View(sepetUrunu);
        }

        public IActionResult Create()
        {
            ViewBag.SepetID = new SelectList(_context.Sepetler, "SepetID", "SepetID");
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SepetUrunuID,SepetID,UrunID,Miktar,Fiyat")] SepetUrunu sepetUrunu)
        {
            if (ModelState.IsValid)
            {
                var urun = await _context.Urunler.FindAsync(sepetUrunu.UrunID);
                if (urun == null || urun.StokMiktari < sepetUrunu.Miktar)
                {
                    ModelState.AddModelError("", "Yetersiz stok!");
                    ViewBag.SepetID = new SelectList(_context.Sepetler, "SepetID", "SepetID", sepetUrunu.SepetID);
                    ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", sepetUrunu.UrunID);
                    return View(sepetUrunu);
                }

                sepetUrunu.Fiyat = urun.Fiyat;

                _context.Add(sepetUrunu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SepetID = new SelectList(_context.Sepetler, "SepetID", "SepetID", sepetUrunu.SepetID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", sepetUrunu.UrunID);
            return View(sepetUrunu);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sepetUrunu = await _context.SepetUrunleri.FindAsync(id);
            if (sepetUrunu == null)
            {
                return NotFound();
            }

            ViewBag.SepetID = new SelectList(_context.Sepetler, "SepetID", "SepetID", sepetUrunu.SepetID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", sepetUrunu.UrunID);
            return View(sepetUrunu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SepetUrunuID,SepetID,UrunID,Miktar,Fiyat")] SepetUrunu sepetUrunu)
        {
            if (id != sepetUrunu.SepetUrunuID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var urun = await _context.Urunler.FindAsync(sepetUrunu.UrunID);
                    if (urun == null || urun.StokMiktari < sepetUrunu.Miktar)
                    {
                        ModelState.AddModelError("", "Yetersiz stok!");
                        ViewBag.SepetID = new SelectList(_context.Sepetler, "SepetID", "SepetID", sepetUrunu.SepetID);
                        ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", sepetUrunu.UrunID);
                        return View(sepetUrunu);
                    }

                    // Ürünün güncel fiyatını al
                    sepetUrunu.Fiyat = urun.Fiyat;

                    _context.Update(sepetUrunu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SepetUrunuExists(sepetUrunu.SepetUrunuID))
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

            ViewBag.SepetID = new SelectList(_context.Sepetler, "SepetID", "SepetID", sepetUrunu.SepetID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", sepetUrunu.UrunID);
            return View(sepetUrunu);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sepetUrunu = await _context.SepetUrunleri
                .Include(s => s.Sepet)
                .Include(s => s.Urun)
                .FirstOrDefaultAsync(m => m.SepetUrunuID == id);

            if (sepetUrunu == null)
            {
                return NotFound();
            }

            return View(sepetUrunu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sepetUrunu = await _context.SepetUrunleri.FindAsync(id);
            if (sepetUrunu != null)
            {
                _context.SepetUrunleri.Remove(sepetUrunu);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MiktarGuncelle(int id, int yeniMiktar)
        {
            var sepetUrunu = await _context.SepetUrunleri
                .Include(s => s.Urun)
                .FirstOrDefaultAsync(s => s.SepetUrunuID == id);

            if (sepetUrunu == null)
            {
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }

            // Stok kontrolü
            if (sepetUrunu.Urun.StokMiktari < yeniMiktar)
            {
                return Json(new
                {
                    success = false,
                    message = "Yetersiz stok! Maksimum " + sepetUrunu.Urun.StokMiktari + " adet ekleyebilirsiniz."
                });
            }

            sepetUrunu.Miktar = yeniMiktar;
            await _context.SaveChangesAsync();

            decimal yeniToplam = sepetUrunu.Miktar * sepetUrunu.Fiyat;

            return Json(new
            {
                success = true,
                message = "Miktar güncellendi.",
                yeniToplam = yeniToplam,
                yeniToplamFormatted = yeniToplam.ToString("C")
            });
        }

        [HttpPost]
        public async Task<IActionResult> TopluGuncelle(List<SepetUrunuGuncelleme> guncellemeler)
        {
            try
            {
                foreach (var guncelleme in guncellemeler)
                {
                    var sepetUrunu = await _context.SepetUrunleri.FindAsync(guncelleme.SepetUrunuID);
                    if (sepetUrunu != null)
                    {
                        sepetUrunu.Miktar = guncelleme.YeniMiktar;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tüm ürünler güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Güncelleme sırasında hata oluştu: " + ex.Message });
            }
        }

        private async Task<decimal> SepetToplaminiHesapla(int sepetId)
        {
            return await _context.SepetUrunleri
                .Where(su => su.SepetID == sepetId)
                .SumAsync(su => su.Miktar * su.Fiyat);
        }

        [HttpGet]
        public async Task<JsonResult> GetSepetUrunSayisi(int sepetId)
        {
            var urunSayisi = await _context.SepetUrunleri
                .Where(su => su.SepetID == sepetId)
                .SumAsync(su => su.Miktar);

            return Json(new { urunSayisi = urunSayisi });
        }

        private bool SepetUrunuExists(int id)
        {
            return _context.SepetUrunleri.Any(e => e.SepetUrunuID == id);
        }
    }

    public class SepetUrunuGuncelleme
    {
        public int SepetUrunuID { get; set; }
        public int YeniMiktar { get; set; }
    }
}
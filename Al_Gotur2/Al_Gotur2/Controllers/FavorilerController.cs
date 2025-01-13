using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;

namespace Al_Gotur2.Controllers
{
    public class FavorilerController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public FavorilerController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var favoriler = await _context.Favoriler
                .Include(f => f.Kullanici)
                .Include(f => f.Urun)
                .ToListAsync();
            return View(favoriler);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favori = await _context.Favoriler
                .Include(f => f.Kullanici)
                .Include(f => f.Urun)
                .FirstOrDefaultAsync(m => m.FavoriID == id);

            if (favori == null)
            {
                return NotFound();
            }

            return View(favori);
        }

        public IActionResult Create()
        {
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad");
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FavoriID,KullaniciID,UrunID")] Favoriler favori)
        {
            if (ModelState.IsValid)
            {
                // Aynı ürün daha önce favorilere eklenmişse kontrol et
                var mevcutFavori = await _context.Favoriler
                    .FirstOrDefaultAsync(f => f.KullaniciID == favori.KullaniciID && f.UrunID == favori.UrunID);

                if (mevcutFavori != null)
                {
                    ModelState.AddModelError("", "Bu ürün zaten favorilerinizde bulunuyor.");
                }
                else
                {
                    _context.Add(favori);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", favori.KullaniciID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", favori.UrunID);
            return View(favori);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favori = await _context.Favoriler.FindAsync(id);
            if (favori == null)
            {
                return NotFound();
            }

            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", favori.KullaniciID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", favori.UrunID);
            return View(favori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FavoriID,KullaniciID,UrunID")] Favoriler favori)
        {
            if (id != favori.FavoriID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(favori);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FavoriExists(favori.FavoriID))
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

            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", favori.KullaniciID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", favori.UrunID);
            return View(favori);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favori = await _context.Favoriler
                .Include(f => f.Kullanici)
                .Include(f => f.Urun)
                .FirstOrDefaultAsync(m => m.FavoriID == id);

            if (favori == null)
            {
                return NotFound();
            }

            return View(favori);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var favori = await _context.Favoriler.FindAsync(id);
            if (favori != null)
            {
                _context.Favoriler.Remove(favori);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int urunId)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return Json(new { success = false, redirectToLogin = true, message = "Lütfen giriş yapın." });
            }

            try
            {
                var mevcutFavori = await _context.Favoriler
                    .FirstOrDefaultAsync(f => f.KullaniciID == kullaniciId && f.UrunID == urunId);

                if (mevcutFavori != null)
                {
                    _context.Favoriler.Remove(mevcutFavori);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, added = false });
                }
                else
                {
                    var yeniFavori = new Favoriler
                    {
                        KullaniciID = kullaniciId.Value,
                        UrunID = urunId,
                        EklenmeTarihi = DateTime.Now
                    };
                    _context.Favoriler.Add(yeniFavori);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, added = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> KullaniciFavorileri()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("GirisYap", "Kullanici", new { returnUrl = Request.Path });
            }

            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("GirisYap", "Kullanici");
            }

            try
            {
                var favoriler = await _context.Favoriler
                    .Include(f => f.Urun)
                        .ThenInclude(u => u.Kategori)
                    .Where(f => f.KullaniciID == kullaniciId)
                    .Select(f => new FavoriUrunViewModel
                    {
                        FavoriID = f.FavoriID,
                        UrunID = f.UrunID,
                        UrunAdi = f.Urun.UrunAdi,
                        Fiyat = f.Urun.Fiyat,
                        ResimUrl = f.Urun.ResimUrl,
                        KategoriAdi = f.Urun.Kategori.KategoriAdi,
                        EklenmeTarihi = f.EklenmeTarihi,
                        StokMiktari = f.Urun.StokMiktari
                    })
                    .OrderByDescending(f => f.EklenmeTarihi)
                    .ToListAsync();

                return View(favoriler);
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }

        private bool FavoriExists(int id)
        {
            return _context.Favoriler.Any(e => e.FavoriID == id);
        }
    }
}
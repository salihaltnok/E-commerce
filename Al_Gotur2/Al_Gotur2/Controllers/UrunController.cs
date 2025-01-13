 using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace Al_Gotur2.Controllers
{
    public class UrunController : Controller
    {
        private readonly Al_Gotur2Context _context;
        private readonly ILogger<UrunController> _logger;

        public UrunController(Al_Gotur2Context context, ILogger<UrunController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index(int? kategoriId, string siralama, string aramaMetni, int sayfa = 1)
        {
            try
            {
                var query = _context.Urunler
                    .Include(u => u.Kategori)
                    .AsQueryable();

                // Arama filtresi
                if (!string.IsNullOrEmpty(aramaMetni))
                {
                    query = query.Where(u => u.UrunAdi.Contains(aramaMetni) ||
                                           u.Aciklama.Contains(aramaMetni));
                }

                // Kategori filtresi
                if (kategoriId.HasValue)
                {
                    query = query.Where(u => u.KategoriID == kategoriId);
                }

                switch (siralama)
                {
                    case "fiyatArtan":
                        query = query.OrderBy(u => u.Fiyat);
                        break;
                    case "fiyatAzalan":
                        query = query.OrderByDescending(u => u.Fiyat);
                        break;
                    case "isimArtan":
                        query = query.OrderBy(u => u.UrunAdi);
                        break;
                    case "isimAzalan":
                        query = query.OrderByDescending(u => u.UrunAdi);
                        break;
                    default:
                        query = query.OrderBy(u => u.UrunAdi);
                        break;
                }

                int sayfaBoyutu = 12;
                int toplamUrun = query.Count();
                int toplamSayfa = (int)Math.Ceiling(toplamUrun / (double)sayfaBoyutu);

                var urunler = query
                    .Skip((sayfa - 1) * sayfaBoyutu)
                    .Take(sayfaBoyutu)
                    .ToList();

                ViewBag.Kategoriler = _context.Kategoriler.ToList();
                ViewBag.SecilenKategori = kategoriId;
                ViewBag.Siralama = siralama;
                ViewBag.AramaMetni = aramaMetni;
                ViewBag.Sayfa = sayfa;
                ViewBag.ToplamSayfa = toplamSayfa;

                return View(urunler);
            }
            catch (Exception ex)
            {
                // Hata logla
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }
        public async Task<IActionResult> Detay(int id)
        {
            var urun = await _context.Urunler
                .Include(u => u.Kategori)
                .Include(u => u.Yorumlar)
                    .ThenInclude(y => y.Kullanici)
                .FirstOrDefaultAsync(u => u.UrunID == id);

            if (urun == null)
            {
                return NotFound();
            }

            // Benzer ürünleri getir
            var benzerUrunler = await _context.Urunler
                .Where(u => u.KategoriID == urun.KategoriID && u.UrunID != urun.UrunID)
                .Take(4)
                .ToListAsync();

            ViewBag.BenzerUrunler = benzerUrunler;

            // Kullanıcı giriş kontrolü
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;

            // Eğer kullanıcı giriş yapmışsa favori durumunu kontrol et
            if (ViewBag.IsAuthenticated)
            {
                var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
                if (kullaniciId.HasValue)
                {
                    var isFavorite = await _context.Favoriler
                        .AnyAsync(f => f.UrunID == id && f.KullaniciID == kullaniciId.Value);
                    ViewBag.IsFavorite = isFavorite;
                }
                else
                {
                    ViewBag.IsFavorite = false;
                }
            }
            else
            {
                ViewBag.IsFavorite = false;
            }

            // Debug için konsola yazdırma
            Console.WriteLine($"IsAuthenticated: {ViewBag.IsAuthenticated}");
            Console.WriteLine($"IsFavorite: {ViewBag.IsFavorite}");

            return View(urun);
        }

        [HttpPost]
        public async Task<IActionResult> YorumEkle(int urunId, string yorumMetni, int puan)
        {
            try
            {
                var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
                if (!kullaniciId.HasValue)
                {
                    return Json(new { success = false, message = "Yorum yapmak için giriş yapmalısınız." });
                }

                var yeniYorum = new Yorum
                {
                    UrunID = urunId,
                    KullaniciID = kullaniciId.Value,
                    YorumMetni = yorumMetni,
                    Puan = puan,
                    YorumTarihi = DateTime.Now
                };

                _context.Yorumlar.Add(yeniYorum);
                await _context.SaveChangesAsync();

                TempData["Mesaj"] = "Yorumunuz başarıyla eklendi.";
                return Json(new { success = true, message = "Yorumunuz başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult YorumFormuGetir(int urunId)
        {
            if (User.Identity.IsAuthenticated)
            {
                return PartialView("_YorumFormu", urunId);
            }
            return PartialView("_GirisGerekli");
        }
        public async Task<IActionResult> KategoriyeGoreUrunler(int? kategoriId)
        {
            if (kategoriId == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler
                .FirstOrDefaultAsync(k => k.KategoriID == kategoriId);

            if (kategori == null)
            {
                return NotFound();
            }

            var urunler = await _context.Urunler
                .Where(u => u.KategoriID == kategoriId && u.IsActive)
                .OrderBy(u => u.UrunAdi)
                .ToListAsync();

            ViewBag.KategoriAdi = kategori.KategoriAdi;
            return View("Index", urunler);
        }

        [HttpPost]
        public async Task<IActionResult> HizliAra(string term)
        {
            var sonuclar = await _context.Urunler
                .Where(u => u.IsActive &&
                           (u.UrunAdi.Contains(term) ||
                            u.Aciklama.Contains(term)))
                .Select(u => new
                {
                    id = u.UrunID,
                    label = u.UrunAdi,
                    value = u.UrunAdi,
                    fiyat = u.Fiyat.ToString("C2"),
                    resim = u.ResimUrl
                })
                .Take(5)
                .ToListAsync();

            return Json(sonuclar);
        }

        [HttpGet]
        public async Task<IActionResult> StokDurumuRaporu()
        {
            var urunler = await _context.Urunler
                .Include(u => u.Kategori)
                .Where(u => u.IsActive)
                .OrderBy(u => u.StokMiktari)
                .ToListAsync();

            return View(urunler);
        }

        [HttpPost]
        public async Task<IActionResult> StokGuncelle(int urunId, int yeniStok)
        {
            try
            {
                var urun = await _context.Urunler.FindAsync(urunId);
                if (urun == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                urun.StokMiktari = yeniStok;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Stok başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Stok güncellenirken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FavoriEkle(int urunId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Favorilere eklemek için giriş yapmalısınız." });
            }

            try
            {
                var kullaniciId = int.Parse(User.FindFirst("KullaniciId").Value);
                var mevcutFavori = await _context.Favoriler
                    .FirstOrDefaultAsync(f => f.UrunID == urunId && f.KullaniciID == kullaniciId);

                if (mevcutFavori != null)
                {
                    return Json(new { success = false, message = "Bu ürün zaten favorilerinizde." });
                }

                var yeniFavori = new Favoriler
                {
                    UrunID = urunId,
                    KullaniciID = kullaniciId,
                    EklenmeTarihi = DateTime.Now
                };

                _context.Favoriler.Add(yeniFavori);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ürün favorilere eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FavoriKaldir(int urunId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "İşlem için giriş yapmalısınız." });
            }

            try
            {
                var kullaniciId = int.Parse(User.FindFirst("KullaniciId").Value);
                var favori = await _context.Favoriler
                    .FirstOrDefaultAsync(f => f.UrunID == urunId && f.KullaniciID == kullaniciId);

                if (favori == null)
                {
                    return Json(new { success = false, message = "Favori bulunamadı." });
                }

                _context.Favoriler.Remove(favori);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ürün favorilerden kaldırıldı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        private bool UrunExists(int id)
        {
            return _context.Urunler.Any(e => e.UrunID == id);
        }
    }
}
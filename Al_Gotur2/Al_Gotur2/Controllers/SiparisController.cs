using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;
using System.Text;

namespace Al_Gotur2.Controllers
{
    public class SiparisController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public SiparisController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var siparisler = await _context.Siparisler
                .Include(s => s.Kullanici)
                .Include(s => s.SiparisDetaylari)
                    .ThenInclude(sd => sd.Urun)
                .OrderByDescending(s => s.SiparisTarihi)
                .ToListAsync();

            return View(siparisler);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var siparis = await _context.Siparisler
                .Include(s => s.Kullanici)
                .Include(s => s.SiparisDetaylari)
                    .ThenInclude(sd => sd.Urun)
                .FirstOrDefaultAsync(m => m.SiparisID == id);

            if (siparis == null)
                return NotFound();

            return View(siparis);
        }

        public async Task<IActionResult> Create()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
                return RedirectToAction("Login", "Kullanici");
            var sepet = await _context.Sepetler
                .Include(s => s.SepetUrunleri)
                    .ThenInclude(su => su.Urun)
                .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

            if (sepet == null || !sepet.SepetUrunleri.Any())
            {
                TempData["Error"] = "Sepetiniz boş!";
                return RedirectToAction("Index", "Sepet");
            }

            ViewBag.Adresler = await _context.Adresler
                .Where(a => a.KullaniciID == kullaniciId)
                .ToListAsync();

            return View(new Siparis { KullaniciID = kullaniciId.Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SiparisID,KullaniciID,SiparisTarihi,Durum,ToplamTutar,TeslimatAdresi")] Siparis siparis)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Adresler = await _context.Adresler
                    .Where(a => a.KullaniciID == siparis.KullaniciID)
                    .ToListAsync();
                return View(siparis);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sepet = await _context.Sepetler
                    .Include(s => s.SepetUrunleri)
                        .ThenInclude(su => su.Urun)
                    .FirstOrDefaultAsync(s => s.KullaniciID == siparis.KullaniciID);

                if (sepet == null || !sepet.SepetUrunleri.Any())
                    throw new Exception("Sepet boş!");

                foreach (var sepetUrunu in sepet.SepetUrunleri)
                {
                    if (sepetUrunu.Urun.StokMiktari < sepetUrunu.Miktar)
                        throw new Exception($"{sepetUrunu.Urun.UrunAdi} için yeterli stok yok!");
                }

                siparis.SiparisTarihi = DateTime.Now;
                siparis.Durum = "Beklemede"; 
                siparis.ToplamTutar = sepet.ToplamTutar;

                _context.Add(siparis);
                await _context.SaveChangesAsync();

                foreach (var sepetUrunu in sepet.SepetUrunleri)
                {
                    var siparisDetayi = new SiparisDetayi
                    {
                        SiparisID = siparis.SiparisID,
                        UrunID = sepetUrunu.UrunID,
                        Miktar = sepetUrunu.Miktar,
                        Fiyat = sepetUrunu.Fiyat
                    };

                    _context.SiparisDetaylari.Add(siparisDetayi);

                    sepetUrunu.Urun.StokMiktari -= sepetUrunu.Miktar;
                    _context.Update(sepetUrunu.Urun);
                }

                _context.SepetUrunleri.RemoveRange(sepet.SepetUrunleri);
                _context.Sepetler.Remove(sepet);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = siparis.SiparisID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Sipariş oluşturulurken hata: " + ex.Message);

                ViewBag.Adresler = await _context.Adresler
                    .Where(a => a.KullaniciID == siparis.KullaniciID)
                    .ToListAsync();

                return View(siparis);
            }
        }
        [HttpPost]
        public async Task<IActionResult> DurumGuncelle(int id, string yeniDurum)
        {
            var siparis = await _context.Siparisler.FindAsync(id);
            if (siparis == null)
            {
                return Json(new { success = false, message = "Sipariş bulunamadı." });
            }

            siparis.Durum = yeniDurum;
            _context.Update(siparis);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sipariş durumu güncellendi." });
        }

        public async Task<IActionResult> Siparislerim()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("GirisYap", "Kullanici");
            }

            var siparisler = await _context.Siparisler
                .Include(s => s.SiparisDetaylari.Where(sd => !sd.IsDeleted))
                    .ThenInclude(sd => sd.Urun)
                .Where(s => s.KullaniciID == kullaniciId && !s.IsDeleted)
                .OrderByDescending(s => s.SiparisTarihi)
                .ToListAsync();

            return View(siparisler);
        }
        [HttpPost]
        public async Task<IActionResult> IptalEt(int id)
        {
            var siparis = await _context.Siparisler
                .Include(s => s.SiparisDetaylari)
                    .ThenInclude(sd => sd.Urun)
                .FirstOrDefaultAsync(s => s.SiparisID == id);

            if (siparis == null)
            {
                return Json(new { success = false, message = "Sipariş bulunamadı." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                siparis.Durum = "İptal Edildi";
                _context.Update(siparis);

                foreach (var detay in siparis.SiparisDetaylari)
                {
                    detay.Urun.StokMiktari += detay.Miktar;
                    _context.Update(detay.Urun);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Sipariş iptal edildi." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Sipariş iptali sırasında hata: " + ex.Message });
            }
        }
        public async Task<IActionResult> SiparisleriIndir()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("GirisYap", "Kullanici");
            }

            var siparisler = await _context.Siparisler
                .Where(s => s.KullaniciID == kullaniciId)
                .OrderByDescending(s => s.SiparisTarihi)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("SİPARİŞ GEÇMİŞİ");
            sb.AppendLine("================");
            sb.AppendLine();

            foreach (var siparis in siparisler)
            {
                sb.AppendLine($"Sipariş No: {siparis.SiparisID}");
                sb.AppendLine($"Tarih: {siparis.SiparisTarihi:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Durum: {siparis.Durum}");
                sb.AppendLine("Ürünler:");
                sb.AppendLine("--------");

                // Sakli Procedure'ü çağır
                var siparisDetaylari = await _context.SiparisDetaylari
     .Include(sd => sd.Urun)
     .Where(sd => sd.SiparisID == siparis.SiparisID)
     .Select(sd => new SiparisDetayViewModel
     {
         SiparisDetayiID = sd.SiparisDetayID,
         SiparisID = sd.SiparisID,
         UrunID = sd.UrunID,
         UrunAdi = sd.Urun.UrunAdi,
         Miktar = sd.Miktar,
         BirimFiyat = sd.Fiyat,
         ToplamFiyat = sd.Miktar * sd.Fiyat
     })
     .ToListAsync();

                foreach (var detay in siparisDetaylari)
                {
                    sb.AppendLine($"- {detay.UrunAdi}");
                    sb.AppendLine($"  Miktar: {detay.Miktar} adet");
                    sb.AppendLine($"  Birim Fiyat: {detay.BirimFiyat:C}");
                    sb.AppendLine($"  Toplam: {detay.ToplamFiyat:C}");
                    sb.AppendLine();
                }

                sb.AppendLine($"Sipariş Toplam Tutarı: {siparis.ToplamTutar:C}");
                sb.AppendLine("================");
                sb.AppendLine();
            }

            string fileName = $"Siparislerim_{DateTime.Now:yyyyMMdd}.txt";
            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(fileBytes, "text/plain", fileName);
        }

        public class SiparisDetayViewModel
        {
            public int SiparisDetayiID { get; set; }
            public int SiparisID { get; set; }
            public int UrunID { get; set; }
            public string UrunAdi { get; set; }
            public int Miktar { get; set; }
            public decimal BirimFiyat { get; set; }
            public decimal ToplamFiyat { get; set; }
        }
        public async Task<IActionResult> SiparisTakip(int? id)
        {
            if (id == null)
                return NotFound();

            var siparis = await _context.Siparisler
                .Include(s => s.SiparisDetaylari)
                    .ThenInclude(sd => sd.Urun)
                .Include(s => s.Kullanici)
                .FirstOrDefaultAsync(m => m.SiparisID == id);

            if (siparis == null)
                return NotFound();

            return View(siparis);
        }

        private bool SiparisExists(int id)
        {
            return _context.Siparisler.Any(e => e.SiparisID == id);
        }
    }
}
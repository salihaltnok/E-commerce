using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Al_Gotur2.Controllers
{
    public class KullaniciController : Controller
    {
        private readonly Al_Gotur2Context _context;

        private const string SessionKeyKullaniciID = "KullaniciID";
        private const string SessionKeyKullaniciAdi = "KullaniciAdi";
        private const string SessionKeyIsAdmin = "IsAdmin";

        public KullaniciController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public IActionResult GirisYap()
        {
            if (HttpContext.Session.GetInt32(SessionKeyKullaniciID).HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GirisYap(string email, string sifre)
        {
            var hashedSifre = HashPassword(sifre);
            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(k => k.Email == email && k.Sifre == hashedSifre && k.IsActive);

            if (kullanici == null)
            {
                TempData["Hata"] = "Email veya şifre hatalı!";
                return View();
            }

            HttpContext.Session.SetInt32(SessionKeyKullaniciID, kullanici.KullaniciID);
            HttpContext.Session.SetString(SessionKeyKullaniciAdi, kullanici.KullaniciAdi);
            HttpContext.Session.SetString(SessionKeyIsAdmin, kullanici.IsAdmin.ToString());

            if (kullanici.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult KayitOl()
        {
            if (HttpContext.Session.GetInt32(SessionKeyKullaniciID).HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KayitOl([Bind("KullaniciAdi,Email,Sifre,AdSoyad")] Kullanici kullanici)
        {
            if (ModelState.IsValid)
            {
                var emailKontrol = await _context.Kullanicilar
                    .AnyAsync(k => k.Email == kullanici.Email);

                if (emailKontrol)
                {
                    ModelState.AddModelError("Email", "Bu email adresi zaten kayıtlı!");
                    return View(kullanici);
                }

                kullanici.Sifre = HashPassword(kullanici.Sifre);
                kullanici.KayitTarihi = DateTime.Now;
                kullanici.IsActive = true;
                kullanici.IsAdmin = false;

                _context.Kullanicilar.Add(kullanici);
                await _context.SaveChangesAsync();

                TempData["Basari"] = "Kayıt başarıyla oluşturuldu. Giriş yapabilirsiniz.";
                return RedirectToAction("GirisYap");
            }

            return View(kullanici);
        }

        public IActionResult CikisYap()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profil()
        {
            var kullaniciId = HttpContext.Session.GetInt32(SessionKeyKullaniciID);
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("GirisYap");
            }

            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(k => k.KullaniciID == kullaniciId);

            if (kullanici == null)
            {
                return NotFound();
            }

            return View(kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfilGuncelle([Bind("KullaniciID,KullaniciAdi,Email,AdSoyad")] Kullanici kullanici)
        {
            var kullaniciId = HttpContext.Session.GetInt32(SessionKeyKullaniciID);
            if (!kullaniciId.HasValue || kullaniciId != kullanici.KullaniciID)
            {
                return RedirectToAction("GirisYap");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var mevcutKullanici = await _context.Kullanicilar.FindAsync(kullanici.KullaniciID);
                    if (mevcutKullanici == null)
                    {
                        return NotFound();
                    }

                    if (mevcutKullanici.Email != kullanici.Email)
                    {
                        var emailKontrol = await _context.Kullanicilar
                            .AnyAsync(k => k.Email == kullanici.Email && k.KullaniciID != kullanici.KullaniciID);

                        if (emailKontrol)
                        {
                            ModelState.AddModelError("Email", "Bu email adresi zaten kullanılıyor!");
                            return View("Profil", kullanici);
                        }
                    }

                    mevcutKullanici.KullaniciAdi = kullanici.KullaniciAdi;
                    mevcutKullanici.Email = kullanici.Email;
                    mevcutKullanici.AdSoyad = kullanici.AdSoyad;

                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetString(SessionKeyKullaniciAdi, kullanici.KullaniciAdi);

                    TempData["Basari"] = "Profiliniz başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KullaniciExists(kullanici.KullaniciID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Profil));
            }
            return View("Profil", kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreDegistir(string mevcutSifre, string yeniSifre)
        {
            var kullaniciId = HttpContext.Session.GetInt32(SessionKeyKullaniciID);
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("GirisYap");
            }

            var kullanici = await _context.Kullanicilar.FindAsync(kullaniciId.Value);
            if (kullanici == null)
            {
                return NotFound();
            }

            if (kullanici.Sifre != HashPassword(mevcutSifre))
            {
                TempData["Hata"] = "Mevcut şifreniz hatalı!";
                return RedirectToAction("Profil");
            }

            kullanici.Sifre = HashPassword(yeniSifre);
            await _context.SaveChangesAsync();

            TempData["Basari"] = "Şifreniz başarıyla değiştirildi.";
            return RedirectToAction("Profil");
        }

        private bool KullaniciExists(int id)
        {
            return _context.Kullanicilar.Any(e => e.KullaniciID == id);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
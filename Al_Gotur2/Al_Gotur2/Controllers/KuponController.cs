using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;

namespace Al_Gotur2.Controllers
{
    public class KuponController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public KuponController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kuponlar = await _context.Kuponlar
                .OrderByDescending(k => k.GecerlilikTarihi)
                .ToListAsync();
            return View(kuponlar);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kupon = await _context.Kuponlar
                .FirstOrDefaultAsync(m => m.KuponID == id);

            if (kupon == null)
            {
                return NotFound();
            }

            return View(kupon);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KuponID,Kod,IndirimOrani,GecerlilikTarihi")] Kupon kupon)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Kuponlar.AnyAsync(k => k.Kod == kupon.Kod))
                {
                    ModelState.AddModelError("Kod", "Bu kupon kodu zaten kullanılıyor.");
                    return View(kupon);
                }

                _context.Add(kupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(kupon);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kupon = await _context.Kuponlar.FindAsync(id);
            if (kupon == null)
            {
                return NotFound();
            }
            return View(kupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KuponID,Kod,IndirimOrani,GecerlilikTarihi")] Kupon kupon)
        {
            if (id != kupon.KuponID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (await _context.Kuponlar.AnyAsync(k => k.Kod == kupon.Kod && k.KuponID != kupon.KuponID))
                {
                    ModelState.AddModelError("Kod", "Bu kupon kodu zaten kullanılıyor.");
                    return View(kupon);
                }

                try
                {
                    _context.Update(kupon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KuponExists(kupon.KuponID))
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
            return View(kupon);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kupon = await _context.Kuponlar
                .FirstOrDefaultAsync(m => m.KuponID == id);

            if (kupon == null)
            {
                return NotFound();
            }

            return View(kupon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kupon = await _context.Kuponlar.FindAsync(id);
            if (kupon != null)
            {
                _context.Kuponlar.Remove(kupon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> KuponKontrol(string kuponKodu)
        {
            var kupon = await _context.Kuponlar
                .FirstOrDefaultAsync(k => k.Kod == kuponKodu && k.GecerlilikTarihi > DateTime.Now);

            if (kupon == null)
            {
                return Json(new { isValid = false, message = "Geçersiz kupon kodu." });
            }

            return Json(new
            {
                isValid = true,
                indirimOrani = kupon.IndirimOrani,
                message = $"Kupon uygulandı. İndirim oranı: %{kupon.IndirimOrani}"
            });
        }

        public async Task<IActionResult> AktifKuponlar()
        {
            var aktifKuponlar = await _context.Kuponlar
                .Where(k => k.GecerlilikTarihi > DateTime.Now)
                .OrderBy(k => k.GecerlilikTarihi)
                .ToListAsync();

            return View(aktifKuponlar);
        }

        public async Task<IActionResult> GecmisKuponlar()
        {
            var gecmisKuponlar = await _context.Kuponlar
                .Where(k => k.GecerlilikTarihi <= DateTime.Now)
                .OrderByDescending(k => k.GecerlilikTarihi)
                .ToListAsync();

            return View(gecmisKuponlar);
        }

        [HttpPost]
        public IActionResult KuponKoduOlustur()
        {
            string kod = GenerateRandomCouponCode();
            return Json(new { kod = kod });
        }

        private bool KuponExists(int id)
        {
            return _context.Kuponlar.Any(e => e.KuponID == id);
        }

        private string GenerateRandomCouponCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Al_Gotur2.Models;

namespace Al_Gotur2.Controllers
{
    public class KargoController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public KargoController(Al_Gotur2Context context)
        {
            _context = context;
        }

        // GET: Kargo
        public async Task<IActionResult> Index()
        {
            var kargolar = await _context.Kargolar
                .Include(k => k.Siparis)
                .OrderByDescending(k => k.KargoTarihi)
                .ToListAsync();
            return View(kargolar);
        }

        // GET: Kargo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kargo = await _context.Kargolar
                .Include(k => k.Siparis)
                .FirstOrDefaultAsync(m => m.KargoID == id);

            if (kargo == null)
            {
                return NotFound();
            }

            return View(kargo);
        }

        // GET: Kargo/Create
        public IActionResult Create()
        {
            // Sadece kargoya verilmemiş siparişleri listele
            var siparisler = _context.Siparisler
                .Where(s => !_context.Kargolar.Any(k => k.SiparisID == s.SiparisID))
                .ToList();
            ViewBag.SiparisID = new SelectList(siparisler, "SiparisID", "SiparisID");
            return View();
        }

        // POST: Kargo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KargoID,SiparisID,KargoTarihi,TahminiTeslimTarihi,KargoDurumu")] Kargo kargo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(kargo);
                await _context.SaveChangesAsync();

                // Siparişin durumunu güncelle
                var siparis = await _context.Siparisler.FindAsync(kargo.SiparisID);
                if (siparis != null)
                {
                    siparis.Durum = "Kargoya Verildi";
                    _context.Update(siparis);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.SiparisID = new SelectList(_context.Siparisler, "SiparisID", "SiparisID", kargo.SiparisID);
            return View(kargo);
        }

        // GET: Kargo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kargo = await _context.Kargolar.FindAsync(id);
            if (kargo == null)
            {
                return NotFound();
            }

            ViewBag.SiparisID = new SelectList(_context.Siparisler, "SiparisID", "SiparisID", kargo.SiparisID);
            return View(kargo);
        }

        // POST: Kargo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KargoID,SiparisID,KargoTarihi,TahminiTeslimTarihi,KargoDurumu")] Kargo kargo)
        {
            if (id != kargo.KargoID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(kargo);
                    await _context.SaveChangesAsync();

                    // Kargo durumuna göre sipariş durumunu güncelle
                    var siparis = await _context.Siparisler.FindAsync(kargo.SiparisID);
                    if (siparis != null)
                    {
                        switch (kargo.KargoDurumu.ToLower())
                        {
                            case "teslim edildi":
                                siparis.Durum = "Tamamlandı";
                                break;
                            case "yolda":
                                siparis.Durum = "Kargoda";
                                break;
                            case "iade":
                                siparis.Durum = "İade Edildi";
                                break;
                        }
                        _context.Update(siparis);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KargoExists(kargo.KargoID))
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

            ViewBag.SiparisID = new SelectList(_context.Siparisler, "SiparisID", "SiparisID", kargo.SiparisID);
            return View(kargo);
        }

        // GET: Kargo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kargo = await _context.Kargolar
                .Include(k => k.Siparis)
                .FirstOrDefaultAsync(m => m.KargoID == id);

            if (kargo == null)
            {
                return NotFound();
            }

            return View(kargo);
        }

        // POST: Kargo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kargo = await _context.Kargolar.FindAsync(id);
            if (kargo != null)
            {
                _context.Kargolar.Remove(kargo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Kargo Takip
        public async Task<IActionResult> KargoTakip(int? siparisId)
        {
            if (siparisId == null)
            {
                return NotFound();
            }

            var kargo = await _context.Kargolar
                .Include(k => k.Siparis)
                .FirstOrDefaultAsync(k => k.SiparisID == siparisId);

            if (kargo == null)
            {
                return NotFound();
            }

            return View(kargo);
        }

        // Teslim Edilmeyen Kargolar
        public async Task<IActionResult> TeslimEdilmeyenKargolar()
        {
            var kargolar = await _context.Kargolar
                .Include(k => k.Siparis)
                .Where(k => k.KargoDurumu != "Teslim Edildi")
                .OrderBy(k => k.TahminiTeslimTarihi)
                .ToListAsync();

            return View(kargolar);
        }

        // Geciken Kargolar
        public async Task<IActionResult> GecikenKargolar()
        {
            var bugun = DateTime.Now;
            var kargolar = await _context.Kargolar
                .Include(k => k.Siparis)
                .Where(k => k.TahminiTeslimTarihi < bugun && k.KargoDurumu != "Teslim Edildi")
                .OrderBy(k => k.TahminiTeslimTarihi)
                .ToListAsync();

            return View(kargolar);
        }

        // Kargo Durumu Güncelle (AJAX için)
        [HttpPost]
        public async Task<IActionResult> DurumGuncelle(int kargoId, string yeniDurum)
        {
            var kargo = await _context.Kargolar.FindAsync(kargoId);
            if (kargo == null)
            {
                return Json(new { success = false, message = "Kargo bulunamadı." });
            }

            kargo.KargoDurumu = yeniDurum;
            _context.Update(kargo);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Kargo durumu güncellendi." });
        }

        private bool KargoExists(int id)
        {
            return _context.Kargolar.Any(e => e.KargoID == id);
        }
    }
}
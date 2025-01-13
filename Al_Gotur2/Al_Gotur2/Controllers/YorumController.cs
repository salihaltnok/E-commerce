using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Al_Gotur2.Controllers
{
    public class YorumController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public YorumController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var yorumlar = await _context.Yorumlar
                .Include(y => y.Kullanici)
                .Include(y => y.Urun)
                .ToListAsync();
            return View(yorumlar);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yorum = await _context.Yorumlar
                .Include(y => y.Kullanici)
                .Include(y => y.Urun)
                .FirstOrDefaultAsync(m => m.YorumID == id);

            if (yorum == null)
            {
                return NotFound();
            }

            return View(yorum);
        }

        public IActionResult Create()
        {
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad");
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("YorumID,KullaniciID,UrunID,YorumMetni,Puan")] Yorum yorum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(yorum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", yorum.KullaniciID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", yorum.UrunID);
            return View(yorum);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yorum = await _context.Yorumlar.FindAsync(id);
            if (yorum == null)
            {
                return NotFound();
            }
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", yorum.KullaniciID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", yorum.UrunID);
            return View(yorum);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("YorumID,KullaniciID,UrunID,YorumMetni,Puan")] Yorum yorum)
        {
            if (id != yorum.YorumID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(yorum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!YorumExists(yorum.YorumID))
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
            ViewBag.KullaniciID = new SelectList(_context.Kullanicilar, "KullaniciID", "AdSoyad", yorum.KullaniciID);
            ViewBag.UrunID = new SelectList(_context.Urunler, "UrunID", "UrunAdi", yorum.UrunID);
            return View(yorum);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yorum = await _context.Yorumlar
                .Include(y => y.Kullanici)
                .Include(y => y.Urun)
                .FirstOrDefaultAsync(m => m.YorumID == id);

            if (yorum == null)
            {
                return NotFound();
            }

            return View(yorum);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yorum = await _context.Yorumlar.FindAsync(id);
            if (yorum != null)
            {
                _context.Yorumlar.Remove(yorum);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> UruneGoreYorumlar(int? urunId)
        {
            if (urunId == null)
            {
                return NotFound();
            }

            var yorumlar = await _context.Yorumlar
                .Include(y => y.Kullanici)
                .Include(y => y.Urun)
                .Where(y => y.UrunID == urunId)
                .ToListAsync();

            ViewBag.UrunAdi = await _context.Urunler
                .Where(u => u.UrunID == urunId)
                .Select(u => u.UrunAdi)
                .FirstOrDefaultAsync();

            return View(yorumlar);
        }

        public async Task<IActionResult> KullanicininYorumlari(int? kullaniciId)
        {
            if (kullaniciId == null)
            {
                return NotFound();
            }

            var yorumlar = await _context.Yorumlar
                .Include(y => y.Kullanici)
                .Include(y => y.Urun)
                .Where(y => y.KullaniciID == kullaniciId)
                .ToListAsync();

            return View(yorumlar);
        }

        private bool YorumExists(int id)
        {
            return _context.Yorumlar.Any(e => e.YorumID == id);
        }
    }
}
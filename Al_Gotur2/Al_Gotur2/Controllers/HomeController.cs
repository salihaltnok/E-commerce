using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;

namespace Al_Gotur2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Al_Gotur2Context _context;

        public HomeController(ILogger<HomeController> logger, Al_Gotur2Context context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Index()
        {
            var enCokSatanUrunler = _context.SiparisDetaylari
                .GroupBy(sd => sd.UrunID)
                .Select(group => new
                {
                    UrunID = group.Key,
                    ToplamSatisMiktari = group.Sum(sd => sd.Miktar)
                })
                .OrderByDescending(x => x.ToplamSatisMiktari)
                .Take(4)
                .Join(
                    _context.Urunler.Include(u => u.Kategori),
                    satislar => satislar.UrunID,
                    urun => urun.UrunID,
                    (satislar, urun) => new EnCokSatanUrunViewModel
                    {
                        UrunID = urun.UrunID,
                        UrunAdi = urun.UrunAdi,
                        Fiyat = urun.Fiyat,
                        ResimUrl = urun.ResimUrl,
                        Aciklama = urun.Aciklama,
                        KategoriAdi = urun.Kategori.KategoriAdi,
                        ToplamSatisMiktari = satislar.ToplamSatisMiktari,
                        StokMiktari = urun.StokMiktari
                    }
                )
                .ToList();

            var viewModel = new AnaSayfaViewModel
            {
                OneCikanUrunler = _context.Urunler
                    .Include(u => u.Kategori)
                    .Select(u => new EnCokSatanUrunViewModel
                    {
                        UrunID = u.UrunID,
                        UrunAdi = u.UrunAdi,
                        Fiyat = u.Fiyat,
                        ResimUrl = u.ResimUrl,
                        Aciklama = u.Aciklama,
                        KategoriAdi = u.Kategori.KategoriAdi,
                        StokMiktari = u.StokMiktari
                    })
                    .Take(4)
                    .ToList(),

                EnCokSatanUrunler = enCokSatanUrunler
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
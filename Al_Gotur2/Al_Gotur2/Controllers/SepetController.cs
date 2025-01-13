using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Microsoft.Data.SqlClient;

namespace Al_Gotur2.Controllers
{
    public class SepetController : Controller
    {
        private readonly Al_Gotur2Context _context;

        public SepetController(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("Login", "Kullanici");
            }

            var sepet = await _context.Sepetler
                .Include(s => s.SepetUrunleri)
                    .ThenInclude(su => su.Urun)
                .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

            if (sepet != null && sepet.OrijinalTutar == 0)
            {
                sepet.OrijinalTutar = sepet.SepetUrunleri.Sum(su => su.Miktar * su.Fiyat);
                sepet.UygulananIndirimOrani = 5;
                sepet.ToplamTutar = sepet.OrijinalTutar * 0.95m;
                await _context.SaveChangesAsync();
            }

            return View(sepet);
        }

        [HttpPost]
        public async Task<IActionResult> UrunEkle(int urunId, int miktar = 1)
        {
            var kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");

            if (string.IsNullOrEmpty(kullaniciAdi) || !kullaniciId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen giriş yapın.", redirectToLogin = true });
            }

            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string checkStockSql = "SELECT StokMiktari, Fiyat FROM Urunler WHERE UrunID = @UrunID";
                            decimal urunFiyat;
                            int stokMiktari;

                            using (var cmd = new SqlCommand(checkStockSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@UrunID", urunId);
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    if (!reader.Read())
                                    {
                                        return Json(new { success = false, message = "Ürün bulunamadı." });
                                    }
                                    stokMiktari = reader.GetInt32(0);
                                    urunFiyat = reader.GetDecimal(1);
                                }
                            }

                            if (stokMiktari < miktar)
                            {
                                return Json(new { success = false, message = $"Yetersiz stok. Mevcut stok: {stokMiktari}" });
                            }

                            string checkCartSql = "SELECT SepetID FROM Sepetler WHERE KullaniciID = @KullaniciID";
                            int sepetId;

                            using (var cmd = new SqlCommand(checkCartSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId.Value);
                                var result = await cmd.ExecuteScalarAsync();

                                if (result == null)
                                {
                                    string createCartSql = @"
                                INSERT INTO Sepetler (KullaniciID, ToplamTutar) 
                                VALUES (@KullaniciID, 0);
                                SELECT SCOPE_IDENTITY();";

                                    using (var createCmd = new SqlCommand(createCartSql, connection, transaction))
                                    {
                                        createCmd.Parameters.AddWithValue("@KullaniciID", kullaniciId.Value);
                                        sepetId = Convert.ToInt32(await createCmd.ExecuteScalarAsync());
                                    }
                                }
                                else
                                {
                                    sepetId = Convert.ToInt32(result);
                                }
                            }

                            string checkCartItemSql = "SELECT Miktar FROM SepetUrunleri WHERE SepetID = @SepetID AND UrunID = @UrunID";
                            using (var cmd = new SqlCommand(checkCartItemSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SepetID", sepetId);
                                cmd.Parameters.AddWithValue("@UrunID", urunId);
                                var existingMiktar = await cmd.ExecuteScalarAsync();

                                if (existingMiktar != null)
                                {
                                    string updateCartItemSql = @"
                                UPDATE SepetUrunleri 
                                SET Miktar = Miktar + @Miktar 
                                WHERE SepetID = @SepetID AND UrunID = @UrunID";

                                    using (var updateCmd = new SqlCommand(updateCartItemSql, connection, transaction))
                                    {
                                        updateCmd.Parameters.AddWithValue("@Miktar", miktar);
                                        updateCmd.Parameters.AddWithValue("@SepetID", sepetId);
                                        updateCmd.Parameters.AddWithValue("@UrunID", urunId);
                                        await updateCmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    string insertCartItemSql = @"
                                INSERT INTO SepetUrunleri (SepetID, UrunID, Miktar, Fiyat) 
                                VALUES (@SepetID, @UrunID, @Miktar, @Fiyat)";

                                    using (var insertCmd = new SqlCommand(insertCartItemSql, connection, transaction))
                                    {
                                        insertCmd.Parameters.AddWithValue("@SepetID", sepetId);
                                        insertCmd.Parameters.AddWithValue("@UrunID", urunId);
                                        insertCmd.Parameters.AddWithValue("@Miktar", miktar);
                                        insertCmd.Parameters.AddWithValue("@Fiyat", urunFiyat);
                                        await insertCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            string updateStockSql = "UPDATE Urunler SET StokMiktari = StokMiktari - @Miktar WHERE UrunID = @UrunID";
                            using (var cmd = new SqlCommand(updateStockSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Miktar", miktar);
                                cmd.Parameters.AddWithValue("@UrunID", urunId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            string updateCartTotalSql = @"
                        UPDATE Sepetler 
                        SET ToplamTutar = (
                            SELECT SUM(Miktar * Fiyat) 
                            FROM SepetUrunleri 
                            WHERE SepetID = @SepetID
                        ) 
                        WHERE SepetID = @SepetID";

                            using (var cmd = new SqlCommand(updateCartTotalSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SepetID", sepetId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();

                            return Json(new
                            {
                                success = true,
                                message = "Ürün sepete eklendi.",
                                sepetUrunSayisi = miktar,
                                kalanStok = stokMiktari - miktar
                            });
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UrunGuncelle(int sepetUrunuId, int yeniMiktar)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen giriş yapın." });
            }

            try
            {
                var sepetUrunu = await _context.SepetUrunleri
                    .Include(su => su.Urun)
                    .FirstOrDefaultAsync(su => su.SepetUrunuID == sepetUrunuId);

                if (sepetUrunu == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                if (sepetUrunu.Urun.StokMiktari < yeniMiktar)
                {
                    return Json(new { success = false, message = "Yetersiz stok." });
                }

                if (yeniMiktar <= 0)
                {
                    _context.SepetUrunleri.Remove(sepetUrunu);
                }
                else
                {
                    sepetUrunu.Miktar = yeniMiktar;
                }

                await _context.SaveChangesAsync();

                var sepet = await _context.Sepetler
                    .Include(s => s.SepetUrunleri)
                    .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

                return Json(new
                {
                    success = true,
                    message = "Sepet güncellendi.",
                    yeniToplamTutar = sepet.ToplamTutar.ToString("C"),
                    sepetUrunSayisi = sepet.SepetUrunleri.Sum(su => su.Miktar)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UrunSil(int sepetUrunuId)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen giriş yapın." });
            }

            try
            {
                var sepetUrunu = await _context.SepetUrunleri.FindAsync(sepetUrunuId);
                if (sepetUrunu != null)
                {
                    _context.SepetUrunleri.Remove(sepetUrunu);
                    await _context.SaveChangesAsync();
                }

                var sepet = await _context.Sepetler
                    .Include(s => s.SepetUrunleri)
                    .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

                return Json(new
                {
                    success = true,
                    message = "Ürün sepetten silindi.",
                    yeniToplamTutar = sepet.ToplamTutar.ToString("C"),
                    sepetUrunSayisi = sepet.SepetUrunleri.Sum(su => su.Miktar)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SepetTamamla()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("Login", "Kullanici");
            }

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int sepetId;
                        decimal orijinalTutar, indirimliTutar;
                        var sepetBilgileri = new List<(int UrunID, int Miktar, decimal Fiyat)>();

                        using (var cmd = new SqlCommand(@"
                    SELECT s.SepetID, s.OrijinalTutar, s.ToplamTutar, su.UrunID, su.Miktar, su.Fiyat
                    FROM Sepetler s
                    JOIN SepetUrunleri su ON s.SepetID = su.SepetID
                    WHERE s.KullaniciID = @KullaniciID", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId.Value);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (!reader.Read())
                                {
                                    return RedirectToAction("Index", "Home");
                                }

                                sepetId = reader.GetInt32(0);
                                orijinalTutar = reader.GetDecimal(1);
                                indirimliTutar = reader.GetDecimal(2);

                                do
                                {
                                    sepetBilgileri.Add((
                                        reader.GetInt32(3),
                                        reader.GetInt32(4),
                                        reader.GetDecimal(5)
                                    ));
                                } while (reader.Read());
                            }
                        }

                        // Yeni sipariş oluştur (indirimli fiyatla)
                        int siparisId;
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO Siparisler (KullaniciID, SiparisTarihi, ToplamTutar, OrijinalTutar, UygulananIndirim, Durum, IsDeleted)
                    OUTPUT INSERTED.SiparisID
                    VALUES (@KullaniciID, GETDATE(), @ToplamTutar, @OrijinalTutar, 5, 'Onay Bekliyor', 0)", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId.Value);
                            cmd.Parameters.AddWithValue("@ToplamTutar", indirimliTutar);
                            cmd.Parameters.AddWithValue("@OrijinalTutar", orijinalTutar);
                            siparisId = (int)await cmd.ExecuteScalarAsync();
                        }

                        // Sipariş detaylarını oluştur
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO SiparisDetaylari (SiparisID, UrunID, Miktar, Fiyat, IndirimliFiyat, IsDeleted)
                    VALUES (@SiparisID, @UrunID, @Miktar, @Fiyat, @IndirimliFiyat, 0)", connection, transaction))
                        {
                            var siparisIdParam = cmd.Parameters.Add("@SiparisID", System.Data.SqlDbType.Int);
                            var urunIdParam = cmd.Parameters.Add("@UrunID", System.Data.SqlDbType.Int);
                            var miktarParam = cmd.Parameters.Add("@Miktar", System.Data.SqlDbType.Int);
                            var fiyatParam = cmd.Parameters.Add("@Fiyat", System.Data.SqlDbType.Decimal);
                            var indirimliFiyatParam = cmd.Parameters.Add("@IndirimliFiyat", System.Data.SqlDbType.Decimal);

                            foreach (var (urunId, miktar, fiyat) in sepetBilgileri)
                            {
                                siparisIdParam.Value = siparisId;
                                urunIdParam.Value = urunId;
                                miktarParam.Value = miktar;
                                fiyatParam.Value = fiyat;
                                indirimliFiyatParam.Value = fiyat * 0.95m; 
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        using (var cmd = new SqlCommand(@"
                    UPDATE Urunler 
                    SET StokMiktari = StokMiktari - @Miktar 
                    WHERE UrunID = @UrunID", connection, transaction))
                        {
                            var urunIdParam = cmd.Parameters.Add("@UrunID", System.Data.SqlDbType.Int);
                            var miktarParam = cmd.Parameters.Add("@Miktar", System.Data.SqlDbType.Int);

                            foreach (var (urunId, miktar, _) in sepetBilgileri)
                            {
                                urunIdParam.Value = urunId;
                                miktarParam.Value = miktar;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        using (var cmd = new SqlCommand(@"
                    DELETE FROM SepetUrunleri WHERE SepetID = @SepetID;
                    DELETE FROM Sepetler WHERE SepetID = @SepetID;", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@SepetID", sepetId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();

                        TempData["Mesaj"] = "Siparişiniz başarıyla oluşturuldu!";
                        return RedirectToAction("Index", "Home");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        TempData["Hata"] = "Sipariş oluşturulurken bir hata oluştu: " + ex.Message;
                        return RedirectToAction("Index");
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Temizle()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen giriş yapın." });
            }

            try
            {
                var sepetUrunleri = await _context.SepetUrunleri
                    .Where(su => su.Sepet.KullaniciID == kullaniciId)
                    .ToListAsync();

                if (sepetUrunleri.Any())
                {
                    _context.SepetUrunleri.RemoveRange(sepetUrunleri);
                    await _context.SaveChangesAsync();
                }

                var sepet = await _context.Sepetler
                    .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

                if (sepet != null)
                {
                    _context.Sepetler.Remove(sepet);
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.Remove("Sepet");

                return Json(new { success = true, message = "Sepet başarıyla temizlendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        public async Task<IActionResult> Ozet()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return RedirectToAction("Login", "Kullanici");
            }

            var sepet = await _context.Sepetler
                .Include(s => s.SepetUrunleri)
                    .ThenInclude(su => su.Urun)
                .Include(s => s.Kullanici)
                    .ThenInclude(k => k.Adresler)
                .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

            if (sepet != null)
            {
                sepet.OrijinalTutar = sepet.SepetUrunleri.Sum(su => su.Miktar * su.Fiyat);
                sepet.ToplamTutar = sepet.OrijinalTutar * 0.95m;
                sepet.UygulananIndirimOrani = 5;

                await _context.SaveChangesAsync();
            }

            return View(sepet);
        }

        [HttpPost]
        public async Task<IActionResult> KuponUygula(string kuponKodu)
        {
            try
            {
                if (string.IsNullOrEmpty(kuponKodu))
                {
                    return Json(new { success = false, message = "Kupon kodu boş olamaz." });
                }

                var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
                if (!kullaniciId.HasValue)
                {
                    return Json(new { success = false, message = "Lütfen giriş yapın." });
                }

                var kupon = await _context.Kuponlar
                    .Where(k => k.Kod == kuponKodu && k.Aktif && k.GecerlilikTarihi > DateTime.Now)
                    .FirstOrDefaultAsync();

                if (kupon == null)
                {
                    return Json(new { success = false, message = "Geçersiz veya süresi dolmuş kupon kodu." });
                }

                var sepet = await _context.Sepetler
                    .Include(s => s.SepetUrunleri)
                    .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

                if (sepet == null || !sepet.SepetUrunleri.Any())
                {
                    return Json(new { success = false, message = "Sepetiniz boş." });
                }

                decimal indirimOrani = kupon.IndirimOrani / 100m;
                decimal indirimMiktari = sepet.ToplamTutar * indirimOrani;
                decimal yeniToplam = sepet.ToplamTutar - indirimMiktari;

                sepet.ToplamTutar = yeniToplam;

                kupon.KullanimSayisi = (kupon.KullanimSayisi ?? 0) + 1;
                kupon.Aktif = false;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Kupon başarıyla uygulandı! %{kupon.IndirimOrani} indirim kazandınız.",
                    indirimMiktari = indirimMiktari.ToString("C"),
                    yeniToplamTutar = yeniToplam.ToString("C")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> SepetDurumu()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (!kullaniciId.HasValue)
            {
                return Json(new { success = false, message = "Kullanıcı girişi yapılmamış." });
            }

            var sepet = await _context.Sepetler
                .Include(s => s.SepetUrunleri)
                .FirstOrDefaultAsync(s => s.KullaniciID == kullaniciId);

            if (sepet == null || !sepet.SepetUrunleri.Any())
            {
                return Json(new { success = true, isEmpty = true });
            }

            return Json(new
            {
                success = true,
                isEmpty = false,
                urunSayisi = sepet.SepetUrunleri.Sum(su => su.Miktar),
                toplamTutar = sepet.ToplamTutar
            });
        }
    }
}
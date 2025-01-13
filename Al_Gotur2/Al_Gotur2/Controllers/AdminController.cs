using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;

namespace Al_Gotur2.Controllers
{
    public class AdminController : Controller
    {
        private readonly Al_Gotur2Context _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminController(Al_Gotur2Context context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        private bool IsAdmin()
        {
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            return isAdmin == "True";
        }

        private IActionResult CheckAdminAccess()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("GirisYap", "Kullanici");
            }
            return null;
        }

        public IActionResult Index()
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            return View();
        }

        public async Task<IActionResult> UrunListesi()
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var urunler = await _context.Urunler
                .Include(u => u.Kategori)
                .OrderBy(u => u.UrunAdi)
                .ToListAsync();

            return View(urunler);
        }

        public IActionResult UrunEkle()
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            ViewBag.Kategoriler = _context.Kategoriler.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UrunEkle(
     [Bind("UrunAdi,KategoriID,Fiyat,StokMiktari,Aciklama,IsActive,IsDeleted")] Urun urun,
     IFormFile? ResimDosya)
        {
            try
            {
                ModelState.Remove("Kategori");
                ModelState.Remove("ResimUrl");

                if (ModelState.IsValid)
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            urun.IsActive = true;
                            urun.IsDeleted = false;

                            if (ResimDosya != null && ResimDosya.Length > 0)
                            {
                                var fileName = Path.GetFileNameWithoutExtension(ResimDosya.FileName);
                                var extension = Path.GetExtension(ResimDosya.FileName);
                                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;

                                var productsDir = Path.Combine(_hostEnvironment.WebRootPath, "products");
                                if (!Directory.Exists(productsDir))
                                {
                                    Directory.CreateDirectory(productsDir);
                                }

                                var path = Path.Combine(productsDir, fileName);

                                using (var stream = new FileStream(path, FileMode.Create))
                                {
                                    await ResimDosya.CopyToAsync(stream);
                                }

                                urun.ResimUrl = fileName;
                            }
                            else
                            {
                                urun.ResimUrl = "default.jpg";
                            }

                            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                            {
                                await connection.OpenAsync();
                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = @"
                                INSERT INTO Urunler (UrunAdi, KategoriID, Fiyat, StokMiktari, Aciklama, ResimUrl, IsActive, IsDeleted)
                                VALUES (@UrunAdi, @KategoriID, @Fiyat, @StokMiktari, @Aciklama, @ResimUrl, @IsActive, @IsDeleted)";

                                    command.Parameters.AddWithValue("@UrunAdi", urun.UrunAdi);
                                    command.Parameters.AddWithValue("@KategoriID", urun.KategoriID);
                                    command.Parameters.AddWithValue("@Fiyat", urun.Fiyat);
                                    command.Parameters.AddWithValue("@StokMiktari", urun.StokMiktari);
                                    command.Parameters.AddWithValue("@Aciklama", (object)urun.Aciklama ?? DBNull.Value);
                                    command.Parameters.AddWithValue("@ResimUrl", urun.ResimUrl);
                                    command.Parameters.AddWithValue("@IsActive", urun.IsActive);
                                    command.Parameters.AddWithValue("@IsDeleted", urun.IsDeleted);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            await transaction.CommitAsync();
                            TempData["Success"] = "Ürün başarıyla eklendi.";
                            return RedirectToAction(nameof(UrunListesi));
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ürün eklenirken bir hata oluştu: " + ex.Message);
            }

            ViewBag.Kategoriler = await _context.Kategoriler.ToListAsync();
            return View(urun);
        }

        public async Task<IActionResult> UrunDuzenle(int? id)
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            if (id == null)
            {
                return NotFound();
            }

            var urun = await _context.Urunler.FindAsync(id);
            if (urun == null)
            {
                return NotFound();
            }

            ViewBag.Kategoriler = _context.Kategoriler.ToList();
            return View(urun);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UrunDuzenle(int id,
    [Bind("UrunID,UrunAdi,KategoriID,Fiyat,StokMiktari,Aciklama,ResimUrl,IsActive,IsDeleted")] Urun urun,
    IFormFile? ResimDosya)
        {
            if (id != urun.UrunID)
            {
                return NotFound();
            }

            ModelState.Remove("Kategori");
            ModelState.Remove("ResimUrl");

            try
            {
                if (ModelState.IsValid)
                {
                    using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                        UPDATE Urunler 
                        SET UrunAdi = @UrunAdi,
                            KategoriID = @KategoriID,
                            Fiyat = @Fiyat,
                            StokMiktari = @StokMiktari,
                            Aciklama = @Aciklama,
                            ResimUrl = @ResimUrl,
                            IsActive = @IsActive,
                            IsDeleted = @IsDeleted
                        WHERE UrunID = @UrunID";

                            command.Parameters.AddWithValue("@UrunID", urun.UrunID);
                            command.Parameters.AddWithValue("@UrunAdi", urun.UrunAdi);
                            command.Parameters.AddWithValue("@KategoriID", urun.KategoriID);
                            command.Parameters.AddWithValue("@Fiyat", urun.Fiyat);
                            command.Parameters.AddWithValue("@StokMiktari", urun.StokMiktari);
                            command.Parameters.AddWithValue("@Aciklama", (object)urun.Aciklama ?? DBNull.Value);
                            command.Parameters.AddWithValue("@IsActive", urun.IsActive);
                            command.Parameters.AddWithValue("@IsDeleted", urun.IsDeleted);

                            if (ResimDosya != null && ResimDosya.Length > 0)
                            {
                                var fileName = Path.GetFileNameWithoutExtension(ResimDosya.FileName);
                                var extension = Path.GetExtension(ResimDosya.FileName);
                                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;

                                var path = Path.Combine(_hostEnvironment.WebRootPath, "products", fileName);

                                using (var stream = new FileStream(path, FileMode.Create))
                                {
                                    await ResimDosya.CopyToAsync(stream);
                                }

                                command.Parameters.AddWithValue("@ResimUrl", fileName);

                                var oldImage = await _context.Urunler
                                    .Where(u => u.UrunID == id)
                                    .Select(u => u.ResimUrl)
                                    .FirstOrDefaultAsync();

                                if (!string.IsNullOrEmpty(oldImage) && oldImage != "default.jpg")
                                {
                                    var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, "products", oldImage);
                                    if (System.IO.File.Exists(oldImagePath))
                                    {
                                        System.IO.File.Delete(oldImagePath);
                                    }
                                }
                            }
                            else
                            {
                                var existingImage = await _context.Urunler
                                    .Where(u => u.UrunID == id)
                                    .Select(u => u.ResimUrl)
                                    .FirstOrDefaultAsync();
                                command.Parameters.AddWithValue("@ResimUrl", existingImage);
                            }

                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    TempData["Success"] = "Ürün başarıyla güncellendi.";
                    return RedirectToAction(nameof(UrunListesi));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu: " + ex.Message);
            }

            ViewBag.Kategoriler = await _context.Kategoriler.ToListAsync();
            return View(urun);
        }
        public async Task<IActionResult> FiyatDegisiklikLoglari()
{
    var accessCheck = CheckAdminAccess();
    if (accessCheck != null) return accessCheck;

    var logs = await _context.UrunFiyatGuncellemeLog
        .OrderByDescending(l => l.changedata)
        .ToListAsync();

    return View(logs);
}
        public async Task<IActionResult> UrunSil(int? id)
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            if (id == null)
            {
                return NotFound();
            }

            var urun = await _context.Urunler
                .Include(u => u.Kategori)
                .FirstOrDefaultAsync(m => m.UrunID == id);

            if (urun == null)
            {
                return NotFound();
            }

            return View(urun);
        }

        [HttpPost, ActionName("UrunSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UrunSilOnay(int id)
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                        {
                            await connection.OpenAsync();
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = "DELETE FROM Urunler WHERE UrunID = @UrunID";
                                command.Parameters.AddWithValue("@UrunID", id);

                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        var urun = await _context.Urunler.FindAsync(id);
                        if (urun != null && !string.IsNullOrEmpty(urun.ResimUrl) && urun.ResimUrl != "default.jpg")
                        {
                            var imagePath = Path.Combine(_hostEnvironment.WebRootPath, "products", urun.ResimUrl);
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }

                        await transaction.CommitAsync();
                        TempData["Success"] = "Ürün başarıyla silindi.";
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ürün silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(UrunListesi));
        }
        public async Task<IActionResult> UrunDegisimLoglari()
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var logs = await _context.UrunDegisimLog
                .Include(l => l.Kategori)
                .OrderByDescending(l => l.changedata)
                .ToListAsync();

            return View(logs);
        }
        public async Task<IActionResult> ExcelIndir()
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var urunListesi = new List<dynamic>();

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("sp_UrunleriListele", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            urunListesi.Add(new
                            {
                                SiraNo = reader.GetInt64(0),
                                UrunAdi = reader.GetString(1),
                                Fiyat = reader.GetDecimal(2),
                                Stok = reader.GetInt32(3),
                                Kategori = reader.IsDBNull(4) ? "-" : reader.GetString(4)
                            });
                        }
                    }
                }
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Ürünler");

                // Başlıkları ekle
                worksheet.Cell(1, 1).Value = "Sıra No";
                worksheet.Cell(1, 2).Value = "Ürün Adı";
                worksheet.Cell(1, 3).Value = "Fiyat";
                worksheet.Cell(1, 4).Value = "Stok";
                worksheet.Cell(1, 5).Value = "Kategori";

                // Başlık stilini ayarla
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Verileri ekle
                for (int i = 0; i < urunListesi.Count; i++)
                {
                    var urun = urunListesi[i];
                    worksheet.Cell(i + 2, 1).Value = urun.SiraNo;
                    worksheet.Cell(i + 2, 2).Value = urun.UrunAdi;
                    worksheet.Cell(i + 2, 3).Value = urun.Fiyat.ToString("C2");
                    worksheet.Cell(i + 2, 4).Value = urun.Stok;
                    worksheet.Cell(i + 2, 5).Value = urun.Kategori;
                }

                // Sütun genişliklerini otomatik ayarla
                worksheet.Columns().AdjustToContents();

                // Tüm hücrelere border ekle
                var range = worksheet.Range(1, 1, urunListesi.Count + 1, 5);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Para birimi formatı
                var fiyatSutunu = worksheet.Column(3);
                fiyatSutunu.Style.NumberFormat.Format = "t#,##0.00";

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"UrunListesi_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        private bool UrunExists(int id)
        {
            return _context.Urunler.Any(e => e.UrunID == id);
        }
    }
}
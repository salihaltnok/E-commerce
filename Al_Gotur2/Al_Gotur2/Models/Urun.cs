// Models/Urun.cs
using System.ComponentModel.DataAnnotations;

namespace Al_Gotur2.Models
{
    public class Urun
    {
        public int UrunID { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [Display(Name = "Ürün Adı")]
        [StringLength(100)]
        public string UrunAdi { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        public int KategoriID { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur.")]
        [Range(0, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır.")]
        public decimal Fiyat { get; set; }

        [Required(ErrorMessage = "Stok miktarı zorunludur.")]
        [Display(Name = "Stok Miktarı")]
        public int StokMiktari { get; set; }

        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; } 

        [Display(Name = "Resim")]
        public string? ResimUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } 
        public int ToplamSatisMiktari { get; set; }
        public virtual Kategori Kategori { get; set; }
        public virtual ICollection<Yorum> Yorumlar { get; set; }
        public virtual ICollection<SiparisDetayi> SiparisDetaylari { get; set; }
        public virtual ICollection<Favoriler> Favoriler { get; set; }
        public virtual ICollection<Indirim> Indirimler { get; set; }

        public Urun()
        {
            Yorumlar = new HashSet<Yorum>();
            SiparisDetaylari = new HashSet<SiparisDetayi>();
            Favoriler = new HashSet<Favoriler>();
            Indirimler = new HashSet<Indirim>();
        }
    }
}
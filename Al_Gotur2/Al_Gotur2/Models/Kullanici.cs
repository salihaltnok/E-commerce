using System.ComponentModel.DataAnnotations;

namespace Al_Gotur2.Models
{
    public class Kullanici
    {
        public int KullaniciID { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "Kullanıcı Adı")]
        [StringLength(50)]
        public string KullaniciAdi { get; set; }

        [Required(ErrorMessage = "Email adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        [Display(Name = "Email")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [Display(Name = "Ad Soyad")]
        [StringLength(100)]
        public string AdSoyad { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        [Display(Name = "Admin Mi?")]
        public bool IsAdmin { get; set; } = false;

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;
        public virtual ICollection<Siparis> Siparisler { get; set; }
        public virtual ICollection<Adres> Adresler { get; set; }
        public virtual ICollection<Favoriler> Favoriler { get; set; }
        public virtual ICollection<Yorum> Yorumlar { get; set; }
        public Kullanici()
        {
            Siparisler = new HashSet<Siparis>();
            Adresler = new HashSet<Adres>();
            Favoriler = new HashSet<Favoriler>();
            Yorumlar = new HashSet<Yorum>();
        }
    }
}
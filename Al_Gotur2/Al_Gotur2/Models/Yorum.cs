using System.ComponentModel.DataAnnotations;

namespace Al_Gotur2.Models
{
    public class Yorum
    {
        public int YorumID { get; set; }

        public int UrunID { get; set; }
        public int KullaniciID { get; set; }

        [Required(ErrorMessage = "Yorum metni zorunludur.")]
        [StringLength(500)]
        public string YorumMetni { get; set; }

        [Range(1, 5)]
        public int Puan { get; set; }

        public DateTime YorumTarihi { get; set; } = DateTime.Now;
        public virtual Urun Urun { get; set; }
        public virtual Kullanici Kullanici { get; set; }
    }
}
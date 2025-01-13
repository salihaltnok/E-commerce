using System.ComponentModel.DataAnnotations;

namespace Al_Gotur2.Models
{
    public class Kategori
    {
        [Key]
        public int KategoriID { get; set; }

        [Required]
        [StringLength(50)]
        public string KategoriAdi { get; set; }

        public string Aciklama { get; set; }

        public virtual ICollection<Urun> Urunler { get; set; }
    }
}
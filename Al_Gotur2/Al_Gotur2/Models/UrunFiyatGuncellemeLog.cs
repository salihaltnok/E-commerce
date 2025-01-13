using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Al_Gotur2.Models
{
    public class UrunFiyatGuncellemeLog
    {
        [Key]
        public int ID { get; set; }

        public int UrunID { get; set; }

        public int KategoriID { get; set; }

        [StringLength(100)]
        public string UrunAdi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal eskiFiyat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal yeniFiyat { get; set; }

        public DateTime changedata { get; set; }

        [ForeignKey("KategoriID")]
        public virtual Kategori Kategori { get; set; }
    }
}
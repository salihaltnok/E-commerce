namespace Al_Gotur2.Models
{
    public class Indirim
    {
        public int IndirimID { get; set; }
        public int UrunID { get; set; }
        public decimal IndirimOrani { get; set; }
        public DateTime GecerlilikTarihi { get; set; }
        public bool Aktif { get; set; }
        public virtual Urun Urun { get; set; }
    }
}
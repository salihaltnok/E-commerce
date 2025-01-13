namespace Al_Gotur2.Models
{
    public class Kupon
    {
        public int KuponID { get; set; }
        public string Kod { get; set; }
        public decimal IndirimOrani { get; set; }
        public DateTime GecerlilikTarihi { get; set; }
        public bool Aktif { get; set; }
        public int? KullanimSayisi { get; set; }
    }
}
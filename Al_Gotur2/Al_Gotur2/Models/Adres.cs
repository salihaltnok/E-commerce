namespace Al_Gotur2.Models
{
    public class Adres
    {
        public int AdresID { get; set; }
        public int KullaniciID { get; set; }
        public string AdresBasligi { get; set; }
        public string AdresDetay { get; set; }
        public string Il { get; set; }
        public string Ilce { get; set; }
        public string PostaKodu { get; set; }

        public virtual Kullanici Kullanici { get; set; }
    }
}
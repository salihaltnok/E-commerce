namespace Al_Gotur2.Models
{
    public class Favoriler
    {
        public int FavoriID { get; set; }  
        public int KullaniciID { get; set; }
        public int UrunID { get; set; }
        public DateTime EklenmeTarihi { get; set; }

        public virtual Kullanici Kullanici { get; set; }
        public virtual Urun Urun { get; set; }
    }
}
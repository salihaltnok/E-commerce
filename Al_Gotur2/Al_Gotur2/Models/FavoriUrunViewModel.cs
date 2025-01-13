namespace Al_Gotur2.Models
{
    public class FavoriUrunViewModel
    {
        public int FavoriID { get; set; }
        public int UrunID { get; set; }
        public string UrunAdi { get; set; }
        public decimal Fiyat { get; set; }
        public string ResimUrl { get; set; }
        public string KategoriAdi { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public int StokMiktari { get; set; }
    }
}
namespace Al_Gotur2.Models
{
    public class EnCokSatanUrunViewModel
    {
        public int UrunID { get; set; }
        public string UrunAdi { get; set; }
        public decimal Fiyat { get; set; }
        public string ResimUrl { get; set; }
        public string Aciklama { get; set; }
        public string KategoriAdi { get; set; }
        public int ToplamSatisMiktari { get; set; }
        public int StokMiktari { get; set; }
    }
}
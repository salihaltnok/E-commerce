namespace Al_Gotur2.Models
{
    public class SiparisDetayi
    {
        public int SiparisDetayID { get; set; }
        public int SiparisID { get; set; }
        public int UrunID { get; set; }
        public int Miktar { get; set; }
        public decimal Fiyat { get; set; }
        public virtual Siparis Siparis { get; set; }
        public virtual Urun Urun { get; set; }
        public bool IsDeleted { get; internal set; }
    }
}
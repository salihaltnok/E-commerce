namespace Al_Gotur2.Models
{
    public class SepetUrunu
    {
        public int SepetUrunuID { get; set; }
        public int SepetID { get; set; }
        public int UrunID { get; set; }
        public int Miktar { get; set; }
        public decimal Fiyat { get; set; }
        public virtual Sepet Sepet { get; set; }
        public virtual Urun Urun { get; set; }
    }
}

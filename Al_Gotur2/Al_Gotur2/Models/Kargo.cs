namespace Al_Gotur2.Models
{
    public class Kargo
    {
        public int KargoID { get; set; }
        public int SiparisID { get; set; }
        public DateTime KargoTarihi { get; set; }
        public DateTime TahminiTeslimTarihi { get; set; }
        public string KargoDurumu { get; set; }
        public string? TakipNo { get; set; }
        public string? KargoFirmasi { get; set; }
        public virtual Siparis Siparis { get; set; }
    }
}
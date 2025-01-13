using System.ComponentModel.DataAnnotations;

namespace Al_Gotur2.Models
{
    public class Sepet
    {
        public Sepet()
        {
            SepetUrunleri = new HashSet<SepetUrunu>();
        }
        [Key] // using System.ComponentModel.DataAnnotations;
        public int SepetID { get; set; }
        public int KullaniciID { get; set; }
        public decimal ToplamTutar { get; set; }
        public virtual Kullanici Kullanici { get; set; }
        public decimal OrijinalTutar { get; set; }
        public decimal UygulananIndirimOrani { get; set; }
        public virtual ICollection<SepetUrunu> SepetUrunleri { get; set; }
        public decimal IndirimTutari => OrijinalTutar - ToplamTutar;
    }
}
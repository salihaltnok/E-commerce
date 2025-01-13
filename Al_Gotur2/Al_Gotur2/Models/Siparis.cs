using Al_Gotur2.Models;
using System.ComponentModel.DataAnnotations;

public class Siparis
{
    public Siparis()
    {
        SiparisDetaylari = new HashSet<SiparisDetayi>();
    }

    public int SiparisID { get; set; }

    public int KullaniciID { get; set; }

    [Display(Name = "Sipariş Tarihi")]
    public DateTime SiparisTarihi { get; set; }

    [Display(Name = "Toplam Tutar")]
    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal ToplamTutar { get; set; }

    [Display(Name = "Orijinal Tutar")]
    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal OrijinalTutar { get; set; }

    [Display(Name = "Uygulanan İndirim")]
    [DisplayFormat(DataFormatString = "{0:P2}")]
    public decimal UygulananIndirim { get; set; }

    [Display(Name = "Durum")]
    [Required(ErrorMessage = "Sipariş durumu zorunludur.")]
    public string Durum { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Kullanici Kullanici { get; set; }
    public virtual ICollection<SiparisDetayi> SiparisDetaylari { get; set; }
    public virtual Kargo Kargo { get; set; }

    [Display(Name = "İndirim Tutarı")]
    public decimal IndirimTutari => OrijinalTutar - ToplamTutar;

    [Display(Name = "İndirim Oranı")]
    public string IndirimOrani => $"%{UygulananIndirim:N0}";
}
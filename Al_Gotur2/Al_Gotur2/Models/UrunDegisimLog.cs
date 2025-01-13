using Al_Gotur2.Models;

public class UrunDegisimLog
{
    public int ID { get; set; }
    public int UrunID { get; set; }
    public int KategoriID { get; set; }
    public string UrunAdi { get; set; }
    public decimal Fiyat { get; set; }
    public string ChangeType { get; set; }
    public DateTime changedata { get; set; }
    public virtual Kategori Kategori { get; set; }
}
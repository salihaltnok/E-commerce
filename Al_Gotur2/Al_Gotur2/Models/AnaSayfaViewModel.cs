// Models/ViewModels/AnaSayfaViewModel.cs
namespace Al_Gotur2.Models
{
    public class AnaSayfaViewModel
    {
        public List<EnCokSatanUrunViewModel> OneCikanUrunler { get; set; }
        public List<EnCokSatanUrunViewModel> EnCokSatanUrunler { get; set; }

        public AnaSayfaViewModel()
        {
            OneCikanUrunler = new List<EnCokSatanUrunViewModel>();
            EnCokSatanUrunler = new List<EnCokSatanUrunViewModel>();
        }
    }
}
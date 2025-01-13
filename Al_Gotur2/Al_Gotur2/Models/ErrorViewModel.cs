// Models/ErrorViewModel.cs
namespace Al_Gotur2.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string Message { get; set; }  // Bu satırı ekleyin
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
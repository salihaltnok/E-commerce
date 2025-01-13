// Sepet işlemleri için fonksiyonlar
function sepeteEkle(urunId, miktar) {
    $.post('/Sepet/Ekle', { urunId: urunId, miktar: miktar }, function (response) {
        if (response.success) {
            $('#sepetUrunSayisi').text(response.sepetUrunSayisi);
            alert('Ürün sepete eklendi!');
        } else {
            alert('Hata: ' + response.message);
        }
    });
}

// Sayfa yüklendiğinde sepet sayısını güncelle
$(document).ready(function () {
    $.get('/Sepet/UrunSayisi', function (response) {
        $('#sepetUrunSayisi').text(response);
    });
});
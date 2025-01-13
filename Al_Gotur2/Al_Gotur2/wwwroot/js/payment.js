$(document).ready(function () {
    // Kredi kartı input maskeleri
    $('#cardNumber').mask('0000 0000 0000 0000');
    $('#cardExpiry').mask('00/00');
    $('#cardCvv').mask('000');

    // Kredi kartı numarası değiştiğinde kart tipini tespit et
    $('#cardNumber').on('keyup', function () {
        detectCardType($(this).val());
    });

    // Form submit
    $('#paymentForm').submit(function (e) {
        e.preventDefault();

        if (validateForm()) {
            showLoading();
            // Form verilerini hazırla
            const formData = {
                cardNumber: $('#cardNumber').val().replace(/\s/g, ''),
                cardExpiry: $('#cardExpiry').val(),
                cardCvv: $('#cardCvv').val(),
                cardHolderName: $('#cardHolderName').val(),
                amount: $('#amount').val()
            };

            // AJAX ile ödeme işlemini başlat
            $.ajax({
                url: $(this).attr('action'),
                type: 'POST',
                data: formData,
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showSuccess('Ödeme başarıyla tamamlandı', response.redirectUrl);
                    } else {
                        showError(response.message || 'Ödeme işlemi başarısız oldu.');
                    }
                },
                error: function () {
                    hideLoading();
                    showError('Bir hata oluştu. Lütfen daha sonra tekrar deneyin.');
                }
            });
        }
    });
});

// Kart tipini tespit et
function detectCardType(number) {
    const cardTypes = {
        visa: /^4/,
        mastercard: /^5[1-5]/,
        amex: /^3[47]/,
        troy: /^9792/
    };

    number = number.replace(/\s/g, '');
    let cardType = 'unknown';

    for (let type in cardTypes) {
        if (cardTypes[type].test(number)) {
            cardType = type;
            break;
        }
    }

    // Kart ikonunu güncelle
    updateCardIcon(cardType);
}

// Kart ikonunu güncelle
function updateCardIcon(cardType) {
    const iconElement = $('#cardTypeIcon');
    iconElement.removeClass().addClass('card-brand-icon');

    switch (cardType) {
        case 'visa':
            iconElement.addClass('fa fa-cc-visa');
            break;
        case 'mastercard':
            iconElement.addClass('fa fa-cc-mastercard');
            break;
        case 'amex':
            iconElement.addClass('fa fa-cc-amex');
            break;
        default:
            iconElement.addClass('fa fa-credit-card');
    }
}

// Form validasyonu
function validateForm() {
    let isValid = true;
    const cardNumber = $('#cardNumber').val().replace(/\s/g, '');
    const cardExpiry = $('#cardExpiry').val();
    const cardCvv = $('#cardCvv').val();
    const cardHolderName = $('#cardHolderName').val();

    // Kart numarası kontrolü
    if (!luhnCheck(cardNumber)) {
        showFieldError('cardNumber', 'Geçersiz kart numarası');
        isValid = false;
    } else {
        removeFieldError('cardNumber');
    }

    // Son kullanma tarihi kontrolü
    if (!validateExpiry(cardExpiry)) {
        showFieldError('cardExpiry', 'Geçersiz son kullanma tarihi');
        isValid = false;
    } else {
        removeFieldError('cardExpiry');
    }

    // CVV kontrolü
    if (cardCvv.length !== 3) {
        showFieldError('cardCvv', 'Geçersiz CVV');
        isValid = false;
    } else {
        removeFieldError('cardCvv');
    }

    // Kart sahibi adı kontrolü
    if (cardHolderName.trim().length < 5) {
        showFieldError('cardHolderName', 'Kart sahibinin adını giriniz');
        isValid = false;
    } else {
        removeFieldError('cardHolderName');
    }

    return isValid;
}

// Luhn algoritması ile kart numarası kontrolü
function luhnCheck(cardNumber) {
    if (!cardNumber) return false;

    let sum = 0;
    let isEven = false;

    for (let i = cardNumber.length - 1; i >= 0; i--) {
        let digit = parseInt(cardNumber.charAt(i));

        if (isEven) {
            digit *= 2;
            if (digit > 9) {
                digit -= 9;
            }
        }

        sum += digit;
        isEven = !isEven;
    }

    return (sum % 10) === 0;
}

// Son kullanma tarihi kontrolü
function validateExpiry(expiry) {
    if (!expiry) return false;

    const [month, year] = expiry.split('/');
    const expDate = new Date(20 + year, month - 1);
    const today = new Date();

    return expDate > today;
}

// Hata gösterme fonksiyonları
function showFieldError(fieldId, message) {
    $(`#${fieldId}`).addClass('is-invalid');
    $(`#${fieldId}Error`).text(message).show();
}

function removeFieldError(fieldId) {
    $(`#${fieldId}`).removeClass('is-invalid');
    $(`#${fieldId}Error`).hide();
}

// Loading gösterge fonksiyonları
function showLoading() {
    $('.loading-overlay').fadeIn(200);
}

function hideLoading() {
    $('.loading-overlay').fadeOut(200);
}

// Başarı mesajı
function showSuccess(message, redirectUrl) {
    Swal.fire({
        icon: 'success',
        title: 'Başarılı!',
        text: message,
        confirmButtonText: 'Tamam'
    }).then((result) => {
        if (redirectUrl) {
            window.location.href = redirectUrl;
        }
    });
}

// Hata mesajı
function showError(message) {
    Swal.fire({
        icon: 'error',
        title: 'Hata!',
        text: message,
        confirmButtonText: 'Tamam'
    });
}

// Para formatı
function formatMoney(amount) {
    return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
    }).format(amount);
}

// Sayfa yüklendiğinde loading'i gizle
$(window).on('load', function () {
    hideLoading();
});
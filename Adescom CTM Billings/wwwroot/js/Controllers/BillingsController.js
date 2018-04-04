var BillingsController = function () {
    "use strict";

    var startDateInput = $("#start-date-input");
    var endDateInput = $("#end-date-input");
    var requestVerificationToken = $("#request-verification-token");
    var getBillingsStatus = $("#get-billings-status");
    var getBillingsButtonIcon = $("#get-billings-button-icon");
    var getBillingsButton = $("#get-billings-button");

    var init = function (container) {
        getBillingsButton.click(getBillings);
    };

    var getBillings = function (e) {
        getBillingsButton.attr('disabled', true);
        getBillingsButtonIcon.removeClass('glyphicon-download-alt');
        getBillingsButtonIcon.addClass('glyphicon-cog gly-spin');
        getBillingsStatus.text('pobieranie billingów');

        BillingsService.getBillings(startDateInput.val(), endDateInput.val(), requestVerificationToken.val(), done, fail);
    };

    var done = function (xhr) {
        var contentDispositionHeader = xhr.getResponseHeader('Content-Disposition');
        var parts = contentDispositionHeader.split(';');
        var filenamePart = parts[1].split('=')[1];
        var filename = filenamePart.substring(1, filenamePart.length - 1);
        var blob = new Blob([xhr.response], { type: "application/zip" });
        saveAs(blob, filename);

        getBillingsButton.attr('disabled', false);
        getBillingsButtonIcon.removeClass('glyphicon-cog gly-spin');
        getBillingsButtonIcon.addClass('glyphicon-download-alt');
        getBillingsStatus.text('zakończono pobieranie');
    };

    var fail = function (xhr) {
        getBillingsButton.attr('disabled', false);
        getBillingsButtonIcon.removeClass('glyphicon-cog gly-spin');
        getBillingsButtonIcon.addClass('glyphicon-download-alt');
        getBillingsStatus.text('wystąpił błąd');
    };

    return {
        init: init,
    };
}(BillingsService);
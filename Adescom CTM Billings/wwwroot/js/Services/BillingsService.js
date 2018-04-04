var BillingsService = function () {
    "use strict";

    // POST: index
    var getBillings = function (startDate, endDate, requestVerificationToken, done, fail) {
        var xhr = new XMLHttpRequest();
        xhr.open('POST', '/?handler=export', true);
        xhr.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');
        xhr.responseType = 'blob';
        xhr.onload = function () {
            if (this.status == 200) {
                done(xhr);
            }
            else {
                fail(xhr);
            }
        };
        xhr.send('StartDate=' + startDate + '&' + 'EndDate=' + endDate + '&' + '__RequestVerificationToken=' + requestVerificationToken); 
    };

    return {
        getBillings: getBillings
    };
}();
$(document).ready(function () {
    $(document).ajaxStart(function () {
        $('#divProcessando').show();
    });

    $(document).ajaxStop(function () {
        $('#divProcessando').hide();
    });
});

function doIncrementProgressBar(increment) {
    w = parseInt(document.getElementById('progressBar').style.width);
    document.getElementById('progressBar').style.width = (w + increment) + '%';
    document.getElementById('progressPercent').textContent = increment + "%";
}

function baseUrl() {
    var href = window.location.href.split('/');
    return href[0] + '//' + href[2] + '/';
}
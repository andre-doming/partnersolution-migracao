function ReadFile(file) {
    var data;
    completeFile = "upload\\" + file;
    alert(completeFile);
    $.ajax({
        type: "GET",
        url: "js-tutorials.com_sample_file.csv",
        dataType: "text",
        success: function (response) {
            data = $.csv.toArrays(response);
            generateHtmlTable(data);
        }
    })
};
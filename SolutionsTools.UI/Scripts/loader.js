function avisoAguarde() {
    if (document.getElementById('divProcessando')) {
        document.getElementById('divProcessando').style.display = '';
        return;
    }
    oDiv = document.createElement("div");
    with (oDiv) {
        id = "divProcessando";
    }
    document.body.appendChild(oDiv);
}
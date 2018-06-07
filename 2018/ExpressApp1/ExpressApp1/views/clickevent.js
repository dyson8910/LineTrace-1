function entryChange() {
    radio = document.getElementsByName('mode')
    if (radio[1].checked) {
        //フォーム
        document.getElementById('PIDSPEED').style.display = "";
        document.getElementById('PID1').style.display = "";
        document.getElementById('PID2').style.display = "";
        document.getElementById('PID3').style.display = "";
        document.getElementById('ONOFFSPEED').style.display = "none";
        document.getElementById('ONOFF1').style.display = "none";
        document.getElementById('ONOFF2').style.display = "none";
        document.getElementById('ONOFF3').style.display = "none";
        document.getElementById('ONOFF4').style.display = "none";
        document.getElementById('ONOFF5').style.display = "none";
        document.getElementById('ONOFF6').style.display = "none";
        document.getElementById('ONOFF7').style.display = "none";
    } else if (radio[0].checked) {
        //フォーム
        document.getElementById('PIDSPEED').style.display = "none";
        document.getElementById('PID1').style.display = "none";
        document.getElementById('PID2').style.display = "none";
        document.getElementById('PID3').style.display = "none";
        document.getElementById('ONOFFSPEED').style.display = "";
        document.getElementById('ONOFF1').style.display = "";
        document.getElementById('ONOFF2').style.display = "";
        document.getElementById('ONOFF3').style.display = "";
        document.getElementById('ONOFF4').style.display = "";
        document.getElementById('ONOFF5').style.display = "";
        document.getElementById('ONOFF6').style.display = "";
        document.getElementById('ONOFF7').style.display = "";
    }
}

//オンロードさせ、リロード時に選択を保持
window.onload = entryChange;

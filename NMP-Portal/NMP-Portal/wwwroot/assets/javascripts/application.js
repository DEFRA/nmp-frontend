// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    const printButton = document.getElementById("cropReportPrintButton");
    if (printButton) {
        printButton.addEventListener("click", function () {
            window.print();
        });
    }

    const backButton = document.getElementById("BackToPreviousPage");
    if (backButton) {
        backButton.addEventListener("click", function () {
            window.history.back();
        });
    }
});

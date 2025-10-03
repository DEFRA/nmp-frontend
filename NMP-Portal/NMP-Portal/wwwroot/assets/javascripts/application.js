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




    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function () {
            const submitButton = form.querySelector('button[type="submit"]');
            const overlay = document.getElementById('loading-overlay');

            if (submitButton) {
                submitButton.disabled = true;
            }

            if (form.classList.contains('form-with-overlay') && overlay) {
                overlay.classList.add('show');
            }
        });

    }

    //const selectAllCheckbox = document.getElementById("select-all");

    //if (selectAllCheckbox) {
    //    selectAllCheckbox.addEventListener("change", function () {
    //        var checkboxes = document.querySelectorAll('input[name="FieldList"]');

    //        checkboxes.forEach(function (checkbox) {
    //            checkbox.checked = document.getElementById("select-all").checked;
    //        });
    //    });
    //}

    const selectAllCheckbox = document.getElementById("select-all");

    if (selectAllCheckbox) {
        
        const checkboxes = document.querySelectorAll('input[type="checkbox"]:not([name="select-all"])');

        // Update "Select All" checkbox when page loads or when returning
        selectAllCheckbox.checked = Array.from(checkboxes).every(checkbox => checkbox.checked);

        checkboxes.forEach(function (checkbox) {
            checkbox.addEventListener("change", function () {
                // Update "Select All" checkbox based on individual checkboxes' state
                selectAllCheckbox.checked = Array.from(checkboxes).every(checkbox => checkbox.checked);
            });
        });
        // Select or deselect all checkboxes when "Select All" changes
        selectAllCheckbox.addEventListener("change", function () {
            checkboxes.forEach(checkbox => checkbox.checked = selectAllCheckbox.checked);
        });
    }



});

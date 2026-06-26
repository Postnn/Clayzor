// ── Kesco Grid — Print ───────────────────────────────────────────────────
//
// printCurrentPage:  opens the browser print dialog for the current page data.
// printHtml:         renders a server-generated HTML string in a hidden iframe,
//                    prints it, and cleans up after the dialog closes.
// showSpinner / hideSpinner:  toggle a CSS spinner next to the grid title
//                             without triggering Blazor re-renders.
//
// Follows the same IIFE pattern as kescoGridColumnDrag.js.

window.kescoGridPrint = (function () {
    var PRINTING_CLASS = 'kesco-grid-printing';

    /**
     * Shows a CSS spinner by its DOM id (display:inline-block).
     * @param {string} spinnerId — id of the spinner <span> element
     */
    function showSpinner(spinnerId) {
        var el = document.getElementById(spinnerId);
        if (el) el.style.display = 'inline-block';
    }

    /**
     * Hides a CSS spinner by its DOM id (display:none).
     * @param {string} spinnerId — id of the spinner <span> element
     */
    function hideSpinner(spinnerId) {
        var el = document.getElementById(spinnerId);
        if (el) el.style.display = 'none';
    }

    /**
     * Opens the browser print dialog for the current page of the specified grid.
     * Only the target grid will be printed — all other page content is hidden
     * via @media print CSS that reacts to the temporary .kesco-grid-printing class.
     *
     * @param {string} gridId — DOM id of the grid's root MudPaper element
     */
    function printCurrentPage(gridId) {
        var grid = document.getElementById(gridId);
        if (!grid) {
            console.warn('[kescoGridPrint] Grid not found: ' + gridId);
            return;
        }

        grid.classList.add(PRINTING_CLASS);

        function cleanup() {
            grid.classList.remove(PRINTING_CLASS);
            window.removeEventListener('afterprint', cleanup);
        }

        window.addEventListener('afterprint', cleanup);
        window.print();
    }

    /**
     * Prints an HTML string in a hidden iframe. The iframe is created,
     * populated with the HTML, printed, and removed after the print dialog
     * closes. Returns a Promise so Blazor can await completion.
     *
     * @param {string} html — complete HTML document string
     * @returns {Promise<void>}
     */
    function printHtml(html) {
        return new Promise(function (resolve) {
            var iframe = document.createElement('iframe');
            // Off‑screen — never covers the visible grid, but still renders for print
            iframe.style.cssText =
                'position:absolute;left:-9999px;top:0;width:800px;height:600px;';

            document.body.appendChild(iframe);

            var doc = iframe.contentWindow.document;
            doc.open();
            doc.write(html);
            doc.close();

            iframe.contentWindow.addEventListener('afterprint', function () {
                document.body.removeChild(iframe);
                resolve();
            });

            iframe.contentWindow.print();
        });
    }

    return {
        showSpinner: showSpinner,
        hideSpinner: hideSpinner,
        printCurrentPage: printCurrentPage,
        printHtml: printHtml
    };

})();

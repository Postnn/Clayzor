// ── Kesco Grid — Print current page ─────────────────────────────────────
//
// Triggers the browser's native print dialog for the current page data.
// The grid to print is identified by its DOM id (the MudPaper root element).
//
// Before printing, a .kesco-grid-printing class is added to the target grid.
// All print-time styling is handled by @media print rules in app.css —
// they use .kesco-grid-printing to show only the target grid and hide
// everything else (toolbar, paginator, other grids, layout chrome, etc.).
// The class is removed after the print dialog closes.
//
// Follows the same IIFE pattern as kescoGridColumnDrag.js.

window.kescoGridPrint = (function () {
    var PRINTING_CLASS = 'kesco-grid-printing';

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

    return {
        printCurrentPage: printCurrentPage
    };

})();

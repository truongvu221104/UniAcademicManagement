// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("submit", function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    var method = (form.getAttribute("method") || "get").toLowerCase();
    if (method !== "get") {
        return;
    }

    var currentUrl = new URL(window.location.href);
    var currentPageSize = currentUrl.searchParams.get("pageSize");

    if (currentPageSize && !form.querySelector("[name='pageSize']")) {
        var pageSizeInput = document.createElement("input");
        pageSizeInput.type = "hidden";
        pageSizeInput.name = "pageSize";
        pageSizeInput.value = currentPageSize;
        form.appendChild(pageSizeInput);
    }

    var pageInput = form.querySelector("[name='page']");
    if (!pageInput) {
        pageInput = document.createElement("input");
        pageInput.setAttribute("type", "hidden");
        pageInput.setAttribute("name", "page");
        form.appendChild(pageInput);
    }

    pageInput.value = "1";
});

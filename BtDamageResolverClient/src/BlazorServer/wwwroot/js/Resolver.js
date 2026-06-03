// Delegated tooltip handling for elements carrying data-tooltip-id (+ data-tooltip-content).
// A single set of document-level listeners replaces per-element inline onmousemove/onmouseout
// handlers (e.g. the paper doll SVG regions), so the browser no longer parses a handler string
// for every shape.
(function () {
    var activeTooltip = null;
    var activeTarget = null;
    var activeContent = null;

    function findTarget(evt) {
        return evt.target instanceof Element ? evt.target.closest("[data-tooltip-id]") : null;
    }

    function position(tooltip, evt) {
        tooltip.style.left = evt.pageX + 10 + "px";
        tooltip.style.top = evt.pageY + 10 + "px";
    }

    document.addEventListener("mouseover", function (evt) {
        var target = findTarget(evt);
        if (target === null) {
            return;
        }

        var tooltip = document.getElementById(target.getAttribute("data-tooltip-id"));
        if (tooltip === null) {
            return;
        }

        activeTarget = target;
        activeTooltip = tooltip;
        activeContent = target.getAttribute("data-tooltip-content");
        tooltip.innerHTML = activeContent;
        tooltip.style.display = "block";
        position(tooltip, evt);
    });

    document.addEventListener("mousemove", function (evt) {
        if (activeTooltip === null) {
            return;
        }

        var content = activeTarget.getAttribute("data-tooltip-content");
        if (content !== activeContent) {
            activeContent = content;
            activeTooltip.innerHTML = content;
        }

        position(activeTooltip, evt);
    });

    document.addEventListener("mouseout", function (evt) {
        var target = findTarget(evt);
        if (target === null) {
            return;
        }

        if (evt.relatedTarget instanceof Node && target.contains(evt.relatedTarget)) {
            return;
        }

        var tooltip = document.getElementById(target.getAttribute("data-tooltip-id"));
        if (tooltip !== null) {
            tooltip.style.display = "none";
        }

        if (target === activeTarget) {
            activeTooltip = null;
            activeTarget = null;
            activeContent = null;
        }
    });
})();
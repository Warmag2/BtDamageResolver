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

// Delegated drag-and-drop visual feedback for ContainerReorderableList. The dragging/drop-over
// highlight classes used to be driven by server state, so every dragenter was a SignalR roundtrip
// (and dragenter bubbles from child elements, multiplying them). Here the visuals are handled
// purely client-side; only dragstart/drop/dragend still roundtrip to perform the actual reorder.
// A single active container is tracked so nested reorderable lists do not clobber each other.
(function () {
    var containerSelector = ".resolver_div_reorderablelistcontainer";
    var targetSelector = ".resolver_div_reorderableitem, .resolver_div_reorderablesentinel";
    var sourceSelector = ".resolver_div_reorderableitem, .resolver_div_draghandle";
    var draggingClass = "resolver_div_reorderablelistcontainer--dragging";
    var itemOverClass = "resolver_div_reorderableitem--dropover";
    var sentinelOverClass = "resolver_div_reorderablesentinel--dropover";

    var activeContainer = null;

    function clearDropOver(container) {
        if (container === null) {
            return;
        }

        var marked = container.querySelectorAll("." + itemOverClass + ", ." + sentinelOverClass);
        for (var i = 0; i < marked.length; i++) {
            // Skip elements that belong to a nested reorderable list.
            if (marked[i].closest(containerSelector) === container) {
                marked[i].classList.remove(itemOverClass);
                marked[i].classList.remove(sentinelOverClass);
            }
        }
    }

    function endDrag() {
        if (activeContainer === null) {
            return;
        }

        activeContainer.classList.remove(draggingClass);
        clearDropOver(activeContainer);
        activeContainer = null;
    }

    document.addEventListener("dragstart", function (evt) {
        if (!(evt.target instanceof Element)) {
            return;
        }

        var source = evt.target.closest(sourceSelector);
        if (source === null) {
            return;
        }

        var container = source.closest(containerSelector);
        if (container === null) {
            return;
        }

        endDrag();
        activeContainer = container;
        container.classList.add(draggingClass);
    }, true);

    document.addEventListener("dragenter", function (evt) {
        if (activeContainer === null || !(evt.target instanceof Element)) {
            return;
        }

        var over = evt.target.closest(targetSelector);
        if (over === null || over.closest(containerSelector) !== activeContainer) {
            clearDropOver(activeContainer);
            return;
        }

        clearDropOver(activeContainer);
        if (over.classList.contains("resolver_div_reorderableitem")) {
            over.classList.add(itemOverClass);
        } else {
            over.classList.add(sentinelOverClass);
        }
    }, true);

    document.addEventListener("drop", endDrag, true);
    document.addEventListener("dragend", endDrag, true);
})();
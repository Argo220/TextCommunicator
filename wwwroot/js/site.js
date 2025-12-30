(function () {
  function wireSearch(inputId, listSelector, itemSelector, textSelector) {
    const input = document.getElementById(inputId);
    if (!input) return;

    const list = document.querySelector(listSelector);
    if (!list) return;

    const items = Array.from(list.querySelectorAll(itemSelector));
    const getText = (el) => {
      if (!textSelector) return (el.textContent || "").toLowerCase();
      const t = el.querySelector(textSelector);
      return ((t ? t.textContent : el.textContent) || "").toLowerCase();
    };

    input.addEventListener("input", () => {
      const q = (input.value || "").trim().toLowerCase();
      items.forEach((it) => {
        const hay = getText(it);
        it.style.display = hay.includes(q) ? "" : "none";
      });
    });
  }

  // expose
  window.tcWireSearch = wireSearch;
})();

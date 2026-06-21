/**
 * SecureVault Content Script
 * Runs on every page. Detects login forms and injects the auto-fill button.
 */

(function () {
  "use strict";

  let injected = false;

  function findLoginFields() {
    const inputs = Array.from(document.querySelectorAll("input"));
    const passwordFields = inputs.filter(
      (el) =>
        el.type === "password" ||
        (el.getAttribute("autocomplete") || "").includes("current-password") ||
        (el.getAttribute("autocomplete") || "").includes("new-password")
    );
    const usernameFields = inputs.filter(
      (el) =>
        el.type === "email" ||
        el.type === "text" && (
          (el.getAttribute("autocomplete") || "").match(/username|email|user/i) ||
          (el.name || "").match(/username|email|user|login/i) ||
          (el.placeholder || "").match(/username|email|user/i)
        )
    );
    return { passwordFields, usernameFields };
  }

  function injectAutofillButton(passwordField) {
    if (document.getElementById("sv-autofill-btn")) return;

    const btn = document.createElement("button");
    btn.id = "sv-autofill-btn";
    btn.type = "button";
    btn.title = "Fill with SecureVault";
    btn.innerHTML = "🔐";
    btn.style.cssText = `
      position: absolute;
      width: 28px; height: 28px;
      background: #89B4FA;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      z-index: 2147483647;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0;
      box-shadow: 0 2px 8px rgba(0,0,0,.3);
    `;

    const rect = passwordField.getBoundingClientRect();
    const scrollX = window.scrollX, scrollY = window.scrollY;
    btn.style.left = `${rect.right + scrollX - 32}px`;
    btn.style.top = `${rect.top + scrollY + (rect.height - 28) / 2}px`;

    btn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      chrome.runtime.sendMessage({ action: "open_autofill_popup", url: window.location.href });
    });

    document.body.appendChild(btn);
  }

  function tryInject() {
    if (injected) return;
    const { passwordFields, usernameFields } = findLoginFields();
    if (passwordFields.length > 0) {
      injected = true;
      injectAutofillButton(passwordFields[0]);
    }
  }

  // Listen for fill command from popup
  chrome.runtime.onMessage.addListener((msg) => {
    if (msg.action === "fill_credentials") {
      const { username, password } = msg;
      const { passwordFields, usernameFields } = findLoginFields();

      const fillField = (field, value) => {
        if (!field) return;
        const nativeInputValueSetter = Object.getOwnPropertyDescriptor(
          window.HTMLInputElement.prototype, "value"
        )?.set;
        nativeInputValueSetter?.call(field, value);
        field.dispatchEvent(new Event("input", { bubbles: true }));
        field.dispatchEvent(new Event("change", { bubbles: true }));
      };

      if (usernameFields[0]) fillField(usernameFields[0], username);
      if (passwordFields[0]) fillField(passwordFields[0], password);

      // Remove the button after filling
      document.getElementById("sv-autofill-btn")?.remove();
    }
  });

  // Observer for SPA sites that render forms late
  const observer = new MutationObserver(() => tryInject());
  observer.observe(document.body, { childList: true, subtree: true });
  tryInject();
})();

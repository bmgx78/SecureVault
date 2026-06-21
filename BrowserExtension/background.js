/**
 * SecureVault Background Service Worker
 * Handles communication between popup, content scripts, and (optionally) native messaging host.
 */

"use strict";

// In-memory unlock state (cleared when service worker restarts)
let unlockState = { unlocked: false, masterKey: null };

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  switch (msg.action) {
    case "open_autofill_popup":
      // Open popup with URL context
      chrome.storage.session.set({ autofillUrl: msg.url });
      chrome.action.openPopup?.();
      break;

    case "get_credentials_for_url":
      handleGetCredentials(msg.url).then(sendResponse);
      return true; // async

    case "fill_credentials":
      // Relay fill command to the active tab's content script
      chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
        if (tabs[0]?.id) {
          chrome.tabs.sendMessage(tabs[0].id, {
            action: "fill_credentials",
            username: msg.username,
            password: msg.password,
          });
        }
      });
      sendResponse({ ok: true });
      break;

    case "generate_password":
      sendResponse({ password: generatePassword(msg.options || {}) });
      break;

    case "copy_to_clipboard":
      handleCopy(msg.text, msg.clearAfter ?? 30);
      sendResponse({ ok: true });
      break;
  }
});

async function handleGetCredentials(url) {
  try {
    const domain = new URL(url).hostname.replace(/^www\./, "");
    const stored = await chrome.storage.local.get("vault_encrypted");
    if (!stored.vault_encrypted) return { credentials: [] };

    // If native messaging host is available, delegate decryption
    // Otherwise return empty (user must open app)
    return { credentials: [], domain, needsUnlock: true };
  } catch {
    return { credentials: [], error: "Invalid URL" };
  }
}

function generatePassword({ length = 20, upper = true, digits = true, symbols = true } = {}) {
  const lower = "abcdefghijklmnopqrstuvwxyz";
  const upperStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  const digitStr = "0123456789";
  const symbolStr = "!@#$%^&*()-_=+[]{}|;:,.<>?";
  let charset = lower;
  if (upper) charset += upperStr;
  if (digits) charset += digitStr;
  if (symbols) charset += symbolStr;
  const array = new Uint32Array(length);
  crypto.getRandomValues(array);
  return Array.from(array).map((n) => charset[n % charset.length]).join("");
}

function handleCopy(text, clearAfterSeconds) {
  // Clipboard API not available in SW — relay to active tab
  chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
    if (tabs[0]?.id) {
      chrome.scripting.executeScript({
        target: { tabId: tabs[0].id },
        func: (t, delay) => {
          navigator.clipboard.writeText(t);
          setTimeout(() => {
            navigator.clipboard.readText().then((c) => {
              if (c === t) navigator.clipboard.writeText("");
            });
          }, delay * 1000);
        },
        args: [text, clearAfterSeconds],
      });
    }
  });
}

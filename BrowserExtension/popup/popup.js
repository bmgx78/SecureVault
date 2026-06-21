/**
 * SecureVault Popup Script
 * Handles the extension popup UI: unlock, search, fill, and generator.
 */

"use strict";

// --- State ---
let vault = []; // decrypted entries (kept in memory only while popup is open)
let masterKey = null;
let currentUrl = "";

// --- DOM refs ---
const lockedView   = document.getElementById("locked-view");
const unlockedView = document.getElementById("unlocked-view");
const genView      = document.getElementById("gen-view");
const masterPwInput = document.getElementById("master-pw");
const unlockBtn    = document.getElementById("unlock-btn");
const unlockError  = document.getElementById("unlock-error");
const lockBtn      = document.getElementById("lock-btn");
const searchInput  = document.getElementById("search");
const credList     = document.getElementById("cred-list");
const domainLabel  = document.getElementById("domain-label");
const genBtn       = document.getElementById("gen-btn");
const genBack      = document.getElementById("gen-back");
const openAppBtn   = document.getElementById("open-app-btn");
const genOutput    = document.getElementById("gen-output");
const lenSlider    = document.getElementById("len-slider");
const lenVal       = document.getElementById("len-val");
const strengthFill = document.getElementById("strength-fill");
const doGen        = document.getElementById("do-gen");
const copyGen      = document.getElementById("copy-gen");
const genCopied    = document.getElementById("gen-copied");

// --- Init ---
(async () => {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  currentUrl = tab?.url || "";
  try { domainLabel.textContent = "🌐 " + new URL(currentUrl).hostname; } catch {}

  const session = await chrome.storage.session.get("vaultSession");
  if (session.vaultSession) {
    vault = session.vaultSession;
    showUnlocked();
  }
})();

// --- Unlock ---
unlockBtn.addEventListener("click", handleUnlock);
masterPwInput.addEventListener("keydown", (e) => { if (e.key === "Enter") handleUnlock(); });

async function handleUnlock() {
  const pw = masterPwInput.value.trim();
  if (!pw) { showError("Enter your master password."); return; }
  unlockError.textContent = "";
  unlockBtn.disabled = true;
  unlockBtn.textContent = "Unlocking…";

  try {
    const stored = await chrome.storage.local.get("vault_encrypted");
    if (!stored.vault_encrypted) {
      // No vault synced to browser — instruct user
      showError("No vault found. Open the SecureVault app and enable browser sync.");
      return;
    }
    const decrypted = await decryptVault(stored.vault_encrypted, pw);
    vault = JSON.parse(decrypted);
    await chrome.storage.session.set({ vaultSession: vault });
    showUnlocked();
  } catch {
    showError("Incorrect password or corrupted vault.");
  } finally {
    unlockBtn.disabled = false;
    unlockBtn.textContent = "Unlock";
  }
}

function showError(msg) { unlockError.textContent = msg; }

// --- Lock ---
lockBtn.addEventListener("click", async () => {
  vault = [];
  await chrome.storage.session.remove("vaultSession");
  showLocked();
});

function showLocked()   { lockedView.classList.remove("hidden"); unlockedView.classList.add("hidden"); genView.classList.add("hidden"); }
function showUnlocked() { lockedView.classList.add("hidden"); unlockedView.classList.remove("hidden"); genView.classList.add("hidden"); renderList(searchInput.value); }
function showGen()      { lockedView.classList.add("hidden"); unlockedView.classList.add("hidden"); genView.classList.remove("hidden"); generate(); }

// --- Credential List ---
searchInput.addEventListener("input", () => renderList(searchInput.value));

function renderList(query) {
  let items = vault;
  if (query.trim()) {
    const q = query.toLowerCase();
    items = vault.filter(e =>
      e.Title?.toLowerCase().includes(q) ||
      e.Username?.toLowerCase().includes(q) ||
      e.Url?.toLowerCase().includes(q)
    );
  }
  // Sort by URL match first
  try {
    const domain = new URL(currentUrl).hostname.replace(/^www\./, "");
    items = [
      ...items.filter(e => (e.Url || "").toLowerCase().includes(domain)),
      ...items.filter(e => !(e.Url || "").toLowerCase().includes(domain))
    ];
  } catch {}

  credList.innerHTML = "";
  if (items.length === 0) {
    credList.innerHTML = '<p style="color:var(--muted);text-align:center;padding:20px 0;font-size:13px;">No passwords found</p>';
    return;
  }
  items.slice(0, 50).forEach(entry => {
    const initial = (entry.Title || "?")[0].toUpperCase();
    const el = document.createElement("div");
    el.className = "cred-item";
    el.innerHTML = `
      <div class="cred-avatar">${initial}</div>
      <div class="cred-info">
        <div class="cred-title">${escHtml(entry.Title || "Untitled")}</div>
        <div class="cred-user">${escHtml(entry.Username || "")}</div>
      </div>
      <div class="cred-actions">
        <button class="cred-action-btn" title="Auto-fill" data-fill="${escHtml(entry.Id)}">⬇️</button>
        <button class="cred-action-btn" title="Copy password" data-copy="${escHtml(entry.Id)}">📋</button>
      </div>`;
    el.querySelector("[data-fill]").addEventListener("click", (e) => { e.stopPropagation(); fillEntry(entry); });
    el.querySelector("[data-copy]").addEventListener("click", (e) => { e.stopPropagation(); copyPassword(entry); });
    el.addEventListener("click", () => fillEntry(entry));
    credList.appendChild(el);
  });
}

async function fillEntry(entry) {
  const pw = await getPlainPassword(entry);
  await chrome.runtime.sendMessage({
    action: "fill_credentials",
    username: entry.Username,
    password: pw
  });
  window.close();
}

async function copyPassword(entry) {
  const pw = await getPlainPassword(entry);
  await chrome.runtime.sendMessage({ action: "copy_to_clipboard", text: pw, clearAfter: 30 });
}

async function getPlainPassword(entry) {
  // Decrypt the entry's password field using the stored master key
  if (!masterKey) {
    const pw = masterPwInput.value || prompt("Master password:");
    if (!pw) throw new Error("No password");
    const stored = await chrome.storage.local.get("vault_encrypted");
    const saltB64 = stored.vault_encrypted.split(":")[0];
    const salt = base64ToBuffer(saltB64);
    masterKey = await deriveKey(pw, salt);
  }
  return await decryptField(entry.EncryptedPassword, masterKey);
}

// --- Generator ---
genBtn.addEventListener("click", showGen);
genBack.addEventListener("click", showUnlocked);
lenSlider.addEventListener("input", () => { lenVal.textContent = lenSlider.value; generate(); });
document.querySelectorAll("#opt-upper,#opt-digits,#opt-symbols").forEach(el => el.addEventListener("change", generate));
doGen.addEventListener("click", generate);
copyGen.addEventListener("click", async () => {
  const pw = genOutput.textContent;
  if (!pw || pw === "Click Generate") return;
  await chrome.runtime.sendMessage({ action: "copy_to_clipboard", text: pw, clearAfter: 30 });
  genCopied.classList.remove("hidden");
  setTimeout(() => genCopied.classList.add("hidden"), 3000);
});

function generate() {
  const opts = {
    length: +lenSlider.value,
    upper: document.getElementById("opt-upper").checked,
    digits: document.getElementById("opt-digits").checked,
    symbols: document.getElementById("opt-symbols").checked
  };
  chrome.runtime.sendMessage({ action: "generate_password", options: opts }, (res) => {
    const pw = res?.password || "";
    genOutput.textContent = pw;
    updateStrengthBar(scorePassword(pw));
  });
}

function scorePassword(pw) {
  if (!pw) return 0;
  let s = 0;
  if (pw.length >= 8) s += 10; if (pw.length >= 12) s += 10;
  if (pw.length >= 16) s += 10; if (pw.length >= 20) s += 10;
  if (/[a-z]/.test(pw)) s += 10; if (/[A-Z]/.test(pw)) s += 10;
  if (/[0-9]/.test(pw)) s += 10; if (/[^a-zA-Z0-9]/.test(pw)) s += 20;
  if (!(/(.)\1{2,}/.test(pw))) s += 10;
  return Math.min(s, 100);
}

function updateStrengthBar(score) {
  const colors = { 80: "#a6e3a1", 60: "#89b4fa", 40: "#f9e2af", 20: "#fab387", 0: "#f38ba8" };
  const color = Object.entries(colors).find(([k]) => score >= +k)?.[1] || "#f38ba8";
  strengthFill.style.width = `${score}%`;
  strengthFill.style.background = color;
}

// --- Crypto helpers (Web Crypto API) ---
async function deriveKey(password, salt) {
  const enc = new TextEncoder();
  const keyMaterial = await crypto.subtle.importKey("raw", enc.encode(password), "PBKDF2", false, ["deriveKey"]);
  return crypto.subtle.deriveKey(
    { name: "PBKDF2", salt, iterations: 600000, hash: "SHA-256" },
    keyMaterial,
    { name: "AES-GCM", length: 256 },
    false,
    ["decrypt"]
  );
}

async function decryptVault(blob, password) {
  const sep = blob.indexOf(":");
  const salt = base64ToBuffer(blob.slice(0, sep));
  const encrypted = blob.slice(sep + 1);
  const key = await deriveKey(password, salt);
  return decryptField(encrypted, key);
}

async function decryptField(encryptedB64, key) {
  const combined = base64ToBuffer(encryptedB64);
  const nonce = combined.slice(0, 12);
  const data = combined.slice(12);
  const decrypted = await crypto.subtle.decrypt({ name: "AES-GCM", iv: nonce }, key, data);
  return new TextDecoder().decode(decrypted);
}

function base64ToBuffer(b64) {
  const binary = atob(b64);
  const buf = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) buf[i] = binary.charCodeAt(i);
  return buf;
}

function escHtml(s) {
  return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

// --- Open App ---
openAppBtn.addEventListener("click", () => {
  chrome.tabs.create({ url: "https://securevault.app" });
});

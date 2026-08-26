const mappings = [
  { id: 331, glyph: "YS", accent: "#9e8cff", type: "human", source: { ja: "ファースト・ヤ・シュトラ", en: "Wind-up Y'shtola" }, target: "Y'shtola · Battle NPC 13910" },
  { id: 232, glyph: "SK", accent: "#69c9ff", type: "monster", source: { ja: "マメット・スカアハ", en: "Wind-up Scathach" }, target: "Scathach · Battle NPC 6479" },
  { id: 218, glyph: "AL", accent: "#ff8db8", type: "human", source: { ja: "ニュー・アリゼー", en: "Dress-up Alisaie" }, target: "Alisaie · Event NPC 1017687" },
  { id: 398, glyph: "GA", accent: "#8f83ff", type: "demihuman", source: { ja: "マメット・ガイア", en: "Wind-up Gaia" }, target: "Gaia · Battle NPC 17830" },
  { id: 534, glyph: "PP", accent: "#f4b46d", type: "human", source: { ja: "マメット・ペルペル", en: "Mammet Pelupelu" }, target: "Quiet Pelupelu · Event NPC 1046564" },
  { id: 325, glyph: "FR", accent: "#72d9d1", type: "demihuman", source: { ja: "マメット・フラン", en: "Wind-up Fran" }, target: "Fran · Event NPC 1025589" },
  { id: 298, glyph: "ZH", accent: "#65d5ff", type: "human", source: { ja: "マメット・シロ", en: "Wind-up Zhloe" }, target: "Zhloe Aliapoh · Event NPC 1044638" },
  { id: 394, glyph: "2B", accent: "#d4d7e3", type: "human", source: { ja: "オートマトン２Ｂ", en: "Automaton 2B" }, target: "2B · Event NPC 1033925" },
  { id: 395, glyph: "2P", accent: "#f0d9c8", type: "human", source: { ja: "オートマトン２Ｐ", en: "Automaton 2P" }, target: "2P · Battle NPC 11366" },
  { id: 98, glyph: "MI", accent: "#e5a4ff", type: "human", source: { ja: "マメット・ミンフィリア", en: "Wind-up Minfilia" }, target: "Minfilia · Event NPC 1006573" },
];

const translations = {
  ja: {
    roster: "ミニオン一覧", localOnly: "このモックはローカル専用です", localNote: "外部通信・公開処理はありません",
    runtime: "Dalamud Dev Plugin", online: "稼働中", settings: "Settings", language: "UI言語", eyebrow: "APPEARANCE MAPPINGS",
    title: "ミニオン・ロスター", subtitle: "呼び出したミニオンごとに、表示するNPCの外見を切り替えます。",
    enabledMappings: "有効なマッピング", searchLabel: "ミニオンを検索", searchPlaceholder: "ミニオン名・NPC名で検索",
    gridView: "カード表示", listView: "リスト表示", disableAll: "一括OFF", enableAll: "一括ON", all: "すべて",
    human: "ヒューマン", demihuman: "デミヒューマン", monster: "モンスター", enabled: "ON", disabled: "OFF",
    noResults: "該当するミニオンがありません", noResultsHint: "検索語を短くするか、フィルターを「すべて」に戻してください。",
    reset: "条件をリセット", mockLabel: "Interactive GUI mock · Local only", target: "変換先", results: "{shown} / {total} 件",
    allEnabled: "すべてのマッピングをONにしました", allDisabled: "すべてのマッピングをOFFにしました", changed: "{name} を{state}にしました",
  },
  en: {
    roster: "Minion roster", localOnly: "This mock stays on your device", localNote: "No publishing or external requests",
    runtime: "Dalamud Dev Plugin", online: "Online", settings: "Settings", language: "UI language", eyebrow: "APPEARANCE MAPPINGS",
    title: "Minion Roster", subtitle: "Choose which NPC appearance is shown for each summoned minion.",
    enabledMappings: "Enabled mappings", searchLabel: "Search minions", searchPlaceholder: "Search minions or NPCs",
    gridView: "Card view", listView: "List view", disableAll: "Disable all", enableAll: "Enable all", all: "All",
    human: "Human", demihuman: "DemiHuman", monster: "Monster", enabled: "ON", disabled: "OFF",
    noResults: "No matching minions", noResultsHint: "Try a shorter query or return the filter to All.", reset: "Reset filters",
    mockLabel: "Interactive GUI mock · Local only", target: "Target", results: "{shown} of {total}",
    allEnabled: "All mappings enabled", allDisabled: "All mappings disabled", changed: "{name} switched {state}",
  },
  de: {
    roster: "Begleiterliste", localOnly: "Dieses Mockup bleibt lokal", localNote: "Keine Veröffentlichung oder externen Anfragen",
    runtime: "Dalamud Dev Plugin", online: "Aktiv", settings: "Einstellungen", language: "UI-Sprache", eyebrow: "ERSCHEINUNGSZUORDNUNGEN",
    title: "Begleiterliste", subtitle: "Lege fest, welches NPC-Erscheinungsbild für jeden Begleiter angezeigt wird.",
    enabledMappings: "Aktive Zuordnungen", searchLabel: "Begleiter suchen", searchPlaceholder: "Begleiter oder NPC suchen",
    gridView: "Kartenansicht", listView: "Listenansicht", disableAll: "Alle deaktivieren", enableAll: "Alle aktivieren", all: "Alle",
    human: "Human", demihuman: "Demi-Human", monster: "Monster", enabled: "AN", disabled: "AUS",
    noResults: "Keine passenden Begleiter", noResultsHint: "Verkürze die Suche oder wähle wieder Alle.", reset: "Filter zurücksetzen",
    mockLabel: "Interaktives GUI-Mockup · Nur lokal", target: "Ziel", results: "{shown} von {total}",
    allEnabled: "Alle Zuordnungen aktiviert", allDisabled: "Alle Zuordnungen deaktiviert", changed: "{name}: {state}",
  },
  fr: {
    roster: "Liste des mascottes", localOnly: "Cette maquette reste locale", localNote: "Aucune publication ni requête externe",
    runtime: "Dalamud Dev Plugin", online: "Actif", settings: "Paramètres", language: "Langue de l'interface", eyebrow: "APPARENCES ASSOCIÉES",
    title: "Liste des mascottes", subtitle: "Choisissez l'apparence de PNJ affichée pour chaque mascotte invoquée.",
    enabledMappings: "Associations actives", searchLabel: "Rechercher une mascotte", searchPlaceholder: "Rechercher une mascotte ou un PNJ",
    gridView: "Vue cartes", listView: "Vue liste", disableAll: "Tout désactiver", enableAll: "Tout activer", all: "Tout",
    human: "Humain", demihuman: "Demi-humain", monster: "Monstre", enabled: "ON", disabled: "OFF",
    noResults: "Aucune mascotte correspondante", noResultsHint: "Raccourcissez la recherche ou revenez au filtre Tout.", reset: "Réinitialiser",
    mockLabel: "Maquette GUI interactive · Locale uniquement", target: "Cible", results: "{shown} sur {total}",
    allEnabled: "Toutes les associations sont activées", allDisabled: "Toutes les associations sont désactivées", changed: "{name} : {state}",
  },
};

const savedState = JSON.parse(localStorage.getItem("mtnpc-mock-state") || "{}");
const state = {
  language: savedState.language || "ja",
  filter: "all",
  query: "",
  view: savedState.view || "grid",
  enabled: Object.fromEntries(mappings.map((mapping) => [mapping.id, savedState.enabled?.[mapping.id] ?? true])),
};

const elements = {
  grid: document.querySelector("#mappingGrid"),
  empty: document.querySelector("#emptyState"),
  search: document.querySelector("#searchInput"),
  language: document.querySelector("#languageSelect"),
  enabledCount: document.querySelector("#enabledCount"),
  totalCount: document.querySelector("#totalCount"),
  progress: document.querySelector("#progressBar"),
  resultCount: document.querySelector("#resultCount"),
  toast: document.querySelector("#toast"),
  gridView: document.querySelector("#gridViewButton"),
  listView: document.querySelector("#listViewButton"),
};

let toastTimer;

function t(key, replacements = {}) {
  let value = translations[state.language][key] || translations.en[key] || key;
  Object.entries(replacements).forEach(([name, replacement]) => {
    value = value.replace(`{${name}}`, replacement);
  });
  return value;
}

function sourceName(mapping) {
  return mapping.source[state.language] || mapping.source.en;
}

function save() {
  localStorage.setItem("mtnpc-mock-state", JSON.stringify({ language: state.language, view: state.view, enabled: state.enabled }));
}

function showToast(message) {
  clearTimeout(toastTimer);
  elements.toast.textContent = message;
  elements.toast.classList.add("is-visible");
  toastTimer = setTimeout(() => elements.toast.classList.remove("is-visible"), 1900);
}

function visibleMappings() {
  const query = state.query.trim().toLocaleLowerCase(state.language);
  return mappings.filter((mapping) => {
    const matchesType = state.filter === "all" || mapping.type === state.filter;
    const haystack = `${sourceName(mapping)} ${mapping.source.ja} ${mapping.source.en} ${mapping.target}`.toLocaleLowerCase(state.language);
    return matchesType && (!query || haystack.includes(query));
  });
}

function renderCard(mapping) {
  const enabled = state.enabled[mapping.id];
  const article = document.createElement("article");
  article.className = `mapping-card${enabled ? "" : " is-disabled"}`;
  article.style.setProperty("--card-accent", mapping.accent);
  article.dataset.id = mapping.id;

  article.innerHTML = `
    <div class="card-top">
      <div class="minion-art" aria-hidden="true"><span>${mapping.glyph}</span></div>
      <div class="card-copy">
        <h2 title="${sourceName(mapping)}">${sourceName(mapping)}</h2>
        <p>${t("target")} · <span>${mapping.target}</span></p>
      </div>
      <label class="switch">
        <span class="sr-only">${sourceName(mapping)} ${enabled ? t("enabled") : t("disabled")}</span>
        <input type="checkbox" ${enabled ? "checked" : ""} data-mapping-id="${mapping.id}">
        <span class="switch-track" aria-hidden="true"></span>
      </label>
    </div>
    <div class="card-meta">
      <span class="type-badge">${t(mapping.type)}</span>
      <span class="state-label">${enabled ? t("enabled") : t("disabled")}</span>
    </div>`;

  article.querySelector("input").addEventListener("change", (event) => {
    state.enabled[mapping.id] = event.target.checked;
    save();
    render();
    showToast(t("changed", { name: sourceName(mapping), state: event.target.checked ? t("enabled") : t("disabled") }));
  });

  return article;
}

function updateCopy() {
  document.documentElement.lang = state.language;
  document.querySelectorAll("[data-i18n]").forEach((element) => { element.textContent = t(element.dataset.i18n); });
  document.querySelectorAll("[data-i18n-placeholder]").forEach((element) => { element.placeholder = t(element.dataset.i18nPlaceholder); });
  elements.language.value = state.language;
}

function updateSummary(shown) {
  const enabled = Object.values(state.enabled).filter(Boolean).length;
  elements.enabledCount.textContent = enabled;
  elements.totalCount.textContent = mappings.length;
  elements.progress.style.width = `${(enabled / mappings.length) * 100}%`;
  elements.resultCount.textContent = t("results", { shown: shown.length, total: mappings.length });
}

function updateViewButtons() {
  const isGrid = state.view === "grid";
  elements.grid.classList.toggle("is-list", !isGrid);
  elements.gridView.classList.toggle("is-active", isGrid);
  elements.listView.classList.toggle("is-active", !isGrid);
  elements.gridView.setAttribute("aria-pressed", String(isGrid));
  elements.listView.setAttribute("aria-pressed", String(!isGrid));
}

function render() {
  updateCopy();
  const shown = visibleMappings();
  elements.grid.replaceChildren(...shown.map(renderCard));
  elements.grid.hidden = shown.length === 0;
  elements.empty.hidden = shown.length !== 0;
  updateSummary(shown);
  updateViewButtons();
}

document.querySelector("#enableAllButton").addEventListener("click", () => {
  mappings.forEach((mapping) => { state.enabled[mapping.id] = true; });
  save();
  render();
  showToast(t("allEnabled"));
});

document.querySelector("#disableAllButton").addEventListener("click", () => {
  mappings.forEach((mapping) => { state.enabled[mapping.id] = false; });
  save();
  render();
  showToast(t("allDisabled"));
});

document.querySelectorAll(".filter-chip").forEach((button) => {
  button.addEventListener("click", () => {
    state.filter = button.dataset.filter;
    document.querySelectorAll(".filter-chip").forEach((chip) => {
      const active = chip === button;
      chip.classList.toggle("is-active", active);
      chip.setAttribute("aria-pressed", String(active));
    });
    render();
  });
});

elements.search.addEventListener("input", (event) => {
  state.query = event.target.value;
  render();
});

elements.language.addEventListener("change", (event) => {
  state.language = event.target.value;
  save();
  render();
  document.querySelector("#settingsMenu").hidden = true;
  document.querySelector("#settingsButton").setAttribute("aria-expanded", "false");
});

const settingsButton = document.querySelector("#settingsButton");
const settingsMenu = document.querySelector("#settingsMenu");

settingsButton.addEventListener("click", () => {
  const opening = settingsMenu.hidden;
  settingsMenu.hidden = !opening;
  settingsButton.setAttribute("aria-expanded", String(opening));
  if (opening) settingsMenu.querySelector("select").focus();
});

document.addEventListener("click", (event) => {
  if (settingsMenu.hidden || event.target.closest(".menu-bar")) return;
  settingsMenu.hidden = true;
  settingsButton.setAttribute("aria-expanded", "false");
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape" || settingsMenu.hidden) return;
  settingsMenu.hidden = true;
  settingsButton.setAttribute("aria-expanded", "false");
  settingsButton.focus();
});

document.querySelectorAll("[data-view]").forEach((button) => {
  button.addEventListener("click", () => {
    state.view = button.dataset.view;
    save();
    updateViewButtons();
  });
});

document.querySelector("#resetSearchButton").addEventListener("click", () => {
  state.query = "";
  state.filter = "all";
  elements.search.value = "";
  document.querySelectorAll(".filter-chip").forEach((chip) => {
    const active = chip.dataset.filter === "all";
    chip.classList.toggle("is-active", active);
    chip.setAttribute("aria-pressed", String(active));
  });
  render();
  elements.search.focus();
});

document.addEventListener("keydown", (event) => {
  if (event.key === "/" && document.activeElement !== elements.search) {
    event.preventDefault();
    elements.search.focus();
  }
});

render();

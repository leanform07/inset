---
version: 1
slug: "src-lookup-ui-popupwindow-xaml"
primary_target: "src/LookUp/UI/PopupWindow.xaml"
related_targets: ["src/LookUp/UI/SearchWindow.xaml","src/LookUp/UI/NotebookWindow.xaml","src/LookUp/UI/SettingsWindow.xaml","src/LookUp/Web/reader.css"]
---

## Scope

All LookUp windows as one surface family: the lookup popup (WPF shell + WebView2 entry styled by `Web/reader.css`), search box, notebook, settings, name prompt, tray menu; light and dark. Visitor mode: **Operate**. Content and behaviour stay as built; this pass replaces the look.

Audience and job: Traditional Chinese readers of English (papers, exam prep, everyday reading) who select a word, read the entry beside it, optionally save it, and return to their text. The notebook is used later to review and categorise.

Must not feel: flashy or web-like, system-default plain, small-type and hard to read, or like another dictionary app. The popup keeps the full entry (not a collapsed summary).

## Direction contract

THESIS: Every LookUp window is a plate from a design annual: near-white stock, one ink, one grotesque, hairline grid, numbered mono labels, registration crosshairs at the plate corners. The entry is the plate. It refuses the dictionary-app default of a blue header, bold blue headwords, rounded cards and coloured chips.

OWN-WORLD: Ground drifts #FAF6F5 → #F5F4F6 → #EEF3EF (dark: #17181A → #1A1B1E → #181B1A). Ink #16181A, never pure black (dark #ECEBE8); secondary ink #5E6166; hairlines #C9C7C4 at 1px (dark #3A3B3F). One accent only: the periwinkle seal #DCDDF3 (dark fill #33365A, ring text #C3C6F5), used for the seal and the matched phrase and nothing else; selection is neutral (a deeper ground with an ink rule). Geist (bundled, OFL) for all English, display set at regular weight with -0.02em tracking; Microsoft JhengHei UI for Chinese; Segoe UI for IPA only (Geist has no IPA glyphs); Geist Mono 11px caps for labels and tokens; Segoe Fluent Icons for icons. Controls are square: filled-ink primary, 1px-outline secondary and inputs, mono text actions on a hairline rule; a single-line field may instead sit on a 1px ink baseline (search box, name prompt). Navigation marks the current item with an ink dot.

STORY: The reader selects a word and the plate opens beside it: headword, pronunciation, numbered senses each carrying its Chinese. One keystroke saves it; the seal is stamped; they keep reading. Later the notebook reads like the annual's index of plates.

FIRST VIEWPORT: Popup 440×560. A 32px mono token bar across the top (left: `EN · 中文`, or `EN ONLY` for English-only entries; right: `LOOKED UP 03` once a word repeats). The plate below on the warm ground with crosshairs at its two top corners: headword 34px regular; a mono caps row of part of speech and `UK /…/  US /…/` with the speaker buttons; senses numbered `01 02 03` in a 32px mono column, the guideword as a mono caps label, the English definition 15.5px ink and its Chinese 16px directly beneath, examples in secondary ink against a hairline. A 44px bottom bar: filled-ink `Add to notebook` with `Ctrl+S` on the left, mono `CAMBRIDGE ↗` on the right. Signature move: saving stamps the periwinkle seal, its text ring reading `NOTEBOOK · <CATEGORY> · <DATE>`, half over the plate's top-right corner with a 160ms press; saved entries keep the seal on every later lookup. The source's endlessly rotating stamp is deliberately not carried over: constant motion fails the reading task.

FORM: Dealt challenger "design annual's plate section" (catalog `design-annual-s-plate-section`), chosen by the user over the assigned 注音標註 direction; seed key ac7d30a7.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

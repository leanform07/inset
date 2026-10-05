# Product

<!-- impeccable:product-schema 1 -->

## Platform

windows

## Users

People who read English in Traditional Chinese and want the meaning of a word without leaving what they are reading. Confirmed situations, all equally primary:

- reading English professional literature — papers, PDFs, architecture and other specialist material, often for long stretches;
- preparing for English exams such as IELTS, where looked-up words should be remembered afterwards;
- everyday work and web reading — email, web pages, Notion, documents.

The author is the first user. A free public release is planned for anyone who reads Traditional Chinese (Taiwan, Hong Kong and elsewhere).

## Product Purpose

A Windows equivalent of macOS "Look Up": select English text in any app, press a shortcut, and a small window next to the cursor shows the Cambridge English–Traditional Chinese entry — pronunciation, part of speech, English definition, Chinese translation, examples. Words worth keeping go into a personal notebook with categories, familiarity and notes, and every lookup is counted so frequently looked-up words surface.

Success: select → shortcut → understand → keep reading, without switching apps, copying, or opening a browser. The tool should feel like Windows already had it, not like a separate dictionary program.

## Positioning

Instant, in-place lookup over the real Cambridge English–Chinese (Traditional) dictionary, read live from the Cambridge site in an embedded browser rather than from a bundled or scraped database; plus a lightweight personal notebook fed directly by lookups. It is deliberately not a full dictionary suite like Eudic.

## Operating Context

- Lives in the Windows notification area; summoned by global shortcuts (defaults Ctrl+Alt+D look up selection, Ctrl+Alt+F search box, Ctrl+Alt+N notebook; all user-configurable).
- The lookup popup opens beside the cursor over whatever the user is reading (PDF reader, Word, browser, Notion, VS Code), is dismissed with Esc or a click outside, and must not lose the user's place.
- The search box is the fallback when no text can be selected.
- The notebook window is used separately, for review, categorising and CSV export (Excel / Anki).
- Users typically have a Chinese IME (Bopomofo) active.

## Capabilities and Constraints

- Native Windows 11 desktop app: C# / .NET 10 / WPF with the Fluent theme (`ThemeMode`), light, dark and follow-Windows themes. Windows 10 support not targeted.
- Dictionary content is the Cambridge web page shown in WebView2 and reduced to the entry by an injected script and stylesheet; its HTML can change without notice. Content styling is constrained to restyling Cambridge's own markup.
- First use passes a Cloudflare check that must stay visible to the user; the app never automates or bypasses it.
- Cambridge's search falls back to its English-only dictionary for words missing from English–Chinese; such entries have no Chinese.
- Phrase searches may land on a head word's entry; the matching phrase block is highlighted.
- Selection is read via UI Automation, falling back to a simulated copy with the clipboard restored; terminals and apps running as administrator are not supported.
- Notebook data stays local (`%AppData%\Inset\notebook.json`); no accounts, sync, or cloud.
- UI copy is English; content includes Traditional Chinese.
- Explicit non-goals: offline dictionary, bulk scraping, AI translation, flashcards / spaced repetition, accounts, cross-platform, browser extension.

## Brand Commitments

- The name is **Inset** (a printer's term: a small plate set into a page), chosen in October 2026 to replace the placeholder "LookUp", which an existing dictionary app already uses. Still keep the name out of the icon: the mark is a registration crosshair, no letters.
- Unofficial tool: never imply affiliation with Cambridge; do not use Cambridge's logo or marks. Keep the "Open in Cambridge" link and Cambridge's source line visible on entries.

## Evidence on Hand

No users, testimonials, metrics or press exist yet; do not invent any. Real content comes live from Cambridge Dictionary. Project docs: `docs/PLAN.md`, `docs/PHASE0_RESULTS.md`, `docs/TESTING.md`.

## Product Principles

1. Speed over features: anything that slows select → shortcut → understand does not ship.
2. Stay out of the way: small, beside the cursor, gone with Esc; the reading in front of the user matters more than the tool.
3. Feel native to Windows: behave and look like part of the system, not like a website in a window.
4. Learning is opt-in: saving a word is one keystroke; everything else (category, status, note) is optional and can happen later.
5. Honest about the source: the content is Cambridge's, clearly credited, never presented as the app's own.

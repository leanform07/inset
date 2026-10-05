---
name: Inset
description: A design annual's plate section, set beside the cursor. Near-white stock, one ink, one grotesque, hairline rules, and a single periwinkle seal.
colors:
  ground-warm: "#FAF6F5"
  ground: "#F5F4F6"
  ground-cool: "#EEF3EF"
  rail: "#EEF3EF"
  ink: "#16181A"
  ink-hover: "#2E3135"
  ink-secondary: "#5E6166"
  ink-muted: "#686B71"
  inverse-ink: "#FAF6F5"
  press-fill: "#ECE9E7"
  hover-fill: "#F2EFED"
  selection: "#D9D6D3"
  hairline: "#C9C7C4"
  field-border: "#B5B3B0"
  danger: "#B3261E"
  seal: "#DCDDF3"
  seal-ink: "#3E4291"
  seal-ink-secondary: "#4A4E73"
  night-ground-warm: "#1B1A1B"
  night-ground: "#18191B"
  night-ground-cool: "#161A19"
  night-rail: "#1D2120"
  night-ink: "#ECEBE8"
  night-ink-hover: "#D4D3D0"
  night-ink-secondary: "#A9ABAF"
  night-ink-muted: "#8C8F94"
  night-inverse-ink: "#17181A"
  night-press-fill: "#26272A"
  night-hover-fill: "#202124"
  night-selection: "#3A3B3F"
  night-hairline: "#3A3B3F"
  night-field-border: "#55575C"
  night-danger: "#F2B8B5"
  night-seal: "#33365A"
  night-seal-ink: "#C3C6F5"
  night-seal-ink-secondary: "#B9BCE6"
typography:
  display:
    fontFamily: "Geist, Microsoft JhengHei UI, sans-serif"
    fontSize: "34px"
    fontWeight: 400
    lineHeight: "38px"
    letterSpacing: "-0.02em"
  title:
    fontFamily: "Geist, Microsoft JhengHei UI, sans-serif"
    fontSize: "22px"
    fontWeight: 400
  section:
    fontFamily: "Geist, Microsoft JhengHei UI, sans-serif"
    fontSize: "17px"
    fontWeight: 400
  definition:
    fontFamily: "Geist, Microsoft JhengHei UI, sans-serif"
    fontSize: "15.5px"
    fontWeight: 400
    lineHeight: 1.5
  body:
    fontFamily: "Geist, Microsoft JhengHei UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: "21px"
  control:
    fontFamily: "Geist, Microsoft JhengHei UI, sans-serif"
    fontSize: "13px"
    fontWeight: 400
  chinese:
    fontFamily: "Microsoft JhengHei UI, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.6
  ipa:
    fontFamily: "Segoe UI, sans-serif"
    fontSize: "14.5px"
    fontWeight: 400
  label:
    fontFamily: "Geist Mono, Consolas, monospace"
    fontSize: "11px"
    fontWeight: 400
    letterSpacing: "0.12em"
rounded:
  none: "0px"
spacing:
  label-gap: "6px"
  row: "9px"
  control-gap: "10px"
  crosshair-inset: "10px"
  bar-inset: "16px"
  section: "24px"
  plate-inset: "28px"
  plate-top: "30px"
components:
  button-primary:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.inverse-ink}"
    typography: "{typography.control}"
    rounded: "{rounded.none}"
    padding: "7px 14px"
  button-primary-hover:
    backgroundColor: "{colors.ink-hover}"
    textColor: "{colors.inverse-ink}"
  button-secondary:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    typography: "{typography.control}"
    rounded: "{rounded.none}"
    padding: "7px 14px"
  button-secondary-hover:
    backgroundColor: "{colors.press-fill}"
    textColor: "{colors.ink}"
  button-text-action:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    padding: "4px 0"
  segment:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    typography: "{typography.control}"
    rounded: "{rounded.none}"
    padding: "6px 12px"
  segment-hover:
    backgroundColor: "{colors.press-fill}"
  segment-selected:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.inverse-ink}"
  field-outline:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    typography: "{typography.control}"
    rounded: "{rounded.none}"
    height: "32px"
  list-row-selected:
    backgroundColor: "{colors.press-fill}"
    textColor: "{colors.ink}"
    padding: "9px 0"
  token-bar:
    textColor: "{colors.ink-muted}"
    typography: "{typography.label}"
    padding: "0 16px"
    height: "32px"
  action-bar:
    padding: "8px 16px"
    height: "48px"
  context-menu:
    backgroundColor: "{colors.ground}"
    textColor: "{colors.ink}"
    typography: "{typography.control}"
    rounded: "{rounded.none}"
    padding: "4px"
  seal:
    backgroundColor: "{colors.seal}"
    textColor: "{colors.seal-ink}"
    rounded: "50%"
    size: "88px"
---

# Design System: Inset

## Overview

**Creative North Star: "The Plate Section"**

Every Inset window is a plate from a design annual. The stock is an uncoated near-white that drifts warm at the top left to faintly cool at the bottom right; there is one ink, never pure black; one grotesque carries all English, and its mono sets the labels, counts and tokens. Structure comes from 1px hairline rules, not boxes or fills. Registration crosshairs sit at a plate's top corners, the way they would on a printed proof. The dictionary entry is the plate; the notebook reads like the annual's index of plates.

Colour is withheld so it can mean something. The single periwinkle accent exists for the notebook seal (a stamped disc with its text running round the ring and a two-digit count in the middle) and for the phrase a search matched. Everything else, including selection, hover and the primary action, is done in ink and neutral grounds. Controls are square, flat and printed: filled ink for the one primary action, 1px ink outline for secondary actions, mono caps on a hairline rule for quiet text actions.

The system rides on WPF's Fluent ThemeMode and overrides it rather than replacing it: corner radii are zeroed, Fluent's accent keys are re-pointed at ink, and field borders take the palette. Light and dark are the same plate; dark is the plate at night, near-black stock with the ink reversed and the seal deepened.

**Key Characteristics:**
- Gradient stock (warm to cool, top left to bottom right), solid ground for menus and inset panels.
- One ink at three strengths (ink, secondary, muted); no pure black, no pure white.
- 1px hairlines for structure; a 1px ink rule where something is emphasised (column heads, selected row, single-line field baseline, second part-of-speech section).
- Square corners on every control, container and window.
- Geist for English, Geist Mono 11px caps for labels and data, Microsoft JhengHei UI for Chinese, Segoe UI for IPA only.
- One accent, periwinkle, for the seal and the matched phrase and nothing else.

## Colors

A paper-and-ink palette: warm-to-cool near-white stock, graphite ink in three strengths, and a single soft periwinkle held in reserve.

### Primary
- **Seal Periwinkle** (`seal`): the fill of the notebook seal, and the fill and border of a matched phrase block in an entry. Its ink, **Seal Indigo** (`seal-ink`), sets the seal's ring text and count; **Seal Slate** (`seal-ink-secondary`) carries examples and labels on the periwinkle fill so they stay at 4.5:1. Dark: `night-seal`, `night-seal-ink`, `night-seal-ink-secondary`.

### Neutral
- **Warm Stock / Stock / Cool Stock** (`ground-warm`, `ground`, `ground-cool`): the three stops of the window background gradient (0, 0.55, 1 on the diagonal; the web entry uses a 160deg linear gradient over the same stops). `ground` alone is the solid ground for context menus and the popup's note panel.
- **Rail** (`rail`): the notebook's category rail, the cool end of the stock.
- **Graphite Ink** (`ink`): all primary text, the primary button fill, the chosen segment, emphasis rules, focus outlines. `ink-hover` is the primary button's hover.
- **Secondary Ink** (`ink-secondary`): examples, pronunciation, part of speech, unselected navigation items, hover border on fields.
- **Muted Ink** (`ink-muted`): mono labels, sense numbers, crosshairs, placeholders, source lines.
- **Inverse Ink** (`inverse-ink`): text on filled ink.
- **Press Fill** (`press-fill`): hover on outlined controls and suggestion rows; the selected list row.
- **Hover Fill** (`hover-fill`): hover on list rows and navigation items.
- **Selection** (`selection`): text selection in fields and in the entry; neutral, never the accent.
- **Hairline** (`hairline`): every 1px structural rule, window edges, example bars, menu edges, the resting rule under a text action.
- **Field Border** (`field-border`): the 1px outline of text fields, combo boxes and unchosen segments.
- **Danger** (`danger`): inline validation text only (for example a shortcut conflict in Settings).

Each light key has a `night-` counterpart with the same role. Light.xaml and Dark.xaml carry identical key lists; reader.css mirrors them as custom properties and switches on `prefers-color-scheme`.

### Named Rules
**The One Seal Rule.** Periwinkle appears in exactly two places: the notebook seal and the phrase block a search matched. Selection, hover, focus, the current item and the primary action never use it. If a new surface wants colour, it does not get it.

**The Neutral Selection Rule.** A selected row is `press-fill` with its bottom rule turned to ink; text selection is `selection`. Selection reads as the plate being pressed, not tinted.

**The Ink Not Black Rule.** Text and fills use `ink` (and its night counterpart), never #000 or #FFF.

## Typography

**Display Font:** Geist (bundled, SIL OFL), falling back to Microsoft JhengHei UI
**Body Font:** Geist
**Label/Mono Font:** Geist Mono (bundled), falling back to Consolas
**Chinese:** Microsoft JhengHei UI
**IPA:** Segoe UI (Geist has no IPA glyphs)
**Icons:** Segoe Fluent Icons, falling back to Segoe MDL2 Assets

**Character:** A single neutral grotesque at regular weight does all the speaking; size and the mono carry hierarchy, not bold. Chinese is set a half-step larger than the English it translates so the two read as equals.

### Hierarchy
- **Display** (400, 34px, 38px line; -0.02em tracking on the web): the headword, and the title of a status message on the plate. The notebook's detail plate sets it at 30px/34px.
- **Title** (400, 22px): a window's own name, such as "Notebook" at the head of the rail.
- **Section** (400, 17px): group headings inside Settings.
- **Definition** (400, 15.5px, 1.5): the English definition in an entry; 14.5px/22px in the notebook detail.
- **Chinese** (400, 16px, 1.6): the translation directly under each definition; examples' Chinese at 13–13.5px in muted ink. WPF's base Chinese style is 15px/23px.
- **Body** (400, 14px, 21px): general text, navigation items, word rows; examples at 13.5–14px in secondary ink.
- **Control** (400, 13px): button, segment, field and menu text.
- **IPA** (Segoe UI 14–14.5px, secondary ink): pronunciation only.
- **Label** (Geist Mono 11px, uppercase, 0.12em tracking where the platform supports it; muted ink): field labels, column heads, part of speech, sense numbers (`01`, `02`), counts (`03 WORDS`, `LOOKED UP 03`), the token bar, quiet text actions, the source line. Counts are zero-padded to two digits.

### Named Rules
**The Regular Weight Rule.** Headwords and titles are Geist 400. Weight 500 is reserved for a phrase title or a bolded word inside an example; nothing in the UI is bold.

**The Label Is Not An Eyebrow Rule.** Mono caps name a field, a column, a count or a token. They never sit above a heading as a decorative kicker.

**The Right Face For The Script Rule.** English in Geist, Chinese in JhengHei, IPA in Segoe UI. Never let IPA fall back inside Geist.

## Layout

Windows are fixed plates rather than responsive pages. The popup is 440×560 with a 32px token bar on top, the entry in the middle and an action bar of at least 48px at the bottom, each separated by a hairline. The search box is 520 wide and sizes to content; settings 540; the name prompt 380. The notebook is a three-column index (220px rail, flexible word list, 370px detail plate) at a default 1120×700, minimum 820×460.

Plate content is inset 28px at the sides and starts 30px from the top (search and settings use 30–32px sides and 34px top); crosshairs sit 10px in from the top corners. Bars use 16px side padding. Rows are 9px top and bottom with a hairline under each. Paired buttons are 10px apart; a field label sits 6px above its field; groups in Settings are separated by a hairline with 24px either side. Labelled forms use a fixed label column (84px in the popup's note panel, 190px in Settings).

In entries, senses hang from a 40px mono number column; examples indent 12px behind a 1px hairline bar.

## Elevation & Depth

Flat. No shadows are drawn by the system; depth comes from the stock gradient, from hairline rules, and from the solid `ground` set against the gradient for inset panels and menus. Windows carry a 1px hairline edge. Fluent's own popup shadows are left as the platform draws them and are not part of the vocabulary.

### Named Rules
**The Printed Plate Rule.** Separation is a rule, not a shadow or a card. If two regions need dividing, draw a 1px hairline between them; if one needs emphasis, turn that rule to ink.

## Shapes

Every control, field, menu, panel and window is square (radius 0); Fluent's `ControlCornerRadius`, `OverlayCornerRadius` and `PopupCornerRadius` are overridden to 0 in both palettes. Borders are 1px and snap to device pixels. Circles belong to marks only: the seal (an 88px disc rotated -8deg), the 5px navigation dot, and the ring of the registration crosshair (an 18px cross with a 4.5px-radius circle, 1px muted stroke).

## Components

### Buttons
Printed and flat: ink or nothing.
- **Shape:** square (0px).
- **Primary:** filled `ink`, `inverse-ink` text, 13px Geist, 7px 14px padding. Hover `ink-hover`, pressed 0.85 opacity, disabled 0.4. One per view. An inline shortcut hint may follow the label in mono at reduced opacity (`CTRL+S`).
- **Secondary:** transparent with a 1px `ink` outline, ink text, same padding. Hover fills `press-fill`. Used for real secondary actions (Cancel, Remove, Export CSV, New category, Try again, Cambridge in the notebook).
- **Text action:** Geist Mono 11px caps (`CAMBRIDGE`, `SEARCH THE WEB`, `REMOVE FROM NOTEBOOK`) on a 1px `hairline` rule 2px below the text; the rule turns `ink` on hover. An external link carries the Fluent open-in-new icon at 11px.
- **Link:** Geist, ink, underlined on hover; for prose-like actions only.
- **Focus:** every interactive control shows a 1px `ink` rectangle 3px outside its bounds.

### Segmented control
Replaces radio buttons. Outlined cells (1px `field-border`, 6px 12px padding) share borders by overlapping 1px; hover fills `press-fill`; the chosen cell fills `ink` with an `ink` border and `inverse-ink` text. Used for status (New / Learning / Known), status filters, and theme.

### Inputs / Fields
- **Outlined:** 1px `field-border`, transparent background, square; hover border `ink-secondary`, focused border `ink`. Fluent's TextBox and ComboBox take these through overridden keys. The notebook filter is a 32px outlined frame around a chromeless text box with a 12px muted search icon.
- **Baseline:** a single-line field may instead sit on a 1px `ink` rule with no box (the search box at 26px text, the name prompt at 18px).
- **Placeholder:** `ink-muted` hint drawn over the empty field.
- **Error:** 12.5px text in `danger` directly under the field.

### Navigation
The notebook rail lists categories at 14px in `ink-secondary`, each with a two-digit mono count on the right. The current item is marked by a 5px `ink` dot to the left of its label and its text turns `ink`; hover fills `hover-fill`. No filled block marks the current item: filled ink means a primary action or a chosen segment.

### Lists and tables
Rows carry a hairline bottom rule and 9px vertical padding; hover fills `hover-fill`; selected rows fill `press-fill` and their rule turns `ink`. Column heads are mono caps labels over a 1px `ink` rule. Numbers are mono and zero-padded.

### Bars
- **Token bar:** 32px, 16px sides, hairline beneath; mono labels on the left (`EN · 中文` or `EN ONLY`), an ink token on the right (`LOOKED UP 03`).
- **Action bar:** at least 48px, 16px 8px padding, hairline above; primary action left, quiet text action right.

### Context menus
A square plate in `ground` with a 1px `hairline` edge, 4px padding, 13px Geist in ink. The style lives in each palette rather than in Shared.xaml: Fluent draws shortcut text in `TextFillColorDisabledBrush`, which falls below 4.5:1, so each palette overrides it with a literal per-theme colour (`ink-secondary`); a brush placed in `Style.Resources` is shared and would not be re-evaluated on theme change.

### Registration crosshair
An 18px mark (vertical and horizontal stroke through a 4.5px-radius ring), 1px `ink-muted` stroke, not hit-testable, placed 10px in from a plate's top corners (8px in the prompt). In an entry carrying the seal, the right crosshair is hidden and the seal takes the corner.

### Notebook seal (signature)
An 88px `seal` disc rotated -8deg, half over the plate's top-right corner. Its ring carries `NOTEBOOK · <CATEGORY> · <DATE>` in Geist Mono 7.6px caps (0.16em tracking), letters spread evenly so the text closes on itself; the centre holds the lookup count in Geist 22px, zero-padded. All in `seal-ink`. The web draws it as SVG with a `textPath`; WPF draws the same 92-unit geometry in `SealStamp`. Saved words keep the seal on every later lookup. It animates once, when a word is first saved: 180ms from 1.35 scale and -16deg to rest, `cubic-bezier(0.2, 0.9, 0.3, 1)`, skipped under reduced motion. It never rotates or animates otherwise.

### App icon
The registration crosshair, enlarged, on a square plate of the stock: the gradient ground with a 1px `field-border` edge (`hairline` is too faint on a light taskbar), the cross and its ring in `ink`, the ring half the cross wide. No letters, so the name can change without it. `tools/make_icon.py` draws every size from 16 to 256 on whole pixels (1px strokes up to 24, 2px to 64, then size/32); at 16px the ring is placed by hand as a 7px pixel circle, since an anti-aliased one turns grey. The same icon serves the exe, the windows and the notification area.

### Matched phrase
Phrase and idiom blocks inside an entry are outlined insets (1px `hairline`, 12px 14px padding, 40px left indent). The one a phrase search asked for fills with `seal` and its border takes the same colour; its secondary text switches to `seal-ink-secondary`.

## Do's and Don'ts

### Do:
- **Do** keep every control, field, menu and window square (0px radius); keep circles for the seal, the navigation dot and the crosshair ring.
- **Do** divide regions with a 1px `hairline` and emphasise with a 1px `ink` rule.
- **Do** mark the current navigation item with the 5px ink dot and ink text.
- **Do** show selection as `press-fill` with an ink rule, and text selection as `selection`.
- **Do** set labels, counts and tokens in Geist Mono 11px caps, counts zero-padded (`03`).
- **Do** give each view one filled-ink primary action; use the 1px ink outline for real secondary actions and mono text-on-a-rule for quiet ones.
- **Do** keep light and dark palettes on one key list and add any new key to Light.xaml, Dark.xaml and reader.css together.
- **Do** keep the "Open in Cambridge" action and Cambridge's source line visible on entries.

### Don't:
- **Don't** use periwinkle for anything except the seal and the matched phrase: not selection, hover, focus, links or the current item.
- **Don't** fill a navigation item with ink to show it is current; filled ink means a primary action or the chosen segment.
- **Don't** add drop shadows, cards or tinted panels to separate content.
- **Don't** set headwords or titles bold; display type is Geist 400.
- **Don't** put a mono caps label above a heading as an eyebrow.
- **Don't** animate the seal continuously or on every lookup; it stamps once, in 180ms, and honours reduced motion.
- **Don't** use pure black or pure white for ink or ground.
- **Don't** put a shared brush in a style's `Style.Resources` when it must change with the theme; give each palette its own literal.

# Windows Cambridge Dictionary Desktop — Project Specification

## 1. Project Overview

本專案目標是開發一個 **Windows 專用、極簡、快速的英文查字工具**。

核心概念不是製作一個大型字典軟體，而是做成類似 macOS「Look Up」的體驗：

- 在任何軟體中選取英文單字或片語
- 按下全域快捷鍵
- 立即在游標附近跳出字典視窗
- 主要資料來源為 Cambridge Dictionary
- 也可以透過快捷鍵直接叫出搜尋框，手動輸入單字
- 介面極簡、快速，不希望發展成 Eudic 那種功能繁多的大型字典程式

本專案 **不考慮離線字典功能**。

---

# 2. Core Product Goal

建立一個：

> **Windows 版的極簡英文 Look Up 工具，以 Cambridge Dictionary 為主要資料來源。**

使用者在閱讀 PDF、Word、瀏覽器、Notion、VS Code、PowerPoint 等軟體時，不需要切換視窗、不需要複製貼上、不需要打開瀏覽器。

只需要：

```text
選取英文
↓
按下 Alt + D
↓
跳出 Cambridge Dictionary 解釋
```

---

# 3. Design Principles

## 3.1 極簡

只顯示真正需要的資訊：

- Word / Phrase
- UK / US pronunciation
- IPA
- Part of speech
- English definition
- Traditional Chinese translation（如果 Cambridge 有）
- Example sentence
- Cambridge source link

避免加入過多功能。

---

## 3.2 快速

查字流程必須盡可能少步驟。

理想操作：

```text
Select
→ Alt + D
→ Result
```

整個過程不應要求使用者：

- 複製文字
- 開啟瀏覽器
- 切換 App
- 手動貼上
- 再按 Search

---

## 3.3 Popup First

字典結果應以小型 Floating Popup 顯示，而不是開一個大型主視窗。

Popup 應：

- Always on top
- 出現在滑鼠游標附近
- 不搶走太多畫面
- 按 `Esc` 即可關閉
- 點擊 popup 外部可以關閉
- 可自動調整位置，避免跑出螢幕

---

# 4. Main User Flows

---

## Flow A — Select + Hotkey

最重要的使用流程。

例如使用者在 Word 中看到：

```text
The city needs greater resilience.
```

使用者選取：

```text
resilience
```

然後按：

```text
Alt + D
```

程式：

```text
Get selected text
↓
resilience
↓
Search Cambridge
↓
Show popup near cursor
```

Popup 顯示：

```text
resilience
/rɪˈzɪl.i.əns/

noun [U]

the ability to be happy, successful, etc.
again after something difficult or bad has happened

韌性；恢復力

[Example sentence]

Cambridge Dictionary
```

---

# 5. Manual Search Mode

除了選字之外，也要能直接叫出搜尋框。

例如：

```text
Alt + Shift + D
```

或其他不衝突的快捷鍵。

顯示：

```text
┌─────────────────────────────┐
│ Search Cambridge Dictionary │
│                             │
│ resilience_                 │
└─────────────────────────────┘
```

使用者：

```text
輸入文字
→ Enter
→ 顯示結果
```

---

# 6. Global Hotkey

程式需要支援 Windows Global Hotkey。

第一版預設：

```text
Alt + D
```

功能：

```text
Lookup Selected Text
```

第二組：

```text
Alt + Shift + D
```

功能：

```text
Open Search Box
```

未來可以讓使用者自己修改快捷鍵。

---

# 7. Getting Selected Text

這是本專案最重要的 Windows integration 功能。

需要有兩層策略。

---

## Strategy 1 — Windows UI Automation

優先使用 Windows UI Automation 取得目前選取的文字。

流程：

```text
Foreground Window
↓
Focused Element
↓
TextPattern
↓
GetSelection()
↓
Selected Text
```

優點：

- 不影響 Clipboard
- 操作乾淨
- 使用者不會感覺到複製動作

---

## Strategy 2 — Clipboard Fallback

如果 UI Automation 無法取得文字：

```text
Save existing clipboard
↓
Send Ctrl + C
↓
Read clipboard
↓
Restore previous clipboard
```

這個 fallback 用來提高相容性。

---

# 8. Target Application Compatibility

希望至少支援：

- Chrome
- Edge
- Firefox
- Microsoft Word
- PowerPoint
- Notion
- VS Code
- Obsidian
- Adobe Acrobat
- Windows PDF Reader
- 일반 Electron Apps

不需要保證所有程式 100% 支援。

可以透過：

```text
UI Automation
+
Clipboard fallback
```

提高成功率。

---

# 9. Dictionary Data Source

主要資料來源：

```text
Cambridge Dictionary
```

優先使用官方允許的 API / data access 方式。

不要製作：

```text
Cambridge website scraper
```

也不要：

```text
大量抓取網站內容
建立本機 Cambridge database
```

本專案目前 **完全不考慮離線 dictionary database**。

---

# 10. Dictionary Search Logic

查詢流程：

```text
Selected Text
↓
Normalize
↓
Exact Search
↓
Result
```

如果 exact match 找不到：

```text
Exact Search
↓
No Result
↓
Lemmatization
↓
Search Again
```

例如：

```text
running → run
cars → car
children → child
architectures → architecture
```

如果仍然沒有：

```text
Cambridge fuzzy / suggestion
```

最後仍然沒有：

```text
Offer Web Search
```

---

# 11. Phrase Support

不應只支援單字。

例如：

```text
account for
in terms of
with respect to
be subject to
```

都應該可以直接查。

簡單判斷：

```text
1 word
→ Word Lookup

2–6 words
→ Phrase Lookup
```

如果選取大量句子：

```text
> 6 words
```

第一版可以直接顯示：

```text
Selection is too long for dictionary lookup.
```

不要在第一版加入 AI 翻譯功能。

---

# 12. Text Normalization

查詢前處理：

```text
trim whitespace
remove unnecessary punctuation
normalize apostrophes
normalize unicode
lowercase when appropriate
```

例如：

```text
“resilience,”
```

應轉為：

```text
resilience
```

---

# 13. Dictionary Popup UI

Popup 是本 App 最重要的 UI。

建議內容：

```text
┌────────────────────────────────┐
│ resilience                     │
│ /rɪˈzɪl.i.əns/      UK 🔊 US 🔊 │
│                                │
│ noun [U]                       │
│                                │
│ the ability to be happy,       │
│ successful, etc. again after   │
│ something difficult...         │
│                                │
│ 韌性；恢復力                    │
│                                │
│ Example:                       │
│ She showed great resilience... │
│                                │
│ Cambridge Dictionary      ↗    │
└────────────────────────────────┘
```

---

# 14. UI Requirements

UI Style：

- Minimal
- Clean
- Modern
- Large whitespace
- Simple typography
- Avoid unnecessary borders
- Avoid information overload

不要設計成：

- 傳統大型字典 App
- 多欄 layout
- 工具列很多按鈕
- 廣告式 UI
- 複雜設定頁

---

# 15. Popup Behavior

Popup：

- Always on top
- Near mouse cursor
- Smart positioning
- No taskbar entry
- Esc closes
- Click outside closes
- Can scroll when content is long

如果有多個 definition：

```text
Meaning 1
Meaning 2
Meaning 3
```

以垂直列表顯示。

---

# 16. Pronunciation

如果 API 有 pronunciation audio：

需要：

```text
UK 🔊
US 🔊
```

點擊播放。

如果沒有 audio：

只顯示 IPA。

---

# 17. Multiple Meanings

例如：

```text
charge
```

可能有：

```text
noun
verb
```

每個 part of speech 分區顯示。

例如：

```text
charge

NOUN

1.
...

2.
...

VERB

1.
...
```

第一版不需要非常複雜的 dictionary hierarchy。

---

# 18. Cambridge Source Link

Popup 最下方：

```text
Open in Cambridge ↗
```

點擊後：

```text
open default browser
→ Cambridge page
```

---

# 19. No Result State

如果 Cambridge 查不到：

顯示：

```text
No Cambridge Dictionary entry found.

Search the web →
```

也可以顯示：

```text
Did you mean:
resilience
resilient
```

---

# 20. Loading State

API 查詢過程不要讓畫面看起來卡住。

顯示：

```text
Searching…
```

如果超過一定時間：

```text
Still searching…
```

---

# 21. Error Handling

需要處理：

```text
No internet
API timeout
API error
Invalid query
Empty selection
Too long selection
Unsupported application
```

錯誤訊息保持簡短。

例如：

```text
Couldn't get selected text.
Try copying the word or use manual search.
```

---

# 22. System Tray

程式啟動後常駐：

```text
Windows System Tray
```

Tray menu：

```text
Open Dictionary
Settings
Quit
```

不要一直顯示大型主視窗。

---

# 23. Startup

設定頁提供：

```text
Launch at Windows startup
```

預設可以關閉。

---

# 24. Search History

可以加入簡單 History：

```text
resilience
infrastructure
account for
decarbonization
```

儲存在本機。

只需要：

```text
query
timestamp
```

第一版甚至可以不做 UI，只先保留架構。

---

# 25. Favourite

Favourite 不是核心功能。

如果加入：

```text
☆ → ★
```

即可。

不要做成單字學習系統。

---

# 26. Settings

Settings 保持簡單。

第一版：

```text
Hotkey — Lookup Selected Text
Hotkey — Open Search
Launch at startup
Theme
```

Theme：

```text
System
Light
Dark
```

---

# 27. Right-Click Integration

「選取文字 → 右鍵 → Cambridge Dictionary」

不是第一優先功能。

原因：

不同 App 的 context menu implementation 不同：

```text
Chrome
Word
Adobe Acrobat
VS Code
Electron apps
```

沒有一個通用 Windows API 可以可靠地替所有程式加入文字右鍵功能。

因此第一版核心 interaction：

```text
Select
→ Hotkey
```

而不是：

```text
Select
→ Right click
→ Dictionary
```

如果未來針對特定 App 有容易實作的 integration，再另外處理。

---

# 28. Recommended Tech Stack

Windows 專用。

推薦：

```text
C#
.NET 8
WPF
```

理由：

需要大量 Windows native integration：

```text
Global Hotkey
UI Automation
Clipboard
Foreground Window
Mouse Position
System Tray
Always-on-top Window
Windows Startup
```

C# / .NET 比 Electron 更適合這類工具。

---

# 29. Suggested Architecture

```text
DictionaryApp
│
├── App
│
│   ├── Startup
│
├── Hotkeys
│   └── GlobalHotkeyService
│
├── Selection
│   ├── UIAutomationSelectionProvider
│   ├── ClipboardSelectionProvider
│   └── SelectionService
│
├── Dictionary
│   ├── CambridgeClient
│   ├── DictionaryService
│   ├── QueryNormalizer
│   └── Lemmatizer
│
├── UI
│   ├── PopupWindow
│   ├── SearchWindow
│   ├── SettingsWindow
│   └── TrayIcon
│
├── Audio
│   └── PronunciationService
│
├── Storage
│   ├── SettingsStore
│   └── HistoryStore
│
└── Models
    ├── DictionaryEntry
    ├── Definition
    └── Pronunciation
```

---

# 30. Suggested Data Models

```csharp
DictionaryEntry
{
    string Word;
    List<Pronunciation> Pronunciations;
    List<SenseGroup> SenseGroups;
    string SourceUrl;
}
```

```csharp
Pronunciation
{
    string Region;
    string IPA;
    string AudioUrl;
}
```

```csharp
SenseGroup
{
    string PartOfSpeech;
    List<Definition> Definitions;
}
```

```csharp
Definition
{
    string EnglishDefinition;
    string TraditionalChinese;
    List<string> Examples;
}
```

---

# 31. Popup Positioning

Popup should open close to current mouse position.

Pseudo logic：

```text
GetCursorPos()
↓
Calculate popup size
↓
Check monitor bounds
↓
If overflow right
    open left
If overflow bottom
    open above
```

要支援多螢幕。

---

# 32. Performance Goal

使用者感知：

```text
Alt + D
→ popup appears immediately
```

可以先：

```text
show popup loading state
```

再等待 API。

不要等 API response 回來才建立 popup。

---

# 33. Development Roadmap

---

## Phase 1 — Basic Dictionary App

先做一個可以運作的 MVP。

功能：

```text
Alt + Shift + D
↓
Open Search Box
↓
Type Word
↓
Cambridge Search
↓
Show Popup
```

需要完成：

- WPF app
- Cambridge API client
- Search window
- Result popup
- Loading
- Error handling

不要先處理 selected text。

---

## Phase 2 — Selected Text Lookup

加入核心功能：

```text
Select text
↓
Alt + D
↓
Get selected text
↓
Cambridge
↓
Popup
```

完成：

- Global Hotkey
- Windows UI Automation
- Clipboard fallback
- Popup at cursor

---

## Phase 3 — Desktop UX

完成：

```text
System Tray
Esc close
Click outside close
Smart popup position
Startup setting
Dark / Light theme
Hotkey settings
```

---

## Phase 4 — Search Intelligence

加入：

```text
Text normalization
Lemmatization
Phrase detection
Suggestions
No-result fallback
```

---

## Phase 5 — Optional Features

最後再考慮：

```text
History
Favourite
Application-specific integrations
Right-click integration
```

這些都不是 MVP 核心需求。

---

# 34. Explicit Non-Goals

目前不要做：

```text
Offline Dictionary
Local Cambridge database
Cambridge website scraping
AI Translation
Flashcards
Spaced repetition
Vocabulary learning system
Account system
Cloud sync
Browser extension
Mobile app
Cross-platform support
Complex right-click integration
```

---

# 35. MVP Definition

MVP 完成標準：

### Scenario 1

使用者：

```text
Alt + Shift + D
```

輸入：

```text
resilience
```

Enter。

結果：

```text
Cambridge definition popup
```

---

### Scenario 2

使用者在 Chrome 選：

```text
resilience
```

按：

```text
Alt + D
```

結果：

```text
Cambridge definition popup
```

---

### Scenario 3

使用者在 Word 選：

```text
account for
```

按：

```text
Alt + D
```

結果：

```text
Phrase definition popup
```

---

# 36. Acceptance Criteria

MVP 至少應做到：

- [ ] App 可以常駐背景
- [ ] Global hotkey 可以在其他 App 中觸發
- [ ] Manual search 可以正常使用
- [ ] 可以取得 Chrome 選取文字
- [ ] 可以取得 Word 選取文字
- [ ] UI Automation 失敗時使用 Clipboard fallback
- [ ] Cambridge lookup 正常
- [ ] 可以顯示 Word
- [ ] 可以顯示 IPA
- [ ] 可以顯示 Part of Speech
- [ ] 可以顯示 Definition
- [ ] 可以顯示 Traditional Chinese（資料來源有提供時）
- [ ] 可以顯示 Example
- [ ] 可以播放 pronunciation audio（資料來源有提供時）
- [ ] Popup 出現在游標附近
- [ ] Esc 可以關閉
- [ ] 點擊 Popup 外部可以關閉
- [ ] No result 有合理提示
- [ ] Network error 有合理提示

---

# 37. UX Priority

優先順序：

```text
Speed
↓
Simplicity
↓
Readability
↓
Features
```

如果某個 feature 會讓查字流程變慢或 UI 變複雜：

```text
不要做。
```

---

# 38. Important Product Principle

這個 App 不應該變成：

```text
Eudic Clone
```

也不是：

```text
Complete Dictionary Software
```

它應該是：

```text
Instant English Lookup Tool
```

最核心的體驗永遠是：

```text
See word
↓
Select
↓
Hotkey
↓
Understand
↓
Continue reading
```

---

# 39. First Task for Claude Code

請先不要一次實作整個專案。

第一個 milestone：

> 建立一個 Windows WPF App prototype，可以按快捷鍵開啟搜尋框，輸入英文後顯示一個極簡 dictionary popup。

第一階段只需要：

```text
1. 建立 .NET 8 WPF project
2. 建立 SearchWindow
3. 建立 PopupWindow
4. 建立 DictionaryService abstraction
5. 先用 mock dictionary data
6. 完成 UI interaction
7. 完成 keyboard navigation
8. 完成 Esc close
```

先不要實作：

```text
Windows UI Automation
Clipboard fallback
System Tray
History
Settings
Lemmatization
Right-click integration
```

先確認：

```text
Search UX
+
Popup UX
```

是舒服的。

之後再開始 Windows integration。

---

# 40. Development Philosophy

每一個 milestone 都要可以單獨運作。

不要一次建立過多 abstraction。

優先：

```text
Working prototype
↓
Test UX
↓
Refactor
↓
Add integration
```

而不是：

```text
Design entire architecture
↓
Build everything
↓
Finally test
```

---

# Final Product Vision

最終產品應該讓使用者感覺：

> Windows 本來就有這個功能。

而不是感覺：

> 我正在使用另一個字典軟體。

理想情況：

```text
Select "resilience"
↓
Alt + D
↓
0.2 sec
↓
Definition appears
↓
Esc
↓
Continue reading
```

這就是整個產品最重要的體驗。

# Windows 極簡 Cambridge 查字工具：可行性分析與執行計劃書

## Context

使用者和 ChatGPT 討論出一份 spec（`C:\Users\USER\Downloads\windows_cambridge_dictionary_project_spec.md`），想做 Windows 版的 macOS「Look Up」：在任何程式選取英文，按快捷鍵後在游標旁跳出 Cambridge 解釋（含繁中翻譯、IPA、發音），整體要極簡、要快。

已確認的決策：
- 使用範圍：自己用，之後可能**免費公開發佈**
- 資料來源：**每次查詢時讀取 Cambridge 網頁**（不批次抓取、不建資料庫）
- 快捷鍵：**可以自訂**，預設不用 Alt+D

## 進度（2026-10-05）

| 階段 | 狀態 | 說明 |
|---|---|---|
| Phase 0 技術驗證 | ✅ | 見 [PHASE0_RESULTS.md](PHASE0_RESULTS.md) |
| Phase 1 搜尋框 + Popup | ✅ | |
| Phase 1.5 單字筆記 | ✅ | 後來新增的需求，見下方 |
| Phase 2 選字查詢 | ✅ | 記事本實測：UIA 57–133 ms、剪貼簿 208–280 ms，剪貼簿有還原 |
| Phase 3 桌面整合 | ✅ | 系統匣、設定視窗、可自訂快捷鍵、深淺色、開機啟動、單一實例 |
| Phase 4 品質 | ✅（相容性表待填） | 片語定位、英英 fallback 提示；各 App 相容性見 [TESTING.md](TESTING.md) |
| 介面設計 | 🔄 進行中（2026-10-05 暫停） | 用 impeccable design skill 重新設計所有視窗，見下方「介面設計進度」 |
| 打包發佈 | ⏳ | single-file publish、README |

### 介面設計進度（暫停點）

使用 `/impeccable`（design skill）做整體重新設計（redesign：保留功能和內容，舊外觀只當參考）。
- ✅ **init**：已訪談並寫好 `PRODUCT.md`（使用情境：專業文獻 / 考試 / 日常閱讀都是主要情境；發佈對象：所有繁中使用者；名稱 LookUp 是暫定，圖示不要用名字）
- ✅ 確認走 **code-first**（這個環境沒有圖片生成工具），也不適用 live 瀏覽器模式（原生 App）
- ⏭ **下一步**：照 skill 的 `reference/new-work.md` 第 2 步，針對 Operate 介面問 2–3 個問題 → 第 3 步「Create or replace the visual world」→ 執行 `impeccable concept-seed --scope direction --mode operate`，在決策頁選方向 → 寫 direction contract（surface brief）→ 動手前讀 `reference/craft-floor.md` → 實作 → 截圖檢查 → finish reviewer → documenter 寫 DESIGN.md
- 要處理的已知問題：筆記本詳細區的 New / Learning / Known 按鈕被切掉（Fluent RadioButton 有最小寬度）、popup 只有提示訊息時上方空一行、間距不一致、控制項看起來都是預設樣式
- 範圍：查字視窗（含 reader.css）、搜尋框、筆記本、設定、命名對話框、系統匣選單、深淺兩種主題、App 圖示（目前是暫定的藍底 Aa）

實作過程中的決定（和原計劃不同的地方）：
- **預設快捷鍵**：Ctrl+Alt+D 選字查詢、Ctrl+Alt+F 搜尋框、Ctrl+Alt+N 筆記本
- **英英 fallback 不用自己做**：Cambridge 的搜尋在英漢版沒有這個字時，會自己跳到英英版（例如 `deplatform`），App 只需在底部標示「English only」
- **片語**：Cambridge 有時把片語導到主詞條（`be subject to` → subject），reader.js 會找到相符的片語區塊，捲動過去並標示，筆記也存那個片語
- **詞形還原、拼字建議**：都直接用 Cambridge 搜尋的結果（`running` → run），不另外做 NLP
- **不封鎖廣告**：會加速載入，但 Cambridge 的內容靠廣告支持，打算公開發佈的工具不該這樣做；而且廣告區塊本來就被 reader 隱藏了
- **深色模式**：使用 .NET 10 內建的 Windows 11 Fluent 主題（`ThemeMode`），標準控制項自動有深色樣式
- **注音輸入法**：搜尋框固定英文輸入（關閉 IME），否則 Enter 會被輸入法吃掉
- **Cloudflare 驗證期間 WebView 必須是可見的**：隱藏時驗證程式會暫停

---

## 一、可行性分析

### 總結：可以做，但 spec 有兩個前提必須修正

| 項目 | 可行性 | 說明 |
|---|---|---|
| 全域快捷鍵、Tray、Popup、開機啟動 | ✅ 高 | Win32 `RegisterHotKey` 和 WPF 都是成熟做法 |
| 取得選取文字 | ⚠️ 中高 | UIA 只能涵蓋一部分 app，大多要靠剪貼簿 fallback |
| **Cambridge 官方 API** | ❌ 不可用 | 見下方「致命問題 1」 |
| **用 HttpClient 直接抓網頁** | ❌ 不可行 | 見下方「致命問題 2」 |
| 用 WebView2 讀取 Cambridge 頁面 | ⚠️ 預期可行，待驗證 | 這是本計劃採用的路線 |
| 0.2 秒出結果 | ❌ 不實際 | popup 可以立即出現（0.1 秒內），結果大約 0.8–2 秒 |

### 致命問題 1：官方 API 已經沒有了
- `dictionary-api.cambridge.org`（含 terms 頁）現在都 301 轉址到主站，API 計劃實際上已經關閉
- 以前的方案是付費（依瀏覽次數計價），而且要審核，第三方調查也只列「30 天試用」
- 結論：spec 第 9 節「優先使用官方 API」無法實現

### 致命問題 2：Cambridge 站有 Cloudflare 防護
- 實測 `curl` 抓 `dictionary.cambridge.org/dictionary/english-chinese-traditional/resilience`，回傳 **403「Just a moment…」(Cloudflare managed challenge)**
- 所以「用程式直接 HTTP 抓再解析」做不到。繞過 Cloudflare（偽造指紋、自動解 challenge）屬於規避 bot 偵測，本專案**不做**
- **可行解法：WebView2（真正的 Edge 瀏覽器核心）**。每次查詢由使用者觸發，WebView2 像一般瀏覽器那樣載入頁面。cookie 會保存在本機 profile，通過一次 challenge 之後通常可以持續使用。如果 Cloudflare 跳出驗證，就**顯示在 popup 裡由使用者自己點**，程式絕不自動處理
- 這條路線本質上是「一個專用的小瀏覽器，加上閱讀模式」，等同使用者自己開網頁

### 其他技術風險（spec 沒提到）
1. **Alt+D 和瀏覽器衝突**：Chrome/Edge/Firefox 的 Alt+D 是「跳到網址列」；Alt+Shift 是 Windows 切換輸入法 → 預設改用 **Ctrl+Alt+D（選字查詢）/ Ctrl+Alt+F（搜尋框）**，可以在設定裡改
2. **剪貼簿 fallback 的陷阱**：
   - 熱鍵觸發時使用者還按著 Ctrl/Alt，直接送 Ctrl+C 會變成 Ctrl+Alt+C。必須先等修飾鍵放開，或先送出 key-up
   - 在終端機（Windows Terminal、VS Code terminal）送 Ctrl+C 是**中斷程式**。偵測到前景是終端機時要跳過 fallback
   - 暫存的剪貼簿要保留所有格式，不只文字；而且要加上 `ExcludeClipboardContentFromMonitorProcessing`，避免污染 Win+V 歷史
3. **UIA 支援度**：Word 和 Chromium 系瀏覽器支援 TextPattern。Electron（Notion、Obsidian、VS Code 編輯器）和 Acrobat 不穩定 → UIA 設 150ms timeout，失敗就改走剪貼簿
4. **權限隔離 (UIPI)**：一般權限的程式無法對「以系統管理員執行」的視窗送按鍵，這類情況要顯示提示
5. **.NET 版本**：spec 寫 .NET 8，但 .NET 8 在 **2026-11 結束支援**。本機已裝 .NET 10 SDK（LTS）→ **改用 .NET 10**
6. **公開發佈的法律考量**（我不是律師，以下只是風險控管建議）：
   - 不在程式名稱、圖示使用 Cambridge 商標，說明欄標註「非官方工具」
   - 不把查詢結果寫入磁碟快取，只在記憶體保留本次 session 的結果
   - 保留「Open in Cambridge ↗」連結，不刪除頁面上的來源標示
   - 發佈前請自行閱讀 Cambridge 網站的 Terms of Use（自動讀取時被 403 擋下）
   - 預留第二個資料來源介面（例如 Free Dictionary API），Cambridge 失效時還能用

### 意外的好處
Cambridge 網站本身就有搜尋導向功能：`/search/direct/?datasetsearch=english-chinese-traditional&q=…`
- `running` 會自動導到 `run`、片語 `account for` 會導到 phrasal verb 條目、拼錯會給「Did you mean」頁
- → spec 第 4 階段的**詞形還原、拼字建議大部分直接拿網站的功能**，不需要自己做 NLP。只要在英漢版查不到時，再退回英英版 `english`

---

## 二、技術架構

**C# / .NET 10 / WPF + WebView2**（本機已裝 WebView2 runtime 154）

```
LookUp/                      ← 專案名稱避開 "Cambridge"（名字可再定）
├── App.xaml(.cs)            ← 單一實例、Tray、啟動流程
├── Hotkeys/GlobalHotkeyService.cs     RegisterHotKey + 隱藏訊息視窗
├── Selection/
│   ├── UiaSelectionProvider.cs        UIAutomationClient / TextPattern
│   ├── ClipboardSelectionProvider.cs  SendInput + 剪貼簿暫存還原
│   └── SelectionService.cs            UIA → Clipboard，並判斷終端機等例外
├── Lookup/
│   ├── QueryNormalizer.cs             trim、標點、彎引號、NFC、字數判斷
│   ├── IDictionaryProvider.cs         回傳要載入的 URL 和 reader 腳本
│   └── CambridgeWebProvider.cs        組 search/direct URL、英漢→英英 fallback
├── UI/
│   ├── PopupWindow.xaml               無邊框、Topmost、不顯示在工作列，內含 WebView2
│   ├── SearchWindow.xaml              極簡輸入框
│   ├── SettingsWindow.xaml            快捷鍵 / 開機啟動 / 主題
│   └── PopupPositioner.cs             GetCursorPos + 多螢幕 + Per-Monitor DPI v2
├── Web/reader.css, reader.js          隱藏頁首頁尾、廣告區、側欄；排版成極簡樣式；深色模式
└── Storage/SettingsStore.cs           %AppData%\LookUp\settings.json
```

**popup 運作方式**
1. 熱鍵觸發 → 立即在游標旁顯示 popup（WPF 原生的「Searching…」畫面）
2. 預先建立好的 WebView2（程式啟動時就暖機，隱藏待命）導向 search URL
3. `NavigationCompleted` 時注入 `reader.css` / `reader.js`，只留下詞條區
4. 判斷頁面類型：條目頁 / 拼字建議頁 / Cloudflare 驗證頁 / 錯誤頁。驗證頁就直接顯示，讓使用者自己操作
5. 發音直接用頁面上原本的 UK/US 播放鈕，不用另外寫 audio 服務

**資料模型**：因為採用 reader-mode 顯示，**不需要** spec 第 30 節的 `DictionaryEntry` 等結構化模型，先不建，符合 spec 第 40 節「不要過早抽象」。

---

## 三、執行階段（每個階段結束都能單獨跑）

### Phase 0：技術驗證 Spike（約 0.5–1 天，最優先）
目的：先確認最大的風險，不要等做完 UI 才發現路走不通。
- 寫一個最小的 WPF + WebView2 視窗，載入 3 種 URL（單字、片語、拼錯字）
- 驗證：Cloudflare 是否直接放行、第二次載入時 cookie 是否保留、載入時間、注入 CSS 是否有效
- **判定點**：如果 WebView2 也經常被擋 → 回頭討論改用合法 API（Free Dictionary / Merriam-Webster）

### Phase 1：搜尋框 + Popup UX（spec 第 39 節的第一個 milestone）
- 建立 .NET 10 WPF 專案、SearchWindow、PopupWindow
- 先用 `Ctrl+Alt+F` 開搜尋框 → Enter → popup 顯示 Cambridge reader 畫面
- 包含 Loading（1.5 秒以上顯示「Still searching…」）、Esc 關閉、點外面關閉（`Deactivated`）、網路錯誤和查無結果的提示
- 先寫死熱鍵，還不做 Tray

### Phase 2：選字查詢（核心功能）
- `Ctrl+Alt+D` → SelectionService → QueryNormalizer → popup 出現在游標旁
- UIA 優先、剪貼簿 fallback（處理修飾鍵、終端機例外、剪貼簿還原、排除 Win+V 歷史）
- 字數規則：1 字是單字、2–6 字是片語、超過 6 字顯示「Selection is too long」
- PopupPositioner：右邊超出就往左開、下方超出就往上開，支援多螢幕 / DPI

### Phase 3：桌面整合
- Tray（Open Dictionary / Settings / Quit）、單一實例
- Settings：自訂兩組熱鍵（含衝突偵測、註冊失敗提示）、開機啟動（HKCU Run）、主題（System/Light/Dark，同步到 reader.css）

### Phase 4：品質與相容性
- 英漢查不到 → 英英 fallback；「Did you mean」頁的建議可以點
- 依 spec 第 8 節逐一測試各 app，記錄結果（相容性表）
- reader.js 選擇器集中管理，Cambridge 改版時只改一個地方
- 選配：封鎖第三方廣告 / 追蹤請求來加速載入

### Phase 1.5：單字學習筆記（2026-10-05 新增需求）
原 spec 把「單字學習系統」列為不做，但使用者決定加入。做法是讓查字流程不受影響：
- popup 底部只多一個「☆ Add to notebook」（Ctrl+S），一按就存成 Uncategorized；下方展開的小面板可選填分類、熟悉度（New / Learning / Known）、個人筆記，全部都是選填
- 每次查字自動記錄查詢次數與日期（30 分鐘內重複查同一字只算一次）；查第 2 次起 popup 底部顯示「Looked up N times」
- 筆記本視窗（Ctrl+Alt+N）：左側自訂分類（新增 / 改名 / 刪除）、中間單字列表（篩選、熟悉度過濾、多選後右鍵批次分類）、右側詳細資料（摘要、分類、熟悉度、筆記、查詢紀錄、Look up again）
- 匯出 CSV（UTF-8 BOM，Excel 和 Anki 都能用）
- 存的內容：單字、詞性、IPA、第一個繁中翻譯與英文定義、一句例句，加上自己的筆記。只存在本機 `%AppData%\LookUp\notebook.json`
- 「不把查詢結果寫入磁碟快取」的原則仍然適用：只有使用者主動加入筆記的字才會存摘要，一般查詢只記字和次數

### Phase 5：選配（不屬於 MVP）
- ~~History、Favourite~~ → 已由 Phase 1.5 筆記功能取代
- 第二資料來源（Free Dictionary API），Cambridge 不能用時切換
- 公開發佈：`dotnet publish` 打包成 single-file、README 加上非官方聲明、GitHub Release

### 不做（沿用 spec 第 34 節）
離線字典、本機資料庫、批次抓取、AI 翻譯、單字卡、帳號、同步、跨平台、右鍵整合。另外新增：**任何繞過 Cloudflare 或 bot 偵測的手段**。

---

## 四、專案位置與交付
- 建議放在 `D:\Projects\Dictionary\`，用 git 管理
- 核准後，第一件事是把這份計劃書存成 `D:\Projects\Dictionary\docs\PLAN.md`，然後開始 Phase 0

---

## 五、驗收方式

| 階段 | 驗證方法 |
|---|---|
| Phase 0 | 實際啟動 spike，量測 `resilience`、`account for`、`resilense` 的載入時間，截圖確認 reader 樣式 |
| Phase 1 | `dotnet build` 無警告；手動跑 spec 第 35 節 Scenario 1 |
| Phase 2 | Scenario 2（Chrome）、Scenario 3（Word 選 `account for`）；確認剪貼簿原本的內容（含圖片）有被還原 |
| Phase 3 | 重新開機後 Tray 自動出現；改熱鍵後立即生效；深淺色切換 |
| Phase 4 | 相容性表：Chrome / Edge / Firefox / Word / PowerPoint / Notion / VS Code / Obsidian / Acrobat，記錄 UIA 或剪貼簿哪個成功 |
| 全部 | 逐條勾選 spec 第 36 節驗收清單 |

單元測試（xUnit）只涵蓋純邏輯：QueryNormalizer、字數判斷、PopupPositioner 的邊界計算、URL 組合。

---

## 時程估計（一人 + Claude Code）
Phase 0：約 1 天 → Phase 1：2–3 天 → Phase 2：3–4 天 → Phase 3：2 天 → Phase 4：2–3 天。**MVP（Phase 0–3）大約 1.5–2 週的業餘開發時間。**

# 交接文件（2026-10-09）

給下一個 Claude Code 對話用。先讀這份，再讀 `PRODUCT.md`、`DESIGN.md`、`docs/PLAN.md`。最初和 ChatGPT 討論的需求文件在 `docs/original-spec.md`。

## 專案現況

Inset（原暫定名稱 LookUp）：Windows 版「選字 → 快捷鍵 → 跳出 Cambridge 英漢解釋」的查字工具，含單字筆記本。
C# / .NET 10 / WPF（Fluent ThemeMode）+ WebView2。專案在 `D:\Projects\Dictionary`，git 只有 `master` 一個分支，remote 是 GitHub（見下方），使用者試用過再 commit。

- 功能全部完成並已 commit（Phase 0–4、筆記本）
- 介面重新設計已 commit（`f00f934`）：用 impeccable design skill 做的，方向是「設計年鑑版面（design annual plate section）」，`DESIGN.md` 與 `.impeccable/design.json` 已寫好。`.impeccable/review/`、`reference/`、`questions/` 不進版控（見 `.gitignore`）
  - 同一個 commit 也包含：查字視窗可拖曳頂端 token bar 移動、從邊緣調整大小（最小 360×400，存在 settings.json 的 `PopupWidth`/`PopupHeight`，「DEFAULT SIZE」或雙擊 token bar 回到 440×560）；筆記本詳細區的「Look up again」「Cambridge」移到 CATEGORY 上方
  - 注意：`f00f934` 裡的這份 HANDOFF.md 是亂碼（PowerShell 編碼問題），下一個 commit 已修正
- 正式名稱與圖示已 commit（`599605e`，2026-10-06）：
  - 名稱 **Inset**（印刷術語「嵌入圖版」）。搜尋時發現 LookUp、Margin、Gloss 都有同名或極相近的查字工具。只查過同類工具撞名，沒查商標與網域
  - 圖示是對位十字（registration crosshair）放在紙色方塊上，沒有字母。由 `tools/make_icon.py` 產生 `src/LookUp/Assets/AppIcon.ico`（16–256 各尺寸分別畫在整數像素上，16px 的圓環是手點的）。改圖示請改腳本再執行 `python tools/make_icon.py [預覽.png]`
  - exe 改名為 `Inset.exe`（csproj 的 `AssemblyName`），pack URI 改成 `/Inset;component/`。程式碼的 namespace、專案資料夾 `src/LookUp`、`LookUp.slnx`、測試專案維持 LookUp，沒有改
  - 資料夾改成 `%AppData%\Inset`、`%LocalAppData%\Inset`。啟動時 `AppFolders.MoveFromFormerName()` 會把舊的 `LookUp` 資料夾搬過去；搬不動（舊版還在跑）時先沿用舊資料夾，下次啟動再試
  - 開機啟動的登錄值從 `LookUp` 改成 `Inset`，`StartupRegistration.MoveFromFormerName()` 會沿用使用者原本的選擇
- 打包（2026-10-06）：`tools/publish.ps1` 產生 self-contained 單一檔案 `out/publish/win-x64/Inset.exe`（約 73 MB，不需安裝 .NET）和 `out/Inset-<版本>-win-x64.zip`（exe + README + licenses/Geist-OFL.txt）。版本號在 csproj 的 `<Version>`，目前 0.2.0（2026-10-09 加上追問 AI 後升版；0.1.1 是筆記本備份）。exe 沒有數位簽章，README 有寫 SmartScreen 怎麼放行。`README.md` 是給使用者看的說明
- 使用說明（2026-10-06）：`src/LookUp/Web/guide.html`（繁中，跟 App 同一套版面，含查字視窗與筆記本的編號示意圖，支援深色與列印）。嵌入 exe，從系統匣右鍵「How to use」或筆記本左下角「HOW TO USE」打開 `GuideWindow`（App 自己的 WebView2 視窗，頁面和 Geist 字型由 App 從 `https://guide.inset.invalid/` 自己回應，不連網）。原本交給預設瀏覽器開，但使用者的 Brave 視窗全部最小化時什麼都沒出現，所以改成自己的視窗。`publish.ps1` 也把它放進 zip，檔名「使用說明.html」。刻意不放在查字視窗（原則：小、不擋路）。介面文字改了要同步更新說明
- GitHub：private repo https://github.com/leanform07/inset（remote `origin`，`master`）
- GitHub Release（2026-10-07）：[v0.1.0](https://github.com/leanform07/inset/releases/tag/v0.1.0)，tag 指向 `42d558a`，附 `Inset-0.1.0-win-x64.zip`。[v0.1.1](https://github.com/leanform07/inset/releases/tag/v0.1.1)（2026-10-07）加上筆記本備份與遺失提示，tag 指向 `7b68cf7`，附 `Inset-0.1.1-win-x64.zip`。[v0.2.0](https://github.com/leanform07/inset/releases/tag/v0.2.0)（2026-10-09）加上追問 AI，tag 指向 `18154ed`，附 `Inset-0.2.0-win-x64.zip`。repo 是 private，所以只有有權限的人下載得到。之後發新版：改 csproj 的 `<Version>`、跑 `publish.ps1`，再 `gh release create v<版本> out/Inset-<版本>-win-x64.zip --repo leanform07/inset --target master`（`--target` 不接受縮寫的 commit 編號）
- 搜尋框選取修正（`42d558a`，2026-10-07）：搜尋框重開時會全選上次的字，原本被不透明的選取色蓋成一塊灰。csproj 設了 `Switch.System.Windows.Controls.Text.UseAdornerForTextboxSelectionRendering=false`，讓選取色畫在文字下面，`BareTextBox` 的 `SelectionTextBrush` 用 InkBrush。測試專案的 csproj 也要有同一個開關，螢幕外截圖才會一樣
- 舊的 `%LocalAppData%\LookUp`（舊 WebView2 profile）已丟進資源回收筒
- 測試：`dotnet test tests/LookUp.Tests/LookUp.Tests.csproj`，110 個全部通過

## 追問 AI（2026-10-09，使用者試用沒問題，commit `1ad6e2d`，打包成 0.2.0）

- 使用者想問「這個字跟另一個字差在哪」「更詳細的用法」。選了三種做法裡最輕的：用瀏覽器打開使用者自己的 ChatGPT／Claude，問題用網址帶進去。不申請 API、不存金鑰、Inset 本身不連 AI。沒選內嵌 ChatGPT 視窗（Google 常擋內嵌 App 的登入）和 API（另外按量收費、視窗變重）
- `Lookup/AskAi.cs`：`Classify` 判斷輸入框的意思（空白 → 問用法；3 個字以內的英文且開頭不是 how/what/is… → 比較兩個字；其他 → 使用者自己的問題），`Prompt` 組繁中問題（附詞性、中文、英文解釋，讓 AI 講同一個意思），`ChatUrl` 產生 `https://chatgpt.com/?q=` 或 `https://claude.ai/new?q=`。**兩家都沒有正式文件**，可能哪天失效，所以面板有 `COPY QUESTION`
- `UI/AskAiPanel`（UserControl）：查字視窗（動作列右邊 `ASK AI`，`Ctrl+Q`，展開在筆記面板的位置，兩個面板互斥）和筆記本詳細區（Look up again / Cambridge 那一排右邊）共用。提示文字會即時說它要怎麼問，避免猜錯
- 查字視窗的 `Esc`：面板開著時先關面板；正在用注音選字（`Key.ImeProcessed`）時不處理，免得選字的 Esc 把面板關掉
- 設定：Settings 新增「Ask AI」：ChatGPT（預設）／Claude，存在 settings.json 的 `AiAssistant`，`App.SetAssistant` 同步到查字視窗與筆記本
- `PRODUCT.md` 原本把「AI translation」列為不做；改寫成「只把追問交給使用者自己的 AI，Cambridge 仍是主要內容」，不做的清單改成「AI 翻譯或 AI 寫的詞條」
- 使用說明（guide.html 的「追問 AI」小節、示意圖加上 5 號 ASK AI）、README、DESIGN.md 都已更新
- 快捷按鈕（同一天，使用者追加；使用者從建議中選了搭配詞、記憶法，沒選正式程度、常見錯誤）：面板上方 `Compare…`／`Usage`／`Collocations`／`Memory tip`。**按快捷按鈕只是選好問題（按鈕變實心，再按一次取消），一律要按 Ask 或 Enter 才送出**（使用者試過「按了就直接問」後要求改的）。Compare… 時輸入框打要比較的字（逗號、頓號分開可以打好幾個，沒打字時 Ask 不能按）；Usage／Collocations／Memory tip 時輸入框是選填的補充，會加成「補充：…」一行。沒選快捷按鈕時，輸入框照舊猜：空白 → Usage、英文字清單 → Compare、其他 → 使用者自己的問題。`AskAi.Prompt` 改成傳 `Kind`
- 結論存回筆記本（同一天，使用者追加的需求）：問題最後請 AI 把簡短的結論（不限行數，使用者要讓 AI 自己判斷長度）放進程式碼區塊，第一行 `[Inset] 單字`。AI 在瀏覽器裡，Inset 讀不到回答，所以靠剪貼簿：使用者按程式碼區塊的「複製」（或複製整則回答），`Selection/ClipboardWatcher`（`AddClipboardFormatListener`，message-only 視窗）把文字交給 `Notebook/AiConclusions`，比對 2 小時內問過的字（大小寫不拘；只問了一個字時，AI 把長詞條縮寫也認），加到筆記下一行（同一段不重複加），字還沒存就用問的那個詞條存進去，系統匣跳通知。**只在有待回的問題時才監聽剪貼簿**，過期就停。問題本身沒有任何一行以 `[Inset]` 開頭，所以 COPY QUESTION 不會被當成結論。查字視窗的 NOTE 欄改成自動換行（最高 84px）
- 等使用者確認：`Ctrl+Q` 在 WebView 有焦點時能不能觸發（跟 `Ctrl+S` 同一條路，應該可以）、ChatGPT 打開後是否真的自動送出、注音輸入問題時按 Enter 會不會誤送、ChatGPT 是否照格式把結論放進 `[Inset] 單字` 開頭的程式碼區塊、複製後筆記有沒有自動填入

## 筆記本遺失事件與防護（2026-10-07）

- 使用者 10/6 下午重開機後，`%AppData%\Inset\notebook.json` 變成一本新的空筆記本（`Lookups` 最早是 10/6 14:42），之前存的單字和查字紀錄全部不見。`%AppData%\LookUp` 不存在、沒有 `notebook.unreadable-*`、資源回收筒只有 10/6 02:23 丟掉的 `%LocalAppData%\LookUp`（92 MB 的 WebView2 profile，不含筆記本）、沒有「以前的版本」、沒匯出過 CSV。**單字沒救回來**
- 原因沒查到。程式碼裡沒有任何會刪掉筆記本的路徑（`Remove` 會保留查字次數，所以連 `Lookups` 一起消失代表整個檔案或資料夾不見了）。前一次對話的線上紀錄只到 10/5 23:34，看不到改名當晚 02:2x 對資料夾做了什麼。注意：改名後單一執行個體的 Mutex 名稱也從 `LookUp` 改成 `Inset`，舊版 LookUp.exe 和 Inset.exe 可以同時執行、各自存檔
- 加上的防護（雲端對話寫的；2026-10-07 併進 master 後在 Windows 上 `dotnet test` 83 個全過，打包成 0.1.1）：
  - `NotebookBackups`：每天第一次存檔時把筆記本複製到 `%LocalAppData%\Inset\Backups\notebook-yyyy-MM-dd.json`，保留 14 份；空的筆記本不備份，免得把好的備份擠掉。故意跟筆記本放在不同資料夾
  - `NotebookWordCount`：每次存檔把字數寫進 `HKCU\Software\Inset\NotebookWords`（不在資料夾裡，資料夾整個不見也還在）
  - `NotebookRecovery` + `App.OpenNotebook`：啟動時如果登錄記得有字、筆記本卻是空的（檔案不見或讀不出來），跳 MessageBox 說明，有備份就問要不要還原；原本的檔案改名成 `notebook.replaced-*.json` 留著。只問一次
  - `NotebookStore.Save` 先把暫存檔 flush 到磁碟再改名，避免斷電後檔案變空；讀不出來的檔案現在會透過 `SetAsidePath` 告訴使用者，不再默默開新的
  - README、使用說明加上備份位置與登錄機碼

## 等使用者確認的事

- 改名後的試用：系統匣圖示、資料有沒有順利搬到 `Inset` 資料夾（筆記本內容、設定、Cloudflare 不用重新驗證）
- 追問 AI 的試用（見上一節最後一點）
- 調整大小的實際手感還沒回報：四邊是否都拉得動、拖到螢幕邊緣會不會被 Windows 貼齊（snap）、左右 5px 內縮有沒有接縫

## 之後可能的工作

- `docs/TESTING.md` 的各 App 相容性表（Chrome、Word、Acrobat、Notion…）等使用者手動測試後填寫
- 專案本身的授權還沒決定（目前沒有 LICENSE 檔，等於保留所有權利）；公開發佈前要決定
- 發佈管道：GitHub Releases 已上傳 v0.1.0、v0.1.1；還沒決定 repo 要不要公開（公開前先決定授權）、要不要 win-arm64 版（`publish.ps1 -Runtime win-arm64`）、要不要買程式碼簽章

## 這台電腦上的注意事項（踩過的坑）

- **Bash 工具會吃掉反斜線**（heredoc 和 sed 都會：`\n`、`\s`、Windows 路徑）。多行的 Python/C# 修改腳本，先用 Write 工具寫成檔案再執行；含反斜線的取代用 Edit 工具
- **PowerShell 5.1 的編碼**：`Get-Content` 會把沒有 BOM 的 UTF-8 檔當成 Big5 讀，`Set-Content -Encoding utf8` 會加 BOM，兩者合起來會把中文檔案變亂碼。不要用 PowerShell 改文字檔，用 Write／Edit 工具。`git commit -F -` 搭配 here-string 也沒用，commit 訊息請寫進暫存檔再 `-F <檔案>`
- **使用者常常在跑 Inset.exe**（舊版叫 LookUp.exe；2026-10-06 起跑的是 `out/publish/win-x64/` 的發佈版），會鎖住 `bin/` 或 `out/` 導致建置或打包失敗。可以 `dotnet test -o <暫存資料夾>` 檢查編譯；要關掉使用者的程式前先問
- **不要做會搶焦點的 GUI 自動測試**（使用者同時在用電腦）。視覺檢查改用：
  - WPF 視窗：`WindowSmokeTests` 在螢幕外渲染，設環境變數 `LOOKUP_SNAPSHOT_DIR` 就會輸出 PNG（淺色、深色都有）
  - 詞條頁（WebView）：`tools/CssProbe`，在螢幕外載入 Cambridge 並套用真正的 reader.js/css 後截圖。用法：`CssProbe.exe <輸出資料夾> <查詢字...>`；環境變數 `PROBE_DARK=1` 深色、`PROBE_SEAL=1` 蓋印章。它有自己的 WebView2 profile，第一次會先通過 Cloudflare 驗證
- **測試裡絕對不要 `new App()`**：WPF 會執行 `App.OnStartup`，啟動一個真的 Inset
- **Cambridge 有 Cloudflare 驗證**：HttpClient 直接抓會 403；WebView2 必須「可見」時驗證才會自己通過。不可自動化或繞過
- impeccable 的決策頁伺服器（`serve-question`）用 PowerShell 或 `--start` 啟動會被清掉；要用 Bash `run_in_background` 不加 `--start` 的方式執行

## 設計系統重點（細節看 DESIGN.md）

- 色票在 `src/LookUp/Themes/Light.xaml`、`Dark.xaml`，由 `tools/make_palettes.py` 一次產生兩份（同一組 key）。改顏色請改這個腳本再執行 `python tools/make_palettes.py`，不要只手改其中一個 XAML
- 共用樣式在 `Themes/Shared.xaml`；ContextMenu 樣式刻意放在色票檔裡（理由見 DESIGN.md）
- 詞條頁樣式在 `src/LookUp/Web/reader.css`（CSS 變數對應色票）、`reader.js`（十字標記、印章）
- 唯一強調色是長春花藍印章（只用在印章與片語相符區塊）；選取狀態是中性的

## 給下一個對話的開場白（可直接貼上）

> 繼續 D:\Projects\Dictionary 的 Inset 專案。先讀 docs/HANDOFF.md，然後告訴我目前有哪些待決定的事。

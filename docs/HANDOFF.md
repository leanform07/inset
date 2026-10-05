# 交接文件（2026-10-06）

給下一個 Claude Code 對話用。先讀這份，再讀 `PRODUCT.md`、`DESIGN.md`、`docs/PLAN.md`。最初和 ChatGPT 討論的需求文件在 `docs/original-spec.md`。

## 專案現況

Inset（原暫定名稱 LookUp）：Windows 版「選字 → 快捷鍵 → 跳出 Cambridge 英漢解釋」的查字工具，含單字筆記本。
C# / .NET 10 / WPF（Fluent ThemeMode）+ WebView2。專案在 `D:\Projects\Dictionary`，git 只有 `master` 一個分支、沒有 remote，使用者試用過再 commit。

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
- 打包（2026-10-06）：`tools/publish.ps1` 產生 self-contained 單一檔案 `out/publish/win-x64/Inset.exe`（約 73 MB，不需安裝 .NET）和 `out/Inset-<版本>-win-x64.zip`（exe + README + licenses/Geist-OFL.txt）。版本號在 csproj 的 `<Version>`，目前 0.1.0。exe 沒有數位簽章，README 有寫 SmartScreen 怎麼放行。`README.md` 是給使用者看的說明
- GitHub：private repo https://github.com/leanform07/inset（remote `origin`，`master`）
- 舊的 `%LocalAppData%\LookUp`（舊 WebView2 profile）已丟進資源回收筒
- 測試：`dotnet test tests/LookUp.Tests/LookUp.Tests.csproj`，72 個全部通過

## 等使用者確認的事

- 改名後的試用：系統匣圖示、資料有沒有順利搬到 `Inset` 資料夾（筆記本內容、設定、Cloudflare 不用重新驗證）
- 調整大小的實際手感還沒回報：四邊是否都拉得動、拖到螢幕邊緣會不會被 Windows 貼齊（snap）、左右 5px 內縮有沒有接縫

## 之後可能的工作

- `docs/TESTING.md` 的各 App 相容性表（Chrome、Word、Acrobat、Notion…）等使用者手動測試後填寫
- 專案本身的授權還沒決定（目前沒有 LICENSE 檔，等於保留所有權利）；公開發佈前要決定
- 發佈管道：GitHub Releases 上傳 zip、要不要 win-arm64 版（`publish.ps1 -Runtime win-arm64`）、要不要買程式碼簽章

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

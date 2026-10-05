# 交接文件（2026-10-05）

給下一個 Claude Code 對話用。先讀這份，再讀 `PRODUCT.md`、`DESIGN.md`、`docs/PLAN.md`。

## 專案現況

LookUp（暫定名稱）：Windows 版「選字 → 快捷鍵 → 跳出 Cambridge 英漢解釋」的查字工具，含單字筆記本。
C# / .NET 10 / WPF（Fluent ThemeMode）+ WebView2。專案在 `D:\Projects\Dictionary`。

- 功能全部完成並已 commit（Phase 0–4、筆記本）：最後一個功能 commit 是 `023c7e5`，設計前置是 `c80ea59`
- **介面重新設計已完成但「尚未 commit」**：用 impeccable design skill 做的，方向是「設計年鑑版面（design annual plate section）」，finish reviewer 判定 ship，`DESIGN.md` 與 `.impeccable/design.json` 已寫好
- 測試：`dotnet test tests/LookUp.Tests/LookUp.Tests.csproj`，67 個全部通過

## 等使用者決定的事

1. **commit 範圍**：建議把 `.impeccable/review/`（截圖）和 `.impeccable/reference/`（從 design skill 網站下載的參考圖）加進 `.gitignore`，其餘（DESIGN.md、`.impeccable/surfaces/`、`.impeccable/design.json`、`.impeccable/decision/`、所有程式碼）一起 commit
2. **筆記本按鈕位置**：預設大小 1120×700 時，右側詳細區的「Look up again」「Cambridge」按鈕在最下緣，要捲動才看得到。選項：把預設視窗加高，或把這兩個按鈕移到 CATEGORY 欄位上方，或維持現狀
3. 使用者還沒實際試用新介面（需要先關掉正在跑的舊版 LookUp 再重新建置）

## 之後可能的工作

- `docs/TESTING.md` 的各 App 相容性表（Chrome、Word、Acrobat、Notion…）等使用者手動測試後填寫
- 打包發佈：`dotnet publish` single-file、README（含非官方工具聲明、Geist 字型 OFL 授權）
- 正式名稱與圖示（目前 `Assets/LookUp.ico` 是暫定的藍底 Aa，圖示不可放名稱；新圖示應跟 DESIGN.md 的世界一致）

## 這台電腦上的注意事項（踩過的坑）

- **Bash 工具的 heredoc 會吃掉反斜線**（`\n`、`\s`、Windows 路徑）。多行的 Python/C# 修改腳本，先用 Write 工具寫成檔案再執行
- **使用者常常在跑 LookUp.exe**，會鎖住 `bin/` 導致建置失敗。可以 `dotnet build -o <暫存資料夾>` 檢查編譯；要關掉使用者的 LookUp 前先問
- **不要做會搶焦點的 GUI 自動測試**（使用者同時在用電腦）。視覺檢查改用：
  - WPF 視窗：`WindowSmokeTests` 在螢幕外渲染，設環境變數 `LOOKUP_SNAPSHOT_DIR` 就會輸出 PNG（淺色、深色都有）
  - 詞條頁（WebView）：暫存區的 `cssprobe` 小程式，在螢幕外載入 Cambridge 並套用真正的 reader.js/css（需要時可以重寫一個，做法在 `.impeccable` 的流程紀錄與本對話中）
- **測試裡絕對不要 `new App()`**：WPF 會執行 `App.OnStartup`，啟動一個真的 LookUp
- **Cambridge 有 Cloudflare 驗證**：HttpClient 直接抓會 403；WebView2 必須「可見」時驗證才會自己通過。不可自動化或繞過
- impeccable 的決策頁伺服器（`serve-question`）用 PowerShell 或 `--start` 啟動會被清掉；要用 Bash `run_in_background` 不加 `--start` 的方式執行

## 設計系統重點（細節看 DESIGN.md）

- 色票在 `src/LookUp/Themes/Light.xaml`、`Dark.xaml`，由暫存區的 `make_palettes.py` 一次產生兩份（同一組 key）。**若重新產生，請先把該腳本重建或直接手動同步兩個檔案**——腳本在暫存區，下個對話不一定還在
- 共用樣式在 `Themes/Shared.xaml`；ContextMenu 樣式刻意放在色票檔裡（理由見 DESIGN.md）
- 詞條頁樣式在 `src/LookUp/Web/reader.css`（CSS 變數對應色票）、`reader.js`（十字標記、印章）
- 唯一強調色是長春花藍印章（只用在印章與片語相符區塊）；選取狀態是中性的

## 給下一個對話的開場白（可直接貼上）

> 繼續 D:\Projects\Dictionary 的 LookUp 專案。先讀 docs/HANDOFF.md，然後告訴我目前有哪些待決定的事。

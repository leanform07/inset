# 手動測試清單

自動測試（`dotnet test`）涵蓋：查詢正規化、視窗定位計算、快捷鍵解析、筆記本存取與 CSV、Cambridge 網址判斷，以及所有視窗能正常開啟（淺色 / 深色）。

以下項目需要真人在真實 App 裡操作。

## 1. 選字查詢相容性（Ctrl+Alt+D）

做法：在各 App 選一個單字（例如 `resilience`）和一個片語（例如 `account for`），按 Ctrl+Alt+D。
「方法」欄填實際成功的方式：
- **UIA**：剪貼簿沒有變動
- **剪貼簿**：Win+V 歷史會多一筆剛選的字（這是那個 App 自己的 Ctrl+C 造成的，LookUp 還原的那一筆不會出現）

| App | 單字 | 片語 | 方法 | 備註 |
|---|---|---|---|---|
| 記事本 | ✅ | ✅ | UIA / 剪貼簿都測過 | 2026-10-05 自動測試 |
| Chrome |  |  |  |  |
| Edge |  |  |  |  |
| Brave |  |  |  |  |
| Firefox |  |  |  |  |
| Word |  |  |  |  |
| PowerPoint |  |  |  |  |
| Notion |  |  |  |  |
| Obsidian |  |  |  |  |
| VS Code（編輯器） |  |  |  | 沒選字時 VS Code 的 Ctrl+C 會複製整行 → 應顯示「too long」 |
| Adobe Acrobat |  |  |  | 跨行選取會有斷字連字號，應會自動接回 |
| Windows 終端機 | — | — | — | 應顯示「Can't read the selection in a terminal」，且不送出 Ctrl+C |

## 2. 查字視窗

- [ ] 視窗出現在游標下方；游標靠螢幕右邊 / 下面時會改往左 / 往上開
- [ ] 多螢幕（不同縮放比例）時出現在游標所在的那個螢幕，大小正確
- [ ] Esc、點視窗外面都會關閉
- [ ] 方向鍵 / PageDown 可以捲動內容
- [ ] UK / US 發音按鈕有聲音
- [ ] 點詞條裡的英文單字，會在原視窗查那個字
- [ ] `be subject to`：跳到 subject 詞條、黃色標示 "be subject to something"
- [ ] `deplatform`：底部顯示「English only · no Chinese translation」
- [ ] 拼錯（`resilense`）：顯示拼字建議，點建議可以查
- [ ] 斷網時查詢：顯示連線錯誤和「Try again」

## 3. 筆記本

- [ ] Ctrl+S 加入筆記；面板打新分類名稱會自動建立分類
- [ ] 同一字隔 30 分鐘以上再查，底部顯示「Looked up 2 times」
- [ ] 筆記本視窗（Ctrl+Alt+N）：多選 → 右鍵 → Move to category
- [ ] 分類改名 / 刪除（刪除後單字變成 Uncategorized，不會消失）
- [ ] Export CSV 用 Excel 打開，中文正常

## 4. 桌面整合

- [ ] 系統匣圖示：左鍵開搜尋框；右鍵選單有 Look up / Notebook / Settings / Quit
- [ ] 已在執行時再開一次 LookUp.exe → 叫出原本那個的搜尋框，不會開第二個
- [ ] Settings：改快捷鍵後立即生效；設成別的 App 已占用的組合會顯示錯誤；Reset to defaults
- [ ] Settings：深色 / 淺色 / 跟隨 Windows，查字視窗內容也會跟著變
- [ ] 勾選「開機啟動」→ 重新登入後 LookUp 在系統匣，不會跳出搜尋框

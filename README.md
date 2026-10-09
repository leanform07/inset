# Inset

在任何 App 裡選取英文，按一下快捷鍵，游標旁就跳出 Cambridge 英漢（繁體）解釋。Windows 版的「查詢（Look Up）」。

- 發音、詞性、英文定義、中文翻譯、例句，看完按 Esc 繼續讀，不用切換視窗、不用複製貼上、不用開瀏覽器
- 想記下來的字按 Ctrl+S 存進單字筆記本，可以分類、標熟悉度、寫筆記，也能匯出 CSV（Excel、Anki）
- 每個字查了幾次都會記下來，常查的字一眼就看得出來
- 字典沒寫到的，按 ASK AI 用瀏覽器問 ChatGPT 或 Claude：快捷按鈕可以比較好幾個字的差別、問詳細用法、搭配詞、記憶法，也可以自己打問題，問題會自動帶上查的字和它的解釋；AI 最後給的簡短結論複製一下，就會自動存進這個字的筆記

> 非官方工具。Inset 與 Cambridge University Press & Assessment 沒有任何關係。字典內容是即時從 [Cambridge Dictionary](https://dictionary.cambridge.org/) 網站讀取並顯示，版權屬於 Cambridge；每個詞條都保留「CAMBRIDGE」連結可以開原網頁。

## 系統需求

- Windows 11（x64）
- Microsoft Edge WebView2 Runtime（Windows 11 已內建）
- 網路連線（字典內容來自 Cambridge 網站，不支援離線）

## 安裝

1. 下載 `Inset-<版本>-win-x64.zip`，解壓縮到一個固定的位置，例如「文件\Inset」
2. 執行 `Inset.exe`。它會常駐在右下角的系統匣，不會開大視窗
3. 程式沒有數位簽章，第一次執行時 Windows 可能顯示「Windows 已保護您的電腦」：按「其他資訊」→「仍要執行」
4. 第一次查字時，Cambridge 網站會跑一次 Cloudflare 安全驗證，等它自己通過就好，之後不會再出現

想開機自動啟動：系統匣圖示按右鍵 → Settings → 勾選「Start Inset when I sign in to Windows」。

完整的圖文說明在 zip 裡的「使用說明.html」；裝好之後也可以從系統匣右鍵 → How to use，或筆記本左下角的 HOW TO USE 打開。

## 怎麼用

| 快捷鍵 | 作用 |
|---|---|
| `Ctrl+Alt+D` | 查目前選取的字或片語 |
| `Ctrl+Alt+F` | 開搜尋框，自己打字查（選不到字的地方用這個） |
| `Ctrl+Alt+N` | 開單字筆記本 |
| `Ctrl+S` | 在查字視窗裡，把這個字存進筆記本 |
| `Ctrl+Q` | 在查字視窗裡，追問 AI（ASK AI） |
| `Esc` | 關閉查字視窗 |

三個全域快捷鍵都可以在 Settings 裡改。查字視窗可以拖曳上緣移動、拉邊緣調整大小，右上角的「DEFAULT SIZE」可以恢復預設大小。

**選不到字的情況：** 終端機，以及用系統管理員身分執行的程式，讀不到選取的文字，請改用 `Ctrl+Alt+F` 自己輸入。

## 資料存在哪裡

全部只存在這台電腦，沒有帳號、不上傳、不同步。唯一的例外是你按 ASK AI 送出的問題：它會在瀏覽器裡送到你選的 ChatGPT 或 Claude，用的是你自己的帳號，Inset 本身不連線到任何 AI 服務。送出後 2 小時內，Inset 會留意剪貼簿裡開頭是 `[Inset]` 的結論並存進筆記本，其他複製的內容不看也不存。

- `%AppData%\Inset\notebook.json`：單字筆記本
- `%AppData%\Inset\settings.json`：設定
- `%LocalAppData%\Inset\Backups`：筆記本的每日備份，保留最近 14 天。刻意跟筆記本分開放，清快取時不要刪掉這個資料夾
- `%LocalAppData%\Inset\WebView2`：內嵌瀏覽器的資料（Cloudflare 驗證、快取）
- 登錄機碼 `HKCU\Software\Inset`：記錄筆記本上次存檔時有幾個字。啟動時如果筆記本突然變空，Inset 會靠它發現，並提議從備份還原

**解除安裝：** 系統匣右鍵 → Quit Inset；如果有勾開機啟動，先在 Settings 取消；再刪掉 `Inset.exe`、`%AppData%\Inset`、`%LocalAppData%\Inset` 兩個資料夾，以及登錄機碼 `HKCU\Software\Inset`。

## 授權

- Inset 本身：[MIT License](LICENSE)，可以自由使用、修改、轉發，請保留版權聲明。字典內容不在此授權內，版權屬於 Cambridge University Press & Assessment
- 介面字型 [Geist、Geist Mono](https://vercel.com/font)：SIL Open Font License 1.1，授權全文在 `licenses/Geist-OFL.txt`

## 開發

C# / .NET 10 / WPF + WebView2。

```
dotnet build src/LookUp/LookUp.csproj
dotnet test tests/LookUp.Tests/LookUp.Tests.csproj
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

`publish.ps1` 產生單一檔案、內含 .NET 執行環境的 `Inset.exe`，以及發佈用的 zip，都放在 `out/`。

產品與設計文件：`PRODUCT.md`、`DESIGN.md`、`docs/`。程式碼的 namespace 與專案資料夾沿用暫定名稱 LookUp。

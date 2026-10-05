# Phase 0 技術驗證結果（2026-10-05）

Spike 程式：`spikes/WebViewSpike`（.NET 10 WPF + WebView2 runtime 154）
查詢 URL：`https://dictionary.cambridge.org/search/direct/?datasetsearch=english-chinese-traditional&q=<query>`

## 結論：✅ 通過，WebView2 路線可行

| 驗證項目 | 結果 |
|---|---|
| HttpClient / curl 直接抓 | ❌ 403 Cloudflare challenge（已確認不可行） |
| WebView2 第一次使用（全新 profile） | Cloudflare「正在執行安全驗證」頁面**自己通過**，不需要點擊；第一次大約等 15 秒，之後的查詢約 2.6 秒 |
| WebView2 之後的查詢（profile 已有 cookie） | **不再出現驗證**，連續兩次執行 12 次查詢全部成功 |
| 載入時間（到 DOM 可用） | **0.8–1.3 秒**（WebView2 初始化約 0.24 秒，程式啟動時暖機一次即可） |
| `resilience` | 跳到條目頁：IPA ×4、定義 ×2、繁中 ×4、例句 ×4、UK/US 音檔 ×2 |
| `account for` | 跳到 phrasal verb `account (to someone) for something`，有完整繁中翻譯和例句 |
| `resilense`（拼錯） | 跳到 `/spellcheck/` 建議頁 → 可以偵測出來（`didYouMean`） |
| Reader 腳本（把詞條區移成唯一內容） | 條目頁效果很好：標題、詞性、IPA、發音鈕、CEFR、定義、繁中、例句都正常顯示 |

## 對正式實作的影響
1. **第一次使用**：popup 要能顯示 Cloudflare 驗證頁（自動通過，偶爾可能要使用者自己點），並提示「第一次使用需要驗證」
2. **WebView2 profile 要固定存在** `%LocalAppData%\<App>\WebView2`，cookie 才能保留
3. **偵測驗證頁**不能靠英文標題（頁面會依系統語言顯示「請稍候...」），改用 `window._cf_chl_opt` 或 `Ray ID`
4. **Spellcheck 頁**要另外寫 reader 樣式（Phase 4），目前顯示的是完整網頁
5. 「0.8–1.3 秒」是目前沒有任何優化的數字；Phase 4 可以試著在 `DOMContentLoaded` 就注入、封鎖第三方廣告請求，應該還能更快
6. 頁面上的 CSS class（`.pr.dictionary`、`.def.ddef_d`、`.trans.dtrans`、`.ipa`、`source[type="audio/mpeg"]`）目前都有效，集中放在 reader.js 統一管理

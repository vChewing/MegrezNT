# Megrez Engine 天權星引擎

- Gitee: [Swift](https://gitee.com/vChewing/Megrez) | [C#](https://gitee.com/vChewing/MegrezNT)
- GitHub: [Swift](https://github.com/vChewing/Megrez) | [C#](https://github.com/vChewing/MegrezNT)

天權星引擎是用來處理輸入法語彙庫的一個模組。該倉庫乃威注音專案的弒神行動（Operation Longinus）的一部分。

Megrez Engine is a module made for processing lingual data of an input method. This repository is part of Operation Longinus of The vChewing Project.

相關使用說明請參見 Swift 版的倉庫的 README.MD。函式用法完全一致。

## 與 Gramambular 2 的區別

敝專案一開始是 Gramambular 2 (Lukhnos Liu 著，MIT License) 的 C# 實作，但經歷了大量修改。主要區別如下：

- 原生 C# 實作，擁有完備的 .NET 6 支援、也可以用作任何新版 .NET 專案的相依套件（需使用者自行處理對跨執行緒安全性的需求）。
- API 經過重新設計，以陣列的形式處理輸入的 key。而且，在獲取候選字詞內容的時候，也可以徹底篩除橫跨游標的詞。
- 爬軌算法（Walking Algorithm）改為 Dijkstra 的算法，且經過效能最佳化處理、擁有比 DAG-Relax 算法更優的效能。

## 著作權 (Credits)

- CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
	- Original Swift programmer: Shiki Suen
- Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
- Walking algorithm (Dijkstra) implemented by (c) 2025 and onwards The vChewing Project (MIT License).
    - Original Swift programmer: Shiki Suen
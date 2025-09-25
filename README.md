# Megrez Engine 天權星引擎

- Gitee: [Swift](https://gitee.com/vChewing/Megrez) | [C#](https://gitee.com/vChewing/MegrezNT)
- GitHub: [Swift](https://github.com/vChewing/Megrez) | [C#](https://github.com/vChewing/MegrezNT)

天權星引擎是用來處理輸入法語彙庫的一個模組。

Megrez Engine is a module made for processing lingual data of an input method.

## 專案特色

- 原生 C# 實作，擁有完備的 .NET 6 支援、也可以用作任何新版 .NET 專案的相依套件（需使用者自行處理對跨執行緒安全性的需求）。
- 以陣列的形式處理輸入的 key。
- 在獲取候選字詞內容的時候，不會出現橫跨游標的詞。
- 使用 DAG-DP 算法，擁有比 DAG-Relax Topology 算法更優的效能。

## 使用說明

`MegrezTests.cs` 展示了詳細的使用方法。

## 著作權 (Credits)

- (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
- The unit tests utilizes certain contents extracted from libvchewing-data by (c) 2022 and onwards The vChewing Project (BSD-3-Clause).

敝專案採雙授權發佈措施。除了 LGPLv3 以外，對商業使用者也提供不同的授權條款（比如允許閉源使用等）。詳情請[電郵聯絡作者](shikisuen@yeah.net)。

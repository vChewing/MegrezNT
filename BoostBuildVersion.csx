#!/usr/bin/env dotnet-script

#nullable enable
// BoostBuildVersion.csx
// 用途:
//  1. 自動將主專案 Megrez/Megrez.csproj 的 <Version> 之 semver patch 自動 +1。
//  2. 或若第一個非旗標參數是合法 semver (X.Y.Z) 則直接使用該版本號。
//  3. 同步更新以下檔案中的版本字串：
//       - Megrez/Megrez.csproj: ReleaseVersion / AssemblyVersion / FileVersion / Version
//       - Megrez.Tests/Megrez.Tests.csproj: ReleaseVersion
//       - Megrez.sln: MonoDevelopProperties 區段中的 version = X.Y.Z
//  4. 保留原始 UTF-8 BOM（若存在）與 CRLF 換行格式；不修改 .sln 檔結構。
// 使用方式:
//   dotnet script BoostBuildVersion.csx             (自動 patch +1)
//   dotnet script BoostBuildVersion.csx --dry-run   (僅顯示不寫回)
//   dotnet script BoostBuildVersion.csx 5.1.0       (指定版本)
//   dotnet script BoostBuildVersion.csx 5.1.0 --dry-run
// 備註: 若自動計算版本與舊版本相同則視為錯誤；成功後會嘗試 git add 三個被更動的檔案。

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

bool dryRun = Environment.GetCommandLineArgs().Contains("--dry-run");
var rawArgs = Environment.GetCommandLineArgs()
              .Skip(1)
              .Where(a => a != "--dry-run")
              .ToList();

string root = Directory.GetCurrentDirectory();
string mainProj = Path.Combine(root, "Megrez", "Megrez.csproj");
if (!File.Exists(mainProj)) {
    Console.Error.WriteLine($"找不到主專案檔: {mainProj}");
    Environment.Exit(1);
}

string ReadAllPreserve(string path, out bool hadBom, out bool usedCRLF) {
    byte[] raw = File.ReadAllBytes(path);
    hadBom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
    string text = hadBom ? Encoding.UTF8.GetString(raw, 3, raw.Length - 3) : Encoding.UTF8.GetString(raw);
    usedCRLF = text.Contains("\r\n");
    return text;
}

void WriteAllPreserve(string path, string text, bool hadBom, bool usedCRLF) {
    if (usedCRLF) {
        text = Regex.Replace(text, "\r?\n", "\r\n");
    }
    byte[] body = Encoding.UTF8.GetBytes(text);
    if (hadBom) {
        var withBom = new byte[body.Length + 3];
        withBom[0] = 0xEF; withBom[1] = 0xBB; withBom[2] = 0xBF;
        Buffer.BlockCopy(body, 0, withBom, 3, body.Length);
        body = withBom;
    }
    File.WriteAllBytes(path, body);
}

string projText = ReadAllPreserve(mainProj, out _, out _);
var verMatch = Regex.Match(projText, "<Version>([0-9]+\\.[0-9]+(?:\\.[0-9]+)?)</Version>");
if (!verMatch.Success) {
    Console.Error.WriteLine("無法在 Megrez.csproj 找到 <Version> 標籤。");
    Environment.Exit(1);
}

string oldVersion = verMatch.Groups[1].Value;
string autoNew;
var parts = oldVersion.Split('.');
if (parts.Length >= 3 && int.TryParse(parts[2], out int patch)) {
    autoNew = $"{parts[0]}.{parts[1]}.{patch + 1}";
} else if (parts.Length == 2) {
    autoNew = $"{parts[0]}.{parts[1]}.1";
} else {
    Console.Error.WriteLine($"不支援的版本格式: {oldVersion}");
    Environment.Exit(1);
    return; // 避免編譯器警告
}

string? manual = null;
if (rawArgs.Count > 0) {
    var cand = rawArgs[0];
    if (Regex.IsMatch(cand, "^[0-9]+\\.[0-9]+\\.[0-9]+$")) {
        manual = cand;
    }
}
string newVersion = manual ?? autoNew;
bool manualOverride = manual != null;

if (!manualOverride && newVersion == oldVersion) {
    Console.Error.WriteLine("自動推算的新版本與舊版本相同，未進行變更。");
    Environment.Exit(1);
}

Console.WriteLine($"Old version: {oldVersion} -> New version: {newVersion}{(manualOverride ? " (manual override)" : "")}{(dryRun ? " (dry-run)" : "")}");

string[] files = {
    Path.Combine(root, "Megrez", "Megrez.csproj"),
    Path.Combine(root, "Megrez.Tests", "Megrez.Tests.csproj"),
    Path.Combine(root, "Megrez.sln")
};

// 修復正則表達式問題
var xmlTagPattern = new Regex("<(ReleaseVersion|AssemblyVersion|FileVersion|Version)>" + Regex.Escape(oldVersion) + "</\\1>");
// 修復 .sln 文件的正則表達式，更精確匹配版本行
var slnPattern = new Regex(@"(^|\s)version\s*=\s*" + Regex.Escape(oldVersion) + @"(?=\s*$)", RegexOptions.Multiline);

int changedCount = 0;
foreach (var file in files) {
    if (!File.Exists(file)) {
        Console.WriteLine($"跳過 (不存在): {file}");
        continue;
    }
    string text = ReadAllPreserve(file, out bool hadBom, out bool usedCRLF);
    string original = text;
    bool isSln = file.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);

    // 對於 XML 文件，使用修復後的正則
    if (!isSln) {
        text = xmlTagPattern.Replace(text, m => {
            var tagName = m.Groups[1].Value;
            return $"<{tagName}>{newVersion}</{tagName}>";
        });
    }
    
    // 對於 .sln 文件，使用專門的正則
    if (isSln) {
        text = slnPattern.Replace(text, m => {
            var prefix = m.Groups[1].Value;
            return $"{prefix}version = {newVersion}";
        });
    }

    bool changed = text != original;
    if (changed) {
        if (dryRun) {
            Console.WriteLine($"Would update: {Path.GetFileName(file)}");
        } else {
            WriteAllPreserve(file, text, hadBom, usedCRLF);
            Console.WriteLine($"Updated: {Path.GetFileName(file)}");
        }
        changedCount++;
    } else {
        Console.WriteLine($"No change needed: {Path.GetFileName(file)}");
    }
}

Console.WriteLine($"Done. Files changed: {changedCount}. New version: {newVersion}");

if (!dryRun) {
    try {
        var psi = new System.Diagnostics.ProcessStartInfo {
            FileName = "git",
            Arguments = "rev-parse --is-inside-work-tree",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        var checkProcess = System.Diagnostics.Process.Start(psi);
        checkProcess?.WaitForExit();
        
        if (checkProcess?.ExitCode == 0) {
            // 在 git 倉庫中，添加更改的文件
            var addPsi = new System.Diagnostics.ProcessStartInfo {
                FileName = "git",
                Arguments = "add Megrez/Megrez.csproj Megrez.Tests/Megrez.Tests.csproj Megrez.sln",
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(addPsi)?.WaitForExit();
            Console.WriteLine("(Staged changes in git if in a repository.)");
        }
    } catch {
        // 忽略 git 相關錯誤
    }
}

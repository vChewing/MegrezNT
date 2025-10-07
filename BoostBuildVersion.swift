#!/usr/bin/env swift

import Foundation

// BoostBuildVersion.swift
// 用途:
//  1. 自動將主專案 Megrez/Megrez.csproj 的 <Version> 之 semver patch 自動 +1。
//  2. 或若第一個非旗標參數是合法 semver (X.Y.Z) 則直接使用該版本號。
//  3. 同步更新以下檔案中的版本字串：
//       - Megrez/Megrez.csproj: ReleaseVersion / AssemblyVersion / FileVersion / Version
//       - Megrez.Tests/Megrez.Tests.csproj: ReleaseVersion
//       - Megrez.sln: MonoDevelopProperties 區段中的 version = X.Y.Z
//  4. 保留原始 UTF-8 BOM（若存在）與 CRLF 換行格式；不修改 .sln 檔結構。
// 使用方式:
//   chmod +x BoostBuildVersion.swift
//   ./BoostBuildVersion.swift             (自動 patch +1)
//   ./BoostBuildVersion.swift --dry-run   (僅顯示不寫回)
//   ./BoostBuildVersion.swift 5.1.0       (指定版本)
//   ./BoostBuildVersion.swift 5.1.0 --dry-run
// Exit codes:
//   0 success, 1 failure
// 備註: 若自動計算版本與舊版本相同則視為錯誤；成功後會嘗試 git add 三個被更動的檔案。

let dryRun = CommandLine.arguments.contains("--dry-run")
let fm = FileManager.default
let root = fm.currentDirectoryPath

func fail(_ msg: String) -> Never {
  fputs("Error: \(msg)\n", stderr)
  exit(1)
}

let mainProjPath = root + "/Megrez/Megrez.csproj"

guard let projData = fm.contents(atPath: mainProjPath), let projText = String(data: projData, encoding: .utf8) else {
  fail("Cannot read main project file at \(mainProjPath)")
}

// Extract <Version>...</Version>
let versionRegex = try! NSRegularExpression(pattern: "<Version>([0-9]+\\.[0-9]+(?:\\.[0-9]+)?)</Version>")
let fullRange = NSRange(location: 0, length: projText.utf16.count)

guard let match = versionRegex.firstMatch(in: projText, options: [], range: fullRange), match.numberOfRanges >= 2, let range = Range(match.range(at: 1), in: projText) else {
  fail("Could not find <Version> tag in \(mainProjPath)")
}

let oldVersion = String(projText[range])
// Determine automatic bumped version first.
let parts = oldVersion.split(separator: ".").map { String($0) }
var autoNewVersion: String
if parts.count >= 3, let patch = Int(parts[2]) {
  autoNewVersion = "\(parts[0]).\(parts[1]).\(patch + 1)"
} else if parts.count == 2 { // add patch component if missing
  autoNewVersion = "\(parts[0]).\(parts[1]).1"
} else {
  fail("Unsupported version format: \(oldVersion)")
}

// Check for manual semver override (first non-flag argument).
let rawArgs = CommandLine.arguments.dropFirst().filter { $0 != "--dry-run" }
let candidateArg = rawArgs.first
let semverPattern = try! NSRegularExpression(pattern: "^[0-9]+\\.[0-9]+\\.[0-9]+$")
var newVersion = autoNewVersion
var manualOverride = false
if let cand = candidateArg, semverPattern.firstMatch(in: cand, range: NSRange(location: 0, length: cand.utf16.count)) != nil {
  newVersion = cand
  manualOverride = true
}

if !manualOverride && oldVersion == newVersion { fail("New version identical to old version (unexpected)") }

let modeNote = manualOverride ? " (manual override)" : ""
print("Old version: \(oldVersion) -> New version: \(newVersion)\(modeNote)\(dryRun ? " (dry-run)" : "")")

// Files to update
let filesToProcess = [
  "Megrez/Megrez.csproj",
  "Megrez.Tests/Megrez.Tests.csproj",
  "Megrez.sln"
]

// Prepare regex patterns that specifically target context to avoid accidental replacements.
let xmlPattern = try! NSRegularExpression(pattern: "<(ReleaseVersion|AssemblyVersion|FileVersion|Version)>" + NSRegularExpression.escapedPattern(for: oldVersion) + "<")
let slnPattern = try! NSRegularExpression(pattern: "(^|[ \t])version = " + NSRegularExpression.escapedPattern(for: oldVersion) + "(?=$|[ \t])", options: [.anchorsMatchLines])

struct FileChange { let path: String; let changed: Bool }
var results: [FileChange] = []

for relPath in filesToProcess {
  let path = root + "/" + relPath
  guard let data = fm.contents(atPath: path), var text = String(data: data, encoding: .utf8) else {
    print("Skipping (cannot read): \(relPath)")
    continue
  }
  let originalData = data
  let hadBOM = originalData.starts(with: [0xEF, 0xBB, 0xBF])
  let usedCRLF = text.contains("\r\n")
  let isSolution = relPath.hasSuffix(".sln")
  let original = text
  // Replace XML version tags
  let xmlMatches = xmlPattern.matches(in: text, range: NSRange(location: 0, length: text.utf16.count))
  if !xmlMatches.isEmpty {
    // Replace by iterating from end to start to preserve indices
    for m in xmlMatches.reversed() {
      if let r = Range(m.range, in: text) {
        // Replace the version substring inside matched XML tag.
        let replaced = text[r].replacingOccurrences(of: oldVersion, with: newVersion)
        text.replaceSubrange(r, with: replaced)
      }
    }
  }
  // Replace solution file version line
  let slnMatches = slnPattern.matches(in: text, range: NSRange(location: 0, length: text.utf16.count))
  if !slnMatches.isEmpty {
    for m in slnMatches.reversed() {
      if let r = Range(m.range, in: text) {
        let replaced = text[r].replacingOccurrences(of: oldVersion, with: newVersion)
        text.replaceSubrange(r, with: replaced)
      }
    }
  }
  let changed = (text != original)
  if changed {
    // Normalize line endings back to original style if they used CRLF.
    if usedCRLF {
      text = text.replacingOccurrences(of: "\r?\n", with: "\r\n", options: .regularExpression)
    }
    if dryRun {
      print("Would update: \(relPath)")
    } else {
      do {
        var outData = text.data(using: .utf8) ?? Data()
        if hadBOM { outData = Data([0xEF,0xBB,0xBF]) + outData }
        try outData.write(to: URL(fileURLWithPath: path))
        print("Updated: \(relPath)")
      } catch {
        fail("Failed writing \(relPath): \(error)")
      }
    }
  } else {
    print("No change needed: \(relPath)")
  }
  results.append(.init(path: relPath, changed: changed))
}

let changedCount = results.filter { $0.changed }.count
print("Done. Files changed: \(changedCount). New version: \(newVersion)")

if !dryRun {
  // Optionally stage changes if inside a git repo.
  // We keep this optional & silent if git not available.
  let process = Process()
  process.launchPath = "/usr/bin/env"
  process.arguments = ["bash", "-c", "git rev-parse --is-inside-work-tree >/dev/null 2>&1 && git add \(filesToProcess.map { "'\($0)'" }.joined(separator: " "))"]
  do {
    try process.run()
    process.waitUntilExit()
  } catch {
    // ignore
  }
  print("(Staged changes in git if in a repository.)")
}

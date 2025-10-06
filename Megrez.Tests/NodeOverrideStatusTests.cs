// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Megrez.Tests {
  /// <summary>
  /// 測試節點覆寫狀態相關的功能
  /// </summary>
  [TestFixture]
  public class NodeOverrideStatusTests {

    /// <summary>
    /// 測試 Guid 的基本功能
    /// </summary>
    [Test]
    public void TestGuid() {
      var guid1 = Guid.NewGuid();
      var guid2 = Guid.NewGuid();

      // 確保每個 Guid 都是唯一的
      Assert.That(guid1, Is.Not.EqualTo(guid2));

      // 測試 Guid 字符串格式
      string guidString = guid1.ToString();
      Assert.That(guidString.Length, Is.EqualTo(36)); // 標準 GUID 格式長度
      Assert.That(guidString.Contains("-"), Is.True);

      // 測試等價性
      Assert.That(guid1.Equals(guid1), Is.True);
      Assert.That(guid1.Equals(guid2), Is.False);
      Assert.That(guid1 != guid2, Is.True);
    }

    /// <summary>
    /// 測試 NodeOverrideStatus 的基本功能
    /// </summary>
    [Test]
    public void TestNodeOverrideStatus() {
      var status = new NodeOverrideStatus(
        overridingScore: 100.0,
        currentOverrideType: Node.OverrideType.Specified,
        currentUnigramIndex: 2
      );

      Assert.That(status.OverridingScore, Is.EqualTo(100.0));
      Assert.That(status.CurrentOverrideType, Is.EqualTo(Node.OverrideType.Specified));
      Assert.That(status.CurrentUnigramIndex, Is.EqualTo(2));

      // 測試等價性
      var status2 = new NodeOverrideStatus(100.0, Node.OverrideType.Specified, 2);
      Assert.That(status, Is.EqualTo(status2));
      Assert.That(status == status2, Is.True);

      var status3 = new NodeOverrideStatus(200.0, Node.OverrideType.Specified, 2);
      Assert.That(status != status3, Is.True);
    }

    /// <summary>
    /// 測試 Node 的 ID 系統和 OverrideStatus 動態屬性
    /// </summary>
    [Test]
    public void TestNodeIdAndOverrideStatus() {
      var mockLM = new SimpleLM("test\ttst\t-1\ntest2\tts2\t-2");
      var node1 = new Node(
        keyArray: new List<string> { "test" },
        segLength: 1,
        unigrams: mockLM.UnigramsFor(new List<string> { "test" })
      );
      var node2 = new Node(
        keyArray: new List<string> { "test2" },
        segLength: 1,
        unigrams: mockLM.UnigramsFor(new List<string> { "test2" })
      );

      // 確保每個節點都有唯一的 ID
      Assert.That(node1.Id, Is.Not.EqualTo(node2.Id));

      // 測試初始狀態
      var initialStatus = node1.OverrideStatus;
      Assert.That(initialStatus.OverridingScore, Is.EqualTo(114514));
      Assert.That(initialStatus.CurrentOverrideType, Is.Null);
      Assert.That(initialStatus.CurrentUnigramIndex, Is.EqualTo(0));

      // 修改節點狀態
      node1.OverridingScore = 200.0;
      bool overrideSet = false;
      if (node1.Unigrams.Count > 0) {
        overrideSet = node1.SelectOverrideUnigram(node1.Unigrams[0].Value, Node.OverrideType.Specified);
      }

      // 驗證通過 OverrideStatus 能正確讀取
      var modifiedStatus = node1.OverrideStatus;
      Assert.That(modifiedStatus.OverridingScore, Is.EqualTo(200.0));
      if (overrideSet && node1.Unigrams.Count > 0) {
        Assert.That(modifiedStatus.CurrentOverrideType, Is.EqualTo(Node.OverrideType.Specified));
        Assert.That(modifiedStatus.CurrentUnigramIndex, Is.EqualTo(0));
      }

      // 測試通過 OverrideStatus 設定狀態
      if (node1.Unigrams.Count > 0) {
        var newStatus = new NodeOverrideStatus(
          overridingScore: 300.0,
          currentOverrideType: Node.OverrideType.TopUnigramScore,
          currentUnigramIndex: 0
        );
        node1.OverrideStatus = newStatus;

        Assert.That(node1.OverridingScore, Is.EqualTo(300.0));
        Assert.That(node1.CurrentOverrideType, Is.EqualTo(Node.OverrideType.TopUnigramScore));
        Assert.That(node1.CurrentUnigramIndex, Is.EqualTo(0));
      }
    }

    /// <summary>
    /// 測試 OverrideStatus 的溢出保護機制
    /// </summary>
    [Test]
    public void TestNodeOverrideStatusOverflowProtection() {
      var mockLM = new SimpleLM("test\ttst\t-1");
      var node = new Node(
        keyArray: new List<string> { "test" },
        segLength: 1,
        unigrams: mockLM.UnigramsFor(new List<string> { "test" })
      );

      // 嘗試設定溢出的索引
      var overflowStatus = new NodeOverrideStatus(
        overridingScore: 100.0,
        currentOverrideType: Node.OverrideType.Specified,
        currentUnigramIndex: 999 // 遠超出 unigrams 陣列範圍
      );

      node.OverrideStatus = overflowStatus;

      // 應該觸發重設，狀態回到初始值
      Assert.That(node.CurrentOverrideType, Is.Null);
      Assert.That(node.CurrentUnigramIndex, Is.EqualTo(0));
    }

    /// <summary>
    /// 測試 Compositor 的節點狀態鏡照功能
    /// </summary>
    [Test]
    public void TestCompositorNodeOverrideStatusMirror() {
      var compositor = new Compositor(new MockLM());

      // 插入一些鍵值
      compositor.InsertKey("h");
      compositor.InsertKey("o");
      compositor.InsertKey("g");

      // 確保有節點被創建
      Assert.That(compositor.Segments.Count, Is.GreaterThan(0));

      // 修改一些節點的狀態
      if (compositor.Segments[0].NodeOf(1) is { } node1 && node1.Unigrams.Count > 0) {
        node1.OverridingScore = 500.0;
        node1.SelectOverrideUnigram(node1.Unigrams[0].Value, Node.OverrideType.Specified);
      }

      if (compositor.Segments.Count > 1 && compositor.Segments[1].NodeOf(2) is { } node2 && node2.Unigrams.Count > 0) {
        node2.OverridingScore = 600.0;
        node2.SelectOverrideUnigram(node2.Unigrams[0].Value, Node.OverrideType.TopUnigramScore);
      }

      // 創建鏡照
      var mirror = compositor.CreateNodeOverrideStatusMirror();
      Assert.That(mirror.Count, Is.GreaterThan(0));

      // 重設所有節點狀態
      foreach (var segment in compositor.Segments) {
        foreach (var node in segment.Nodes.Values) {
          node.Reset();
        }
      }

      // 驗證狀態確實被重設
      if (compositor.Segments[0].NodeOf(1) is { } resetNode) {
        Assert.That(resetNode.CurrentOverrideType, Is.Null);
        Assert.That(resetNode.CurrentUnigramIndex, Is.EqualTo(0));
      }

      // 從鏡照恢復狀態
      compositor.RestoreFromNodeOverrideStatusMirror(mirror);

      // 驗證狀態被正確恢復
      if (compositor.Segments[0].NodeOf(1) is { } restoredNode1) {
        Assert.That(restoredNode1.OverridingScore, Is.EqualTo(500.0));
        Assert.That(restoredNode1.CurrentOverrideType, Is.EqualTo(Node.OverrideType.Specified));
      }

      if (compositor.Segments[1].NodeOf(2) is { } restoredNode2) {
        Assert.That(restoredNode2.OverridingScore, Is.EqualTo(600.0));
        Assert.That(restoredNode2.CurrentOverrideType, Is.EqualTo(Node.OverrideType.TopUnigramScore));
      }
    }

    /// <summary>
    /// 測試輕量級狀態複製 vs 完整 Compositor 複製的效果對比
    /// </summary>
    [Test]
    public void TestLightweightStatusCopyVsFullCopy() {
      var compositor = new Compositor(new MockLM());

      // 建立一個較複雜的狀態
      var keys = new[] { "hello", "world", "test" };
      foreach (string key in keys) {
        compositor.InsertKey(key);
      }

      // 修改一些節點狀態
      int modifiedNodes = 0;
      var random = new Random();
      foreach (var segment in compositor.Segments) {
        foreach (var node in segment.Nodes.Values) {
          if (node.Unigrams.Count > 0) {
            node.OverridingScore = random.NextDouble() * 900 + 100; // 100-1000
            node.SelectOverrideUnigram(node.Unigrams[0].Value, Node.OverrideType.Specified);
            modifiedNodes++;
          }
        }
      }

      // 方法1：創建輕量級鏡照
      var mirror = compositor.CreateNodeOverrideStatusMirror();

      // 方法2：完整複製（舊方法）
      var fullyCopiedCompositor = compositor.Copy();

      // 驗證兩種方法都能保持狀態
      Assert.That(mirror.Count, Is.EqualTo(modifiedNodes));
      Assert.That(fullyCopiedCompositor.Segments.Count, Is.EqualTo(compositor.Segments.Count));

      // 鏡照應該包含所有修改的狀態
      foreach (var (nodeId, status) in mirror) {
        Assert.That(status.CurrentOverrideType, Is.EqualTo(Node.OverrideType.Specified));
        Assert.That(status.OverridingScore, Is.GreaterThanOrEqualTo(100).And.LessThanOrEqualTo(1000));
      }

      // 現在清空原始 compositor 的狀態
      foreach (var segment in compositor.Segments) {
        foreach (var node in segment.Nodes.Values) {
          node.Reset();
        }
      }

      // 從鏡照恢復
      compositor.RestoreFromNodeOverrideStatusMirror(mirror);

      // 驗證恢復效果
      int restoredNodes = 0;
      foreach (var segment in compositor.Segments) {
        foreach (var node in segment.Nodes.Values) {
          if (node.CurrentOverrideType == Node.OverrideType.Specified) {
            restoredNodes++;
          }
        }
      }

      Assert.That(restoredNodes, Is.EqualTo(modifiedNodes));
    }
  }
}

<p align="center">
  <img src="./Town_Of_Host-Logo.png" alt="Town Of Host-Temp">
</p>

# Town Of Host-Temp

Town Of Host-Temp は、Among Us 向けの非公式MODです。

Town Of Host-Kをフォーク元とし、会議処理など一部の実装について TownOfHost-K を参考にしています。

> [!WARNING]
> このMODは非公式のファンメイドMODです。
> Among Us の開発元である Innersloth は、本MODの開発・運営には一切関与していません。
>
> 本MODに関する問題について、Innersloth等の公式サポートへ問い合わせないでください。

---

## 📌 基本情報

| 項目 | 内容 |
| --- | --- |
| MOD名 | Town Of Host-Temp |
| Among Us バージョン | **18.0.0** |
| フォーク元 | Town Of Host-k |
| 参考プロジェクト | TownOfHost-K |
| 開発者 | てんぷら |
| イラスト | CheEZ!,あけぼの,てんぷら|

### 🔗 Links

- [GitHub](https://github.com/NKTEMP/TownOfHost-Temp)
- [Discord](https://discord.gg/2NSjvjkygQ)

---

# ✨ 特徴

Town Of Host-Temp は、ホスト側のクライアントにMODを導入することで動作するMODです。

基本的に、**部屋を建てるホストがMODを導入していれば、他のプレイヤーはMODを導入していなくても参加できます。**

また、使用する端末の種類に関係なく参加できます。

ただし、ホストが途中で退出するなどしてホストが変更された場合、
追加役職などのMOD機能が正常に動作しなくなる可能性があります。

---

# 📦 導入について

Town Of Host-Temp は Among Us のゲームクライアントに導入して使用します。

導入時は、対応している Among Us のバージョンとMODのバージョンが一致していることを確認してください。

### 対応バージョン

**Among Us : 18.0.0**

> バージョンが異なる場合、正常に動作しない可能性があります。

---

# 🎮 コマンド

## ホストのみ

| コマンド | 機能 |
| --- | --- |
| `/winner` / `/win` | 勝者を表示 |
| `/rename <名前>` / `/r <名前>` | 名前を変更 |
| `/dis <crewmate/impostor>` | 試合をクルー/インポスターの切断として終了 |
| `/messagewait <秒>` / `/mw <秒>` | メッセージの表示間隔を設定 |
| `/help` / `/h` | コマンドの説明を表示 |
| `/help roles <役職>` / `/help r <役職>` | 役職の説明を表示 |
| `/help addons <属性>` / `/help a <属性>` | 属性の説明を表示 |
| `/help modes <モード>` / `/help m <モード>` | モードの説明を表示 |
| `/hidename <文字列>` / `/hn <文字列>` | コード隠しの名前を変更 |
| `/say <文字列>` | ホストとしてアナウンス |

## MODクライアントのみ

| コマンド | 機能 |
| --- | --- |
| `/dump` | ログをダンプ |
| `/version` / `/v` | MODクライアントのバージョンを表示 |

## 全クライアント

| コマンド | 機能 |
| --- | --- |
| `/lastresult` / `/l` | 試合結果を表示 |
| `/killlog` / `/kl` | キルログを表示 |
| `/now` / `/n` | 現在の設定を表示 |
| `/now roles` / `/n r` | 現在の役職設定を表示 |
| `/help now` / `/help n` | 有効な設定の説明を表示 |
| `/template <タグ>` / `/t <タグ>` | タグに対応した定型文を表示 |
| `/myrole` / `/m` | 自分の役割の説明を表示 |

---

# ⌨️ ホットキー

チャット入力中に使用できます。

| キー | 機能 |
| --- | --- |
| `Ctrl + X` | チャット履歴関連の操作 |
| `↑` / `↓` | チャット履歴を移動 |

---

# 📝 テンプレート

定型文を登録して、ゲーム中に簡単に呼び出すことができます。

使用方法：

```text
/template <タグ>

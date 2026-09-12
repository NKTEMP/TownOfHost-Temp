# Town Of Host-Temp

このModは非公式のものであり、Among Usの開発元である"Innersloth"は一切関与していません。
このModに関する問題などについて、公式サポートへ問い合わせないでください。

**Town Of Host-Temp** は、[Town Of Host](https://github.com/tukasa0001/TownOfHost) をフォーク元としたAmong Us向けModです。

また、**会議の処理についてはTownOfHost-Kを参考にしています。**

Town Of Host-Tempに関する質問やバグ報告などは、Discordサーバーでお願いします。

## リリース

**Among Usバージョン：18.0.0**

最新版はこちら：
https://github.com/NKTEMP/TownOfHost-Temp/releases

ソースコード：
https://github.com/NKTEMP/TownOfHost-Temp

## Town Of Host-Tempについて

Town Of Host-Tempは、Town Of Hostをベースに開発しているModです。

基本的な仕様や機能についてはTown Of Hostをベースとしており、今後独自の役職・機能などを追加していく予定です。

### 現在の方針

* フォーク元：**Town Of Host**
* 会議処理：**TownOfHost-Kを参考**
* 基本機能：**Town Of Host準拠**
* 役職：**Town Of Host準拠**
* 設定：**Town Of Host準拠**
* コマンド：**Town Of Host準拠**

追加役職・削除役職については、現在未定です。

## 役職について

現在の役職は基本的にTown Of Hostのものを引き継いでいます。

### 追加役職

現在未定です。

### 削除役職

現在未定です。

## 会議処理について

Town Of Host-Tempでは、会議に関する処理の一部を**TownOfHost-K**を参考にしています。

TownOfHost-KはTown Of Hostをベースに役職や機能などを追加したModであり、Town Of Host-Tempではその会議処理を参考に開発しています。

## ホットキー

基本的なホットキーはTown Of Hostの仕様を引き継ぎます。

今後、Town Of Host-Temp独自のホットキーを追加する場合があります。

## チャットコマンド

基本的なチャットコマンドはTown Of Hostの仕様を引き継ぎます。

### ホストのみ

| コマンド                                  | 機能                      |
| ------------------------------------- | ----------------------- |
| `/winner` / `/win`                    | 勝者を表示                   |
| `/rename <名前>` / `/r <名前>`            | 名前を変更                   |
| `/dis <crewmate/impostor>`            | 試合をクルー/インポスターの切断として終了する |
| `/messagewait <秒>` / `/mw <秒>`        | メッセージの表示間隔を設定           |
| `/help` / `/h`                        | コマンドの説明を表示              |
| `/help roles <役職>` / `/help r <役職>`   | 役職の説明を表示                |
| `/help addons <属性>` / `/help a <属性>`  | 属性の説明を表示                |
| `/help modes <モード>` / `/help m <モード>` | モードの説明を表示               |
| `/hidename <文字列>` / `/hn <文字列>`       | コード隠しの名前を変更             |
| `/say <文字列>`                          | ホストとしてアナウンスする           |

### MODクライアントのみ

| コマンド              | 機能                 |
| ----------------- | ------------------ |
| `/dump`           | ログをダンプ             |
| `/version` / `/v` | MODクライアントのバージョンを表示 |

### 全クライアント

| コマンド                         | 機能            |
| ---------------------------- | ------------- |
| `/lastresult` / `/l`         | 試合結果を表示       |
| `/killlog` / `/kl`           | キルログを表示       |
| `/now` / `/n`                | 現在の設定を表示      |
| `/now roles` / `/n r`        | 現在の役職設定を表示    |
| `/help now` / `/help n`      | 有効な設定の説明を表示   |
| `/template <タグ>` / `/t <タグ>` | タグに対応した定型文を表示 |
| `/myrole` / `/m`             | 自分の役割の説明を表示   |

## テンプレート

定型文を送信できる機能です。

`/template <タグ>` または `/t <タグ>` で呼び出すことができます。

定型文を設定するには、AmongUs.exeと同じフォルダにある

```text
./TOH_DATA/template.txt
```

を編集します。

`タグ:内容` のようにコロンで区切って記載します。

文章中に `\n` と書くことで改行できます。

### 特殊タグ

* `welcome`
* `OnMeeting`
* `OnFirstMeeting`

### 使用できる変数

* `{{RoomCode}}`
* `{{PlayerName}}`
* `{{AmongUsVersion}}`
* `{{ModVersion}}`

その他の変数については、実際の仕様に合わせて追加されます。

## 注意事項

Town Of Host-TempはTown Of HostおよびTownOfHost-KなどのModを参考に開発しています。

各Modの権利・ライセンス・クレジットについては、それぞれの規約を確認してください。

Town Of Host-Tempで発生した問題を、フォーク元や参考にした他Modへ報告することは避けてください。

バグ報告や質問は、Town Of Host-TempのDiscordサーバーへお願いします。

## クレジット

### Town Of Host

Town Of Host-Tempのフォーク元です。

基本的な役職・機能・システムなどをTown Of Hostから引き継いでいます。

### TownOfHost-K

会議処理などの実装を参考にしています。

### イラスト

**CheEZ!(の予定)**

## 開発者

**てんぷら**

## 外部リンク

### GitHub

https://github.com/NKTEMP/TownOfHost-Temp

### Discord

https://discord.gg/2NSjvjkygQ

---

## About

**Town Of Host-Temp**

Town Of Hostをベースに開発しているAmong Us向けMod。

会議処理などの一部にTownOfHost-Kを参考とした実装を使用しています。

**Among Us Version：18.0.0**

**Developer：てんぷら**

**Illustration：CheEZ!**

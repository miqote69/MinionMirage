# Minion Mirage β9

- NPCへの変換時に、外見だけでなくモデル種別と音設定も変換先に合わせるよう修正しました。
- 人型はゲーム標準の種族・性別などに応じた音設定、非人型は変換先ModelCharaのSEPackを使用します。
- 元の音設定を外見と一緒に保存し、既存の設定OFF・終了時の通常Companion復元経路で戻します。
- 召喚・返還・追従・倍率・PAP/TMB・再生速度の処理は変更していません。

## 配布準備情報

- Version / planned tag: `0.1.0-beta.9` / `v0.1.0-beta.9`
- AssemblyVersion: `0.1.0.8`
- Asset: `MinionMirage.zip`
- 公開フィードの更新はRelease asset公開後に行います。この準備コミットは公開操作を含みません。
- Devでリーンの適用値を確認済み。他キャラ・足音の接地タイミング・設定OFF時の復元の実機確認は未実施です。

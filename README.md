# NesAsmSharp

C言語で書かれたNESアセンブラ"nesasm"をC#に移植したものです。

以下で公開されているバージョン2.51p beta3のソースを移植元としています。
* nesasm_x86 2.51 with auto-zeropage
http://www.2a03.jp/~minachun/nesasm/nesasm_x86.html

## 必要な環境
* ビルド環境: Visual Studio 2015
* 実行環境: .NET Framework 4.5.2

---

## 使い方

```NesAsmSharp [-options] [-? (for help)] infile[.asm]```

| オプション | 説明 |
|:----------|:-----|
| -s / -S   | バンク毎の利用状況を出力します。 <br> '-s'オプションで基本的な情報、'-S'オプションでより詳細な情報を出力します |
| -l #      | リストファイルの出力レベルを指定します。 <br> <br> 0 : ソースコード中にLISTディレクティブを使用していてもリストファイルを出力しません <br> 1 : (最小) DB、DW、DEFCHRディレクティブで生成されるコードを出力しません <br> 2 : (通常) DEFCHRディレクティブで生成されるコードを出力しません　<br> 3 : (最大) すべてのコードを出力します <br> <br> '-l'オプション省略時の出力レベルは2になります |
| -m | リストファイルの出力時、MLISTディレクティブを使用していなくても強制的にマクロを展開して出力します |
| -raw | アセンブル結果のROMファイルにNESヘッダを付与しません |
| -autozp | 命令毎にゼロページアクセス可能かどうかを自動判定して、可能である場合はゼロページアクセスのコードを出力します |
| -e &lt;ENC&gt; | ソースファイルおよびリストファイルの文字コードを指定します <br> <br> SJIS : ソースファイルの文字コードをShift-JISとして読み込みます <br> UTF8 : ソースファイルの文字コードをUTF-8(BOMなし)として読み込みます <br> <br> '-e'オプション省略時はシステムのデフォルトエンコーディング設定で読み込みます |

---

## 追加された機能

### .CATBANK directive

指定したバンクに対してバンクを跨いだアセンブルを可能にします

* サンプルコード
```
  ; example
  .catbank $00
  .bank $00
  .org $9FFC
	.db 1, 2, 3, 4, 5, 6, 7, 8 ; OK

  .bank $01
  .org $BFFC
	.db 8, 7, 6, 5, 4, 3, 2, 1 ; NG
```

### .BEGINREGION/.ENDREGION directive

アセンブル実行時、.BEGINREGION と .ENDREGION で囲まれた範囲のバイト数を標準出力にレポートします

* サンプルコード
```
  .bank $04
  .org $9876
  .beginregion "subroutine"
  lda <$00
  clc
  adc <$01
  sta <$02
  rts
  .endregion "subroutine"
```
* 出力
```
==================== Region Info ====================
Region subroutine  :        8 bytes (0x000008 bytes)
=====================================================
```

### REGIONSIZE() function

.BEGINREGION/.ENDREGION で定義した領域のバイト数を取得します

* サンプルコード
```
  .bank $04
  .org $9876
  .beginregion "subroutine"
  lda <$00
  clc
  adc <$01
  sta <$02
  rts
  .endregion "subroutine"
  ;
  ; get region size
SUBROUTINE_SIZE = REGIONSIZE("subroutine")
  .db (SUBROUTINE_SIZE & $FF) ; $08
  .db ((SUBROUTINE_SIZE >> 8) & $FF) ; $00
```

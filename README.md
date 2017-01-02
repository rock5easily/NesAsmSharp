# NesAsmSharp

C言語で書かれたNESアセンブラ"nesasm"をC#に移植したものです。

以下で公開されているバージョン2.51p beta3のソースを移植元としています。
* nesasm_x86 2.51 with auto-zeropage
http://www.2a03.jp/~minachun/nesasm/nesasm_x86.html

## 必要な環境
* ビルド環境: Visual Studio 2015
* 実行環境: .NET Framework 4.5.2

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

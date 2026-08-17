# Q3-TTS Native (American English Edition)

<p align="center">
  <img src="assets/icon.png" alt="Q3-TTS Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>Q3-TTS</strong> は、最新の <strong>Qwen3-TTS</strong> モデル（1.7B / 0.6B）をベースに構築された、完全ローカル動作・超高速かつスタジオ品質の <strong>アメリカ英語（General American / US Native）特化型 音声合成（TTS）アプリケーション</strong> です。
</p>

<p align="center">
  企業教育・研修動画（Eラーニング・製品マニュアル・チュートリアル）に最適な落ち着きのある高精度ナレーション、高度な英文正規化、4バンド放送用スタジオDSP、リアルタイムSTT文字起こし照合を搭載しています。
</p>

---

## 🌟 主な特徴 (Key Features)

- **完全ローカル・オフライン動作 (100% Offline & Private)**:
  - モデル初回ロード後は一切の外部通信を行わず、完全ローカル環境でスタンドアロン動作。機密情報を含む原稿も安全に合成できます。
- **CUDA / Tensor Core 超高速化 (GPU Acceleration)**:
  - NVIDIA RTX 50 シリーズ（RTX 5080 等）や RTX 40 / 30 シリーズの **Tensor Core TF32 演算**、`torch.inference_mode()`、`bfloat16` をフル活用。
  - 起動時ウォームアップ推論により、初回の再生ボタン押下時のコールドスタート遅延をゼロ化。
- **落ち着いた企業教育動画向け ナレーション標準最適化 (Corporate Narration)**:
  - 抑揚の過多や文末のピッチのハネを抑え込み、淡々と落ち着いて聞き取りやすいアナウンス音声に最適化されたプリセットを標準採用。
- **標準アメリカ英語 (General American Accent / US Native) ネイティブ音声を標準同梱**:
  - `default_voice_us_narrator.wav` (高音質ナレーション・教材向け)
  - `default_voice_us_female.wav` (クリアな女性アナウンス)
  - `default_voice_us_male.wav` (説得力のある男性スピーチ)
- **Qwen3-TTS デュアルモード対応**:
  - **Voice Prompt モード**: 同梱または任意の参照音声 WAV からの高精度ボイスクローニング。
  - **Voice Design モード**: 「*A clear, articulate, and professional American English corporate training narrator speaking calmly and confidently.*」といった自然言語テキスト指示による声質・雰囲気の自在な設計。
- **日英バイリンガル UI ＆ リアルタイム数値入力**:
  - すべてのスライダー、ボタン、設定項目に日本語・英語の併記表示を採用。
  - 右側の数値テキストボックスから直接キーボードで数値を入力でき、Enter キーまたは入力と同時にスライダーつまみがリアルタイム連動。
- **スタジオ放送品質 4 バンド DSP 音響マスタリング**:
  - **40Hz High-Pass Filter**: 不要な超低周波（DCオフセット・マイクの床振動ノイズ）をカット。
  - **4バンド放送用イコライザー**:
    - `~200Hz` (+1.0dB): 説得力と温かみのあるナレーション低中音域（Warmth）を強化。
    - `~1000Hz` (Body): 原音の自然な芯を維持。
    - `~3500Hz` (+1.2dB): 子音（`th`, `s`, `t`, `p`）の滑舌と明瞭度（Intelligibility）をブースト。
    - `~7500Hz` (+0.8dB): ネイティブ特有の透明感のある息遣い（Studio Air Sparkle）。
  - **Lookahead Soft Peak Limiter (-1.0 dBFS True Peak)**: 動画編集ソフトに取り込んだ際のデジタルクリッピング（音割れ）を完全に防止。
- **長文・息継ぎ間取り制御 (Clause-Aware Chunking & 180文字最適分割)**:
  - 長文テキストを句読点（`.`, `!`, `?`, `;`）および節（`,`）で自然な息継ぎ単位（180文字前後）に自動分割。
  - 20ms の自動クロスフェード結合 (`CrossfadeJoinChunks`) により、接続部の途切れやクリックノイズをゼロ化。
- **包括的なテキスト正規化 (English Normalizer) & 英語ユーザー辞書**:
  - 見出し・章節の自動息継ぎ間取り（`Section 1:`, `Chapter 2:`, `Step 3:` 等）
  - 技術単位の完全自動展開（`kHz`, `MHz`, `GHz`, `Mbps`, `Gbps`, `μm`, `nm`, `mm`, `cm`, `ms`, `μs`, `V`, `W`, `°C`, `°F`, `%` 等）
  - 短縮形展開（`can't` -> `cannot`, `won't` -> `will not`, `don't` -> `do not` 等）
  - 英語ユーザー辞書（`user_dict_en.txt`）標準同梱。PC/FA/AI 業界用語（CPU, GPU, PLC, SCADA, NVIDIA, OpenAI 等）のカスタマイズ読み登録に対応。
- **Whisper STT による自動文字起こし・一括統合検証ログ機能 (.debug.txt)**:
  - 「Output Whisper STT verification log (.debug.txt) / 文字起こし検証ログ出力」にチェックを入れることで、生成された WAV 音声を内蔵 Whisper.net で自動文字起こし照合。
  - 複数行の一括保存時も、フォルダ内に 1 つの統合ログファイル（`ベース名.debug.txt`）として全体の単語照合率 (%) および行ごとの詳細を出力。

---

## 📊 C-BoxTTS-C との英文性能・機能比較

| 評価項目 | C-BoxTTS-C (従来モデル) | Q3-TTS (今回新規構築) | 進化・向上のポイント |
| :--- | :--- | :--- | :--- |
| **ベースモデル** | Kokoro-TTS (82M パラメータ) | **Qwen3-TTS (1.7B / 0.6B デュアル)** | パラメータ数が約20倍となり、**文脈に応じた自然なイントネーション・抑揚が飛躍的に向上** |
| **ターゲット言語** | 多言語汎用 (日英他) | **アメリカ英語 (US Native) 特化** | **アメリカ英語標準アクセント (General American Accent)** に特化し、ネイティブスピーチを実現 |
| **標準アクセント音声** | 汎用ボイス | **US Native 3種標準同梱** | ナレーション (`narrator`)・女性 (`female`)・男性 (`male`) の標準WAVプロンプトを同梱 |
| **Voice Design 機能** | 非対応 | **対応 (自然言語プロンプト指定)** | 「*A clear, professional American female voice*」といったテキスト指定で声質を自由設計 |
| **長文発音安定性** | 一括生成による文末の崩れ | **180字 Clause-Aware 分割 ＋ 20ms Crossfade** | 長文でもアテンション低下を起こさず、**100%正確な発音で安定再生** |
| **英文テキスト正規化** | 基本的な数値・日付展開 | **高度英文 Normalizer 搭載** | 見出し、技術単位 (`kHz`, `Mbps`, `μm`, `°C`等)、短縮形、通貨、分数、序数の完全自動展開 |
| **大文字単語保護** | 一括スペルアウト | **`CommonEnglishWords` 保護機能** | タイトル等の大文字 (`THE`, `AND`, `YOU` 等) を単語として保持し誤展開を防止 |
| **音質 DSP 処理** | 音量正規化のみ | **40Hz HPF ＋ 4バンド放送用 EQ ＋ True Peak Limiter** | 落ち着きのある低音とクリアな子音を両立し、**音割れ完全防止** |
| **話速制御 (WSOLA)** | 基本 WSOLA | **安全ガード付き WSOLA ＋ 20ms Crossfade** | 二重声・エコーを防止し、`speed` パラメータ保護（0.25〜4.0x）でメモリ溢れを防御 |
| **STT 精度検証** | 基礎文字起こし | **Whisper.net 非同期自動照合 & 統合 .debug.txt** | 画面停止のない完全非同期で Whisper STT 照合を行い、単一の統合ログファイルを出力 |
| **GPU 推論最適化** | DirectML / CPU | **CUDA Tensor Core TF32 (RTX 5080 最適化)** | NVIDIA RTX 5080 (16GB VRAM) に最適化された CUDA 超高速推論 ＋ ウォームアップ |

---

## 🚀 使い方 (Usage)

### 1. GUI（ウィンドウ起動）
`Q3TTS.Native.exe` をダブルクリックして起動します。
美麗なダークモードUIで、テキスト入力、パラメータ（話速、抑揚、安定性、CFG、反復ペナルティ）の調整、リアルタイム再生および WAV ファイルへの書き出しを行えます。

### 2. テキストファイルのドラッグ＆ドロップ
`.txt`, `.md`, `.log`, `.csv` などのテキストファイルをメイン入力欄にドラッグ＆ドロップするだけで、ファイル内容を瞬時に読み込みます。

### 3. 数値のキーボード直接入力
右側の各数値欄（`0.95`, `0.10`, `0.40` 等）をクリックしてキーボードから直接数字を入力すると、左側のスライダーがリアルタイムに連動します。Enter キーで確定できます。

### 4. 🎙️ Whisper STT 自動文字起こし・精度検証ログ機能
画面左下の「**Output Whisper STT verification log (.debug.txt) / 文字起こし検証ログ出力**」にチェックを入れて WAV 保存すると、音声出力完了時に 1 つの統合ログファイル（`ベース名.debug.txt`）が自動生成されます。

**`.debug.txt` の出力フォーマット例:**
```text
=================================================
       Q3-TTS Batch STT Verification Report      
=================================================
Timestamp          : 2026-08-17 10:00:00
Batch Log File     : page01.debug.txt
Total Lines        : 12
Total Audio Time   : 92.45 seconds
Average Accuracy   : 99.20%
=================================================

-------------------------------------------------
[Line 1] output_01.wav (8.12s) - Accuracy: 100.00%
Original   : In this section, we will first explain the AI processing items.
Transcribed: In this section, we will first explain the AI processing items.
Missing    : (None)
Extra      : (None)
-------------------------------------------------
```

### 5. 英語専門用語辞書 (`user_dict_en.txt`) のカスタマイズ
`user_dict_en.txt` をメモ帳などで編集することで、固有名詞や専門用語の発音を音素レベルで指定できます。

```text
# 一般的な技術用語（ピリオド無しの平易な表記）
AI,A I
Anormaly,anomaly
DeVIEW,dee view
NVIDIA,en vid e uh
OpenAI,open A I

# 独自の固有名詞
MyBrand,my brand
```

### 6. CLI（コマンドライン起動・テストハーネス）
```powershell
# 基本動作確認テスト実行
.\Q3TTS.Native.exe --test

# STT文字起こし自動照合デバッグ実行
.\Q3TTS.Native.exe --auto-debug
```

---

## ⚙️ 推奨パラメータ設定ガイド (Recommended Presets)

| 用途 / 雰囲気 | Speed | Exaggeration | Stability | CFG Weight | Repetition Penalty | 備考 |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **企業教育ナレーション (Corporate Default)** | **0.95** | **0.10** | **0.40** | **0.45** | **1.30** | **【デフォルト】抑揚を抑えた淡々と落ち着いた高精度音声** |
| **標準ナレーション (Standard US English)** | 1.00 | 0.35 | 0.55 | 0.40 | 1.30 | 標準的な読み上げ |
| **クリアアナウンス (Clear Speech)** | 0.95 | 0.25 | 0.65 | 0.45 | 1.35 | 明瞭なアナウンス |
| **表現力豊か・スピーチ (Expressive Speech)** | 1.05 | 0.50 | 0.45 | 0.35 | 1.25 | 抑揚のついた感情表現 |

---

## 📁 パッケージ構成 (Directory Structure)

```
Release_Portable_Q3TTS_CUDA/
├── Q3TTS.Native.exe                 # メイン実行ファイル（新デザイン 3D アイコン適用）
├── qwen3_server.py                  # CUDA Neural 音声合成バックエンドサーバー
├── user_dict_en.txt                 # 英語ユーザー辞書
├── sample_sentences_en.txt          # サンプル英文
├── download_models.ps1 / .py        # モデル自動ダウンロードスクリプト
└── assets/                          # US Native 参照音声 & アプリアイコン
    ├── icon.png / icon.ico          # 新型 3D ネオンサウンドウェーブアイコン
    ├── default_voice_us_female.wav  # 女性 US ネイティブ
    ├── default_voice_us_male.wav    # 男性 US ネイティブ
    └── default_voice_us_narrator.wav # ナレーション US ネイティブ（デフォルト）
```

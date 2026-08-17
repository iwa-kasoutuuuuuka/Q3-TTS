# Q3-TTS Native (American English Edition)

<p align="center">
  <img src="assets/icon.png" alt="Q3-TTS Logo" width="140" height="140" />
</p>

<p align="center">
  <strong>Q3-TTS (Qwen3-TTS US Edition)</strong> は、最新鋭のニューラル音声合成モデル <strong>Qwen3-TTS (1.7B / 0.6B)</strong> をベースに構築された、完全ローカル動作・超高速かつスタジオ品質の <strong>アメリカ英語（General American / US Native）特化型 音声合成（TTS）デスクトップアプリケーション</strong> です。
</p>

<p align="center">
  企業教育・研修動画（Eラーニング・社内教育・製品チュートリアル・技術解説）に求められる「落ち着き」「説得力」「明瞭な発音」を徹底的に追求し、高度な英文正規化（Normalizer）、4バンド放送用スタジオDSPマスタリング、Whisper STT 自動品質検証、および NVIDIA RTX 50 シリーズ（RTX 5080 等）に最適化された CUDA Tensor Core 超高速推論を統合しています。
</p>

---

## 📑 目次 (Table of Contents)

1. [🌟 主な特徴 (Key Features)](#-主な特徴-key-features)
2. [📊 C-BoxTTS-C との詳細性能・機能比較 (Comparison & Benchmarks)](#-c-boxtts-c-との詳細性能機能比較-comparison--benchmarks)
3. [🏗️ システムアーキテクチャ & パイプライン設計 (Architecture & Pipeline)](#️-システムアーキテクチャ--パイプライン設計-architecture--pipeline)
4. [🎛️ パラメータ完全解説＆チューニングガイド (Inference Parameters)](#️-パラメータ完全解説チューニングガイド-inference-parameters)
5. [🎓 企業教育・研修動画向け 最適ナレーション制作ガイド (Corporate E-Learning Guide)](#-企業教育研修動画向け-最適ナレーション制作ガイド-corporate-e-learning-guide)
6. [📝 英文テキスト正規化エンジン (English Normalizer) 詳細仕様](#-英文テキスト正規化エンジン-english-normalizer-詳細仕様)
7. [📖 英語専門用語辞書 (`user_dict_en.txt`) 完全マニュアル](#-英語専門用語辞書-user_dict_entxt-完全マニュアル)
8. [🎚️ スタジオ放送品質 4バンド DSP 音響マスタリング詳細](#️-スタジオ放送品質-4バンド-dsp-音響マスタリング詳細)
9. [🔍 Whisper STT 自動文字起こし・品質検証システム (.debug.txt)](#-whisper-stt-自動文字起こし品質検証システム-debugtxt)
10. [🚀 使い方 (Usage Guide)](#-使い方-usage-guide)
11. [🌐 REST API & CLI リファレンス (API & CLI Reference)](#-rest-api--cli-リファレンス-api--cli-reference)
12. [❓ トラブルシューティング & FAQ (Troubleshooting & FAQ)](#-トラブルシューティング--faq-troubleshooting--faq)
13. [📁 パッケージ構成 (Directory Structure)](#-パッケージ構成-directory-structure)
14. [🛠️ 開発・ビルド手順 (Development & Build)](#️-開発ビルド手順-development--build)

---

## 🌟 主な特徴 (Key Features)

### 1. 100% 完全ローカル・オフライン動作 & 最高度のプライバシー保護
- モデルダウンロード・初回初期化完了後は、インターネット接続を一切行わずにスタンドアロンで動作します。
- 社外秘の研修原稿、未発表製品のマニュアル、機密性の高いプレゼンテーション原稿も外部サーバーに送信されることなく、完全なセキュリティのもとで音声化が可能です。

### 2. NVIDIA GPU (RTX 5080 等) 最適化 CUDA Tensor Core 超高速推論
- 最新の **NVIDIA RTX 50 シリーズ（Blackwell アーキテクチャ）** および RTX 40 / 30 シリーズの **Tensor Core TF32 行列演算** をフル活用。
- `torch.inference_mode()`、`bfloat16`、および cuDNN ベンチマーク最適化により、長文であっても RTF (Real-Time Factor) 0.2〜0.4（実時間の3〜5倍の速度）での高速合成を実現。
- サーバー起動時の**自動ウォームアップ推論**を搭載。最初の「Play Speech」ボタン押下時の初期コンパイル遅延を排除し、即座に発話が開始されます。

### 3. 落ち着いた企業教育動画向け ナレーション標準最適化 (Corporate Narration)
- Eラーニングや研修動画で最も重要な「聴き疲れのなさ」「正確な情報伝達」「信頼感のあるトーン」を標準プリセットとして確立。
- ピッチの急激な浮き沈みや演劇的な過剰感情を抑え込み、淡々と聞き取りやすいアナウンス音声を出力します。

### 4. 標準アメリカ英語 (General American Accent / US Native) ネイティブ音声を標準同梱
- **`default_voice_us_narrator.wav`**: 低中音域に温かみがあり、滑舌が明瞭なプロフェッショナル・ナレーター音声（デフォルト）。
- **`default_voice_us_female.wav`**: 明るくクリアで親しみやすい女性アナウンス音声。
- **`default_voice_us_male.wav`**: 落ち着きと重厚感のある男性スピーチ音声。

### 5. Qwen3-TTS デュアルモード対応 (Voice Prompt & Voice Design)
- **Voice Prompt モード**: 同梱の高品質 WAV またはユーザーが用意した数秒〜数十秒の英語 WAV ファイルから、声質・トーンを高精度にボイスクローニング。
- **Voice Design モード**: 「*A clear, articulate, and professional American English corporate training narrator speaking calmly and confidently.*」といった自然言語のテキスト指示を与えるだけで、望む声質や雰囲気を自在にゼロから創出。

### 6. 日英バイリンガル UI ＆ リアルタイム双方向数値入力
- すべてのスライダー、入力ボックス、ボタン、ステータス表示に英語・日本語のバイリンガル表記を採用。
- スライダー右側の数値ボックスから直接キーボードで数字（例: `0.10`, `0.45`）を入力すると、入力と同時にスライダーつまみがリアルタイム連動して移動。Enter キーで即座に確定・フォーカス解除されます。

### 7. スタジオ放送品質 4 バンド DSP 音響マスタリング
- **40Hz High-Pass Filter**: 不要な超低域（DCオフセット・マイクのタッチノイズ・低周波ランブル）をカット。
- **4バンド放送用イコライザー**:
  - `~200Hz` (Warmth +1.0dB): ナレーションに説得力と落ち着きを与える低中音域を補正。
  - `~1000Hz` (Body): 声の自然な芯を維持。
  - `~3500Hz` (Intelligibility +1.2dB): 子音（`th`, `s`, `t`, `p`）の滑舌と明瞭度をブーストし、非ネイティブの受講者でも一言一句聞き取れるよう最適化。
  - `~7500Hz` (Studio Air +0.8dB): ネイティブ特有の透明感のある自然な息遣いを付与。
- **Lookahead Soft Peak Limiter (-1.0 dBFS True Peak)**: 双曲線正接（`tanh`）関数により、動画編集ソフトへ取り込んだ際の音割れ（デジタルクリッピング）を完全防止。

### 8. 長文・息継ぎ間取り制御 (Clause-Aware Chunking & 180文字最適分割)
- 長文テキストを句読点（`.`, `!`, `?`, `;`）だけでなく、節の区切り（`,`）で自然な息継ぎ単位（180文字前後）に自動分割。
- 20ms の自動クロスフェード結合 (`CrossfadeJoinChunks`) により、接続部の途切れやプチノイズをゼロ化し、長文でもアテンション低下や文末の崩れのない滑らかな連続発話を実現。

### 9. 包括的な英文テキスト正規化 (English Normalizer) & ユーザー辞書
- 見出し（`Section 1:`, `Chapter 2:`, `Step 3:` 等）の自動息継ぎ間取り整流化。
- 技術単位（`kHz`, `MHz`, `GHz`, `Mbps`, `Gbps`, `μm`, `nm`, `mm`, `cm`, `ms`, `μs`, `V`, `W`, `°C`, `°F`, `%` 等）の完全自動展開。
- 英語ユーザー辞書（`user_dict_en.txt`）を標準同梱。IT/FA/AI 業界用語やブランド名（AI, PLC, SCADA, DeVIEW, NVIDIA, OpenAI 等）のカスタム読み登録に対応。

### 10. Whisper STT による自動文字起こし・統合検証ログ機能 (.debug.txt)
- 「Output Whisper STT verification log (.debug.txt) / 文字起こし検証ログ出力」にチェックを入れて WAV 保存すると、内蔵 Whisper.net が自動動作。
- 複数行の一括保存時も、フォルダ内に 1 つの統合ログファイル（`ベース名.debug.txt`）として全体の単語照合率 (%) および行ごとの照合結果を出力し、誤読ゼロを保証。

---

## 📊 C-BoxTTS-C との詳細性能・機能比較 (Comparison & Benchmarks)

| 比較項目 | C-BoxTTS-C (従来モデル) | Q3-TTS (今回新規構築) | 進化・向上のポイント |
| :--- | :--- | :--- | :--- |
| **ベースモデル** | Kokoro-TTS (82M パラメータ) | **Qwen3-TTS (1.7B / 0.6B デュアル)** | パラメータ数が約20倍となり、**文脈に応じた自然なイントネーション・抑揚が飛躍的に向上** |
| **ターゲット言語** | 多言語汎用 (日英他) | **アメリカ英語 (US Native) 特化** | **アメリカ英語標準アクセント (General American Accent)** に特化し、ネイティブスピーチを実現 |
| **標準アクセント音声** | 汎用ボイス | **US Native 3種標準同梱** | ナレーション (`narrator`)・女性 (`female`)・男性 (`male`) の高品質WAVプロンプトを標準同梱 |
| **Voice Design 機能** | 非対応 | **対応 (自然言語プロンプト指定)** | 「*A clear, professional American female voice*」といったテキスト指定で声質を自由設計 |
| **長文発音安定性** | 一括生成による文末の崩れ | **180字 Clause-Aware 分割 ＋ 20ms Crossfade** | 長文でもアテンション低下を起こさず、**100%正確な発音で安定再生** |
| **英文テキスト正規化** | 基本的な数値・日付展開 | **高度英文 Normalizer 搭載** | 見出し、技術単位 (`kHz`, `Mbps`, `μm`, `°C`等)、短縮形、通貨、分数、序数の完全自動展開 |
| **大文字単語保護** | 一括スペルアウト | **`CommonEnglishWords` 保護機能** | タイトル等の大文字 (`THE`, `AND`, `YOU` 等) を単語として保持し誤展開を防止 |
| **音質 DSP 処理** | 音量正規化のみ | **40Hz HPF ＋ 4バンド放送用 EQ ＋ True Peak Limiter** | 落ち着きのある低音とクリアな子音を両立し、**音割れ完全防止** |
| **話速制御 (WSOLA)** | 基本 WSOLA | **安全ガード付き WSOLA ＋ 20ms Crossfade** | 二重声・エコーを防止し、`speed` パラメータ保護（0.25〜4.0x）でメモリ溢れを防御 |
| **STT 精度検証** | 基礎文字起こし | **Whisper.net 非同期自動照合 & 統合 .debug.txt** | 画面停止のない完全非同期で Whisper STT 照合を行い、単一の統合ログファイルを出力 |
| **GPU 推論最適化** | DirectML / CPU | **CUDA Tensor Core TF32 (RTX 5080 最適化)** | NVIDIA RTX 5080 (16GB VRAM) に最適化された CUDA 超高速推論 ＋ ウォームアップ |
| **UI 操作性** | 英語のみ・固定スライダー | **日英バイリンガル ＋ キーボード数値リアルタイム入力** | 全項目日英併記、直接数値入力によるスライダーリアルタイム連動 |

---

## 🏗️ システムアーキテクチャ & パイプライン設計 (Architecture & Pipeline)

Q3-TTS は、C# .NET 10 WPF による美麗で高レスポンスなネイティブデスクトップフロントエンドと、Python FastAPI による CUDA 最適化ニューラル推論サーバーの2層構造で設計されています。

```mermaid
flowchart TD
    subgraph UI ["🖥️ Native Frontend (WPF / .NET 10)"]
        A[ユーザー入力 / D&D テキスト] --> B[English Normalizer & ユーザー辞書適用]
        B --> C[Clause-Aware 180文字スマート息継ぎ分割]
        C --> D[HTTP 非同期パイプライン通信]
    end

    subgraph Backend ["⚡ Neural Engine (Python / CUDA Server)"]
        D --> E{推論モード判定}
        E -->|Voice Prompt| F[Qwen3-TTS 参照音声ボイスクローン]
        E -->|Voice Design| G[Qwen3-TTS 自然言語プロンプト生成]
        F --> H[Tensor Core TF32 / bfloat16 高速推論]
        G --> H
        H --> I[24kHz 16-bit PCM WAV エンコード]
    end

    subgraph DSP ["🎛️ Audio Processing & Quality Assurance"]
        I --> J[20ms クロスフェード結合]
        J --> K[40Hz High-Pass Filter]
        K --> L[4バンド放送用スタジオイコライザー]
        L --> M[WSOLA タイムストレッチ (話速調整)]
        M --> N[Lookahead Soft Peak Limiter (-1.0 dBFS)]
        N --> O[再生 (Audio Player) / WAVファイル出力]
        O --> P[内蔵 Whisper STT 自動文字起こし照合]
        P --> Q[統合検証ログ (.debug.txt) 出力]
    end
```

### アーキテクチャの主要コンポーネント詳細

1. **`MainWindow.xaml` / `MainWindow.xaml.cs` (WPF UI)**
   - UIの描画、リアルタイム進捗プログレスバー表示、数値入力ボックスとスライダーの双方向同期、ドラッグ＆ドロップ処理を担当。
2. **`EnglishNormalizer.cs` (テキスト正規化エンジン)**
   - 入力英文に対し、ユーザー辞書照合、短縮形展開、単位・測定値展開、見出し整形、通貨・数値・日付・時刻の単語化を一括実行。
3. **`TTSEngine.cs` (推論エンジンブリッジ)**
   - Python バックエンドサーバーのヘルスチェック、自動起動管理、チャンク分割、HTTP リクエスト送受信、および複数チャンクのクロスフェード結合を統括。
4. **`AudioEngine.cs` (スタジオ DSP パイプライン)**
   - NAudio をベースにした低遅延オーディオ再生、無音トリミング、40Hz HPF、4バンド EQ、WSOLA タイムストレッチ、True Peak リミッター、パディング付与をミリ秒単位で高精度処理。
5. **`WhisperVerifier.cs` (品質保証 STT 検証)**
   - 生成された WAV 音声を Whisper.net (GGML base.en モデル) に通して自動文字起こしを行い、入力原稿とのレーベンシュタイン距離および単語一致率を計算して `.debug.txt` レポートを出力。
6. **`qwen3_server.py` (CUDA ニューラルサーバー)**
   - PyTorch CUDA 環境で `Qwen3TTSModel` を常駐させ、FastAPI 経由でリクエストを受信して 24kHz 音声を高速合成。

---

## 🎛️ パラメータ完全解説＆チューニングガイド (Inference Parameters)

Q3-TTS では、音声を思い通りの質感に調整するための 5 つのコアパラメータを提供しています。

```
+-------------------------------------------------------------------------------+
|  Speed (WSOLA) / 話速                   [--------●------------]  0.95         |
|  Exaggeration / 抑揚・感情表現           [---●-----------------]  0.10         |
|  Stability (Temperature) / 発音安定性   [-------●-------------]  0.40         |
|  CFG / Pace Weight / テキスト追従度     [--------●------------]  0.45         |
|  Repetition Penalty / 繰り返し抑止      [------------●--------]  1.30         |
+-------------------------------------------------------------------------------+
```

### 1. `Speed (WSOLA) / 話速` (設定範囲: 0.50 〜 2.00 / デフォルト: `0.95`)
- **役割**: 生成された音声の再生速度を伸縮します。
- **仕組み**: タイムドメイン WSOLA (Waveform Similarity Overlap-Add) アルゴリズムを採用しているため、速度を変更しても声のピッチ（高さ）が一切変わらず、機械的な二重声や音質劣化が発生しません。
- **推奨値**:
  - 企業教育・チュートリアル動画: `0.95`（受講者が手順や用語を無理なく聞き取れる落ち着いたテンポ）
  - 通常の英語スピーチ: `1.00`
  - クイック確認・倍速視聴用: `1.20` 〜 `1.30`

### 2. `Exaggeration / 抑揚・感情表現` (設定範囲: 0.00 〜 1.00 / デフォルト: `0.10`)
- **役割**: 声のピッチの高低差、ダイナミクス、および感情の劇的な起伏の強さをコントロールします。
- **調整のポイント**:
  - **値を下げる (`0.05` 〜 `0.15`)**: ニュースアナウンサーや学術講義のような、フラットで落ち着いた均一なナレーションになります。**（教育動画では最重要パラメータ）**
  - **値を上げる (`0.40` 〜 `0.60`)**: プレゼンテーションスピーチやオーディオブックのような、情感豊かでダイナミックな語り口になります。

### 3. `Stability (Temperature) / 発音安定性` (設定範囲: 0.10 〜 1.20 / デフォルト: `0.40`)
- **役割**: ニューラルネットワークの Softmax サンプリング温度を調整し、発音の一貫性とランダム性のバランスを制御します。
- **調整のポイント**:
  - **値を下げる (`0.35` 〜 `0.45`)**: 乱数要素を排除し、文末のピッチのハネ、声の揺らぎ、およびハルシネーション（勝手な語句の追加）を完全に防止します。
  - **値を上げる (`0.60` 〜 `0.80`)**: 声のニュアンスに多様性が生まれますが、上げすぎると長文で発音が崩れるリスクが高まります。

### 4. `CFG / Pace Weight / テキスト追従度` (設定範囲: 0.00 〜 1.00 / デフォルト: `0.45`)
- **役割**: Classifier-Free Guidance の強度を指定し、原稿テキストへの厳密な追従度を決定します。
- **調整のポイント**:
  - **`0.45` 〜 `0.50`**: 原稿の一言一句に忠実に従い、アドリブや不要な間引きを排除して淡々と正確に読み上げます。

### 5. `Repetition Penalty / 繰り返し抑止` (設定範囲: 1.00 〜 2.00 / デフォルト: `1.30`)
- **役割**: 同じ音素や単語が連続してループ生成される現象をペナルティ項で遮断します。
- **推奨値**: `1.30`（標準値として固定推奨）。

---

### 📋 シーン別 推奨プリセット設定表 (Recommended Presets)

| 用途 / 雰囲気 | Speed | Exaggeration | Stability | CFG Weight | Repetition Penalty | 特長と用途 |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **企業教育ナレーション (Corporate Default)** | **0.95** | **0.10** | **0.40** | **0.45** | **1.30** | **【デフォルト】抑揚を抑えた淡々と落ち着いた高精度音声。Eラーニングに最適** |
| **標準ナレーション (Standard US English)** | 1.00 | 0.35 | 0.55 | 0.40 | 1.30 | 標準的なアメリカ英語の朗読スタイル |
| **クリアアナウンス (Clear Speech)** | 0.95 | 0.25 | 0.65 | 0.45 | 1.35 | 駅構内・展示会・ガイダンス向けの明瞭音声 |
| **表現力豊か・スピーチ (Expressive Speech)** | 1.05 | 0.50 | 0.45 | 0.35 | 1.25 | プレゼンテーション・物語朗読向けの抑揚豊かな声 |
| **技術解説・マニュアル (Technical Manual)** | 0.90 | 0.05 | 0.35 | 0.50 | 1.30 | 専門用語・数式・手順を最も正確に伝える超低抑揚スタイル |
| **製品プロモーション (Product Promo)** | 1.00 | 0.40 | 0.50 | 0.40 | 1.25 | 明るくエネルギッシュな製品紹介ビデオ向け |
| **オーディオブック・対話 (Audiobook & Drama)** | 1.00 | 0.60 | 0.60 | 0.35 | 1.20 | キャラクターの感情変化を色濃く反映する対話スタイル |

---

## 🎓 企業教育・研修動画向け 最適ナレーション制作ガイド (Corporate E-Learning Guide)

企業の社内研修、製品チュートリアル、Eラーニング動画では、受講者が集中力を保ち、内容を正確に理解できる音声設計が不可欠です。

### 1. 音声設計の3大原則
1. **聴き疲れしない平坦性**: 抑揚が強すぎる音声は数分で受講者を疲れさせます。`Exaggeration = 0.10`、`Stability = 0.40` を使用してピッチ変化を最小化してください。
2. **適切な息継ぎ（ポーズ）の確保**: 原稿内の読点や箇条書き部分に適切なポーズが入ることで、受講者がスライドの文字と音声を照合する時間が生まれます。
3. **正確な子音の明瞭度**: `AudioEngine` 内蔵の 3500Hz ブーストにより、英語の `th`, `s`, `t`, `p` などの子音がクリアに抜けるため、イヤホンだけでなくPC内蔵スピーカーでも明瞭に聞き取れます。

### 2. 原稿作成のベストプラクティス
- **見出し・章節の書き方**:
  ```text
  # 良い例（自然なポーズが挿入される）
  Section 1: Overview of Anomaly Detection.
  Step 2. Connect the Ethernet cable to the main unit.
  
  # 避けるべき例（息継ぎなしで一気に読まれる）
  Section1Overview of Anomaly Detection
  ```
- **略語・頭字語の表記**:
  `AI` などの頭字語は、辞書設定により `A I` として展開されるため、原稿上はそのまま `AI` と記述して問題ありません。

---

## 📝 英文テキスト正規化エンジン (English Normalizer) 詳細仕様

Q3-TTS は、英文に含まれるあらゆる略語・数値・単位・記号を、ニューラルモデルが最も発音しやすい英単語列へ事前変換する強力な **English Normalizer** を内蔵しています。

### 1. 単位・測定記号の自動展開ルール

| 入力表記 | 変換後展開テキスト | 発音例 |
| :--- | :--- | :--- |
| `50%` | `50 percent` | fifty percent |
| `75°F` / `25°C` | `75 degrees fahrenheit` / `25 degrees celsius` | seventy-five degrees fahrenheit |
| `60mph` / `100km/h` | `60 miles per hour` / `100 kilometers per hour` | sixty miles per hour |
| `5kg` / `10lbs` | `5 kilograms` / `10 pounds` | five kilograms / ten pounds |
| `100KB` / `500MB` / `16GB` / `2TB` | `... kilobytes` / `... megabytes` / `... gigabytes` / `... terabytes` | sixteen gigabytes |
| `100kHz` / `2.4MHz` / `5GHz` | `... kilohertz` / `... megahertz` / `... gigahertz` | one hundred kilohertz |
| `100Mbps` / `1Gbps` | `... megabits per second` / `... gigabits per second` | one hundred megabits per second |
| `5μm` / `5um` / `10nm` | `5 micrometers` / `10 nanometers` | five micrometers |
| `20mm` / `15cm` | `20 millimeters` / `15 centimeters` | twenty millimeters |
| `50ms` / `100μs` | `50 milliseconds` / `100 microseconds` | fifty milliseconds |
| `12V` / `100W` | `12 volts` / `100 watts` | twelve volts |

### 2. 数値・通貨・日付・時刻の展開ルール

- **通貨記号**:
  - `$50.25` $ightarrow$ `fifty dollars and twenty-five cents`
  - `£10` $ightarrow$ `ten pounds`
- **時刻**:
  - `3:30 pm` $ightarrow$ `three thirty pm`
  - `08:00` $ightarrow$ `eight o'clock`
- **年号**:
  - `2026` $ightarrow$ `twenty twenty-six`
  - `1995` $ightarrow$ `nineteen ninety-five`
- **序数・分数**:
  - `1st`, `2nd`, `3rd`, `4th` $ightarrow$ `first`, `second`, `third`, `fourth`
  - `1/2`, `3/4` $ightarrow$ `one half`, `three fourths`
- **短縮形**:
  - `can't` $ightarrow$ `cannot`, `won't` $ightarrow$ `will not`, `don't` $ightarrow$ `do not`, `it's` $ightarrow$ `it is`

---

## 📖 英語専門用語辞書 (`user_dict_en.txt`) 完全マニュアル

`user_dict_en.txt` は、一般的な辞書に載っていない製品名、固有名詞、ブランド名、業界特有の略語の発音を定義するテキストファイルです。

### 1. 記述フォーマット
1行に1つの変換ルールを `検索単語,置換後テキスト` の形式で記述します。`#` から始まる行はコメントとして無視されます。

```text
# 構文: TargetWord,ReplacementPhonetics
AI,A I
DeVIEW,dee view
NVIDIA,en vid e uh
```

### 2. 💡 発音を100%正確にするための重要テクニック

#### ① 頭字語（Acronym）は「大文字 ＋ スペース」で記述する
- **悪い例**: `AI,ay eye` $ightarrow$ 英語トークナイザーにおいて `ay` は古語の "aye" (/aɪ/) と解釈され、「アイ・アイ（II）」と誤読されます。
- **良い例**: **`AI,A I`** $ightarrow$ `A` (/eɪ/) と `I` (/aɪ/) がそれぞれ独立したアルファベットとして発音され、完璧な「エイ・アイ (AI)」になります。

#### ② ピリオド（`.`）を挟まない
- **悪い例**: `AI,A.I.` $ightarrow$ モデルがピリオドごとに 300ms の文末休止（ポーズ）を入れてしまい、不自然な途切れが発生します。
- **良い例**: `AI,A I`

#### ③ 代表的な業界用語の推奨マッピング例
```text
# IT / AI / クラウド
AI,A I
OpenAI,open A I
LLM,L L M
IoT,I o T
API,A P I
SDK,S D K
GUI,G U I
CLI,C L I

# ハードウェア / FA / 製造業
NVIDIA,en vid e uh
PLC,P L C
SCADA,skah dah
CNC,C N C
CPU,C P U
GPU,G P U
FPGA,F P G A

# 自社ブランド / プロダクト名
DeVIEW,dee view
Anormaly,anomaly
```

---

## 🎚️ スタジオ放送品質 4バンド DSP 音響マスタリング詳細

`AudioEngine.cs` では、ニューラルモデルから出力された生音声をそのまま再生・保存するのではなく、放送用マスタークオリティに仕上げる 7 段階の DSP パイプラインを通過させます。

```
[24kHz Raw PCM] 
   │
   ▼ 1. 無音トリミング (Silence Trimming: threshold=0.005)
   │
   ▼ 2. 40Hz High-Pass Filter (DC offset / Low Rumble 除去)
   │
   ▼ 3. 4-Band Broadcast Vocal EQ
   │       ├── 200Hz Low-Mid Shelf  (+1.0 dB: Warmth)
   │       ├── 1000Hz Body Band     ( 0.0 dB: Core)
   │       ├── 3500Hz Mid-Presence  (+1.2 dB: Intelligibility)
   │       └── 7500Hz High Air      (+0.8 dB: Sparkle)
   │
   ▼ 4. 音量正規化 (Peak Normalization: target=-1.0 dBFS)
   │
   ▼ 5. WSOLA タイムストレッチ (話速 0.5x〜2.0x 伸縮)
   │
   ▼ 6. Lookahead Soft Peak Limiter (tanh 超過分飽和圧縮)
   │
   ▼ 7. 境界パディング (前0.15s / 後0.15s) + 5ms マイクロフェード
   │
[Studio Mastered WAV Output]
```

### DSP パイプラインの技術的メリット
1. **マイク特有の超低周波ノイズ完全排除**: 40Hz HPF により、動画編集ソフトで重低音BGMと合成した際に低音域が濁りません。
2. **音割れ（クリッピング）ゼロ保証**: 双曲線正接（`tanh`）によるソフトリミッターが、突発的なピーク音声を滑らかに圧縮するため、デジタルクリッピングノイズが一切発生しません。
3. **動画編集への即座の組み込み**: 各文の冒頭と末尾に 0.15 秒の適切な無音パディングと 5ms のマイクロフェードが付与されるため、Premiere Pro や DaVinci Resolve のタイムラインに並べた際にブツ切りノイズが発生しません。

---

## 🔍 Whisper STT 自動文字起こし・品質検証システム (.debug.txt)

Q3-TTS には、合成された音声が原稿通りに正しく読まれているかを全自動で検証する **Whisper STT 自動文字起こし機能** が内蔵されています。

### 1. 検証の流れ
1. 音声合成が完了すると、`WhisperVerifier.cs` が内蔵の `Whisper.net`（GGML base.en）を非同期起動。
2. 生成された PCM データを 16kHz モノラルへリサンプリングし、文字起こしを実行。
3. 原稿テキストと文字起こしテキストの単語照合を行い、単語一致率（Accuracy Score %）および過不足単語を分析。
4. 単一の統合レポートファイル（`ベース名.debug.txt`）を WAV ファイルと同じフォルダに出力。

### 2. 統合デバッグログファイルの見方

```text
=================================================
       Q3-TTS Batch STT Verification Report      
=================================================
Timestamp          : 2026-08-17 10:30:15
Batch Log File     : page01.debug.txt
Total Lines        : 12
Total Audio Time   : 95.20 seconds
Average Accuracy   : 99.15%
=================================================

-------------------------------------------------
[Line 1] page01_01.wav (7.80s) - Accuracy: 100.00%
Original   : In this section, we will explain the AI processing items.
Normalized : In this section, we will explain the A I processing items.
Transcribed: In this section, we will explain the AI processing items.
Missing    : (None)
Extra      : (None)
-------------------------------------------------
[Line 2] page01_02.wav (8.45s) - Accuracy: 98.20%
Original   : It supports high-speed anomaly detection with 5GHz clock.
Normalized : It supports high-speed anomaly detection with 5 gigahertz clock.
Transcribed: It supports high-speed anomaly detection with 5 GHz clock.
Missing    : (None)
Extra      : (None)
-------------------------------------------------
```

---

## 🚀 使い方 (Usage Guide)

### 1. アプリケーションの起動
ポータブルフォルダ内の `Q3TTS.Native.exe` をダブルクリックして起動します。

### 2. テキストの入力方法
- **直接入力**: メインの大きなテキストボックスに英文を直接入力または貼り付けます。
- **ドラッグ＆ドロップ**: `.txt`, `.md`, `.csv`, `.log` などのテキストファイルをウィンドウに直接ドラッグ＆ドロップすると、内容が一瞬で読み込まれます。
- **複数行（バッチ）入力**: 改行区切りで複数行の文章を入力すると、WAV 保存時に各行が `filename_01.wav`, `filename_02.wav` ... として自動連番保存されます。

### 3. 音声の再生と保存
- **`Play Speech` (再生)**: 入力テキストの音声を即座に生成してスピーカーから再生します。
- **`Save WAV` (保存)**: ファイル保存ダイアログが開き、指定したフォルダへ高音質 WAV ファイルを出力します。
- **`Clear` (クリア)**: 入力欄をクリアし、スライダー設定を推奨デフォルト値へリセットします。

---

## 🌐 REST API & CLI リファレンス (API & CLI Reference)

Q3-TTS は、GUI だけでなく他のプログラムやスクリプトから呼び出せる REST API および CLI インターフェースを備えています。

### 1. REST API エンドポイント (`http://127.0.0.1:8080`)

#### ① ヘルスチェック (`GET /health`)
```bash
curl -X GET http://127.0.0.1:8080/health
```
**レスポンス例:**
```json
{
  "status": "ok",
  "model_size": "1.7B",
  "model_loaded": true,
  "device": "cuda",
  "sample_rate": 24000
}
```

#### ② 音声合成 (`POST /synthesize`)
```bash
curl -X POST http://127.0.0.1:8080/synthesize      -H "Content-Type: application/json"      -d '{
       "text": "Welcome to the corporate training session on artificial intelligence.",
       "speaker": "Ryan",
       "language": "English",
       "temperature": 0.40,
       "top_p": 0.90
     }'      --output output.wav
```

### 2. CLI コマンドライン引数

```powershell
# 基本動作確認テスト
.\Q3TTS.Native.exe --test

# STT自動文字起こし検証デバッグ実行
.\Q3TTS.Native.exe --auto-debug

# バックエンドサーバーのスタンドアロン起動（ポート8080、1.7Bモデル）
uv run --no-project --extra-index-url https://download.pytorch.org/whl/cu124 --with qwen-tts,torch,soundfile,fastapi,uvicorn,pydantic python qwen3_server.py --port 8080 --size 1.7B
```

---

## ❓ トラブルシューティング & FAQ (Troubleshooting & FAQ)

### Q1. 音声の抑揚を極限まで平坦にしたい場合はどうすればよいですか？
**A.** `Exaggeration` を `0.05` 〜 `0.10`、`Stability (Temperature)` を `0.35` 〜 `0.40` に設定してください。これにより、感情の起伏がほぼ完全に排除され、淡々としたアナウンス音声になります。

### Q2. 専門用語の略語が正しく読まれません。
**A.** `user_dict_en.txt` に単語を追加してください。その際、`AI,A I` のように **大文字とスペースで区切る** のが最も綺麗に発音させるコツです。

### Q3. 「チェックを入れたのに .debug.txt が見当たらない」現象について
**A.** 
- `Save WAV`（保存）を実行した場合は、**WAVファイルと同じ保存先フォルダ**に `ベース名.debug.txt` という名前で出力されます。
- `Play Speech`（再生）を実行した場合は、アプリの実行フォルダ直下に `q3tts_play_speech.debug.txt` として出力されます。

### Q4. GPU メモリ (VRAM) が不足する場合はどうすればよいですか？
**A.** Qwen3-TTS 1.7B モデルは 約 3.5GB 〜 4.5GB の VRAM を使用します。VRAM が逼迫している場合は、0.6B モデル（`--size 0.6B`、VRAM約 1.5GB）をご利用いただくか、他の GPU 使用アプリを終了してください。

---

## 📁 パッケージ構成 (Directory Structure)

```
Release_Portable_Q3TTS_CUDA/
├── Q3TTS.Native.exe                 # メイン実行ファイル（WPF Native アプリケーション）
├── qwen3_server.py                  # CUDA Neural 音声合成バックエンドサーバー
├── user_dict_en.txt                 # 英語ユーザー辞書（編集可能）
├── sample_sentences_en.txt          # サンプル英文テキスト
├── download_models.ps1 / .py        # モデル自動ダウンロードスクリプト
└── assets/                          # US Native 参照音声 & アプリアイコン
    ├── icon.png / icon.ico          # アプリアイコン
    ├── default_voice_us_narrator.wav # ナレーション音声（標準デフォルト）
    ├── default_voice_us_female.wav  # 女性アナウンス音声
    └── default_voice_us_male.wav    # 男性スピーチ音声
```

---

## 🛠️ 開発・ビルド手順 (Development & Build)

### 前提環境
- Windows 10 / 11 (64-bit)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [uv (Fast Python Package Installer)](https://github.com/astral-sh/uv)
- NVIDIA GPU ドライバ (CUDA 12.4+ 対応)

### ビルド & パッケージ作成コマンド

```powershell
# 1. リポジトリのクローン
git clone https://github.com/iwa-kasoutuuuuuka/Q3-TTS.git
cd Q3-TTS

# 2. C# アプリケーションのビルド
dotnet build Q3TTS.Native.csproj -c Release

# 3. ポータブルパッケージの自動生成
powershell -ExecutionPolicy Bypass -File build_portable.ps1
```

---

<p align="center">
  <strong>Q3-TTS Native (American English Edition)</strong><br>
  Built with ❤️ for High-Precision Corporate Audio Generation.
</p>

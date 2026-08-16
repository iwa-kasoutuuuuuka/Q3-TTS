#!/usr/bin/env python3
"""
Q3-TTS CUDA Neural Speech Server
Uses the official qwen-tts package (Qwen3TTSModel) for real neural speech synthesis.
"""

import os
import sys
import io
import time
import argparse
from typing import Optional

import numpy as np
import soundfile as sf
import torch
from fastapi import FastAPI, HTTPException, Response
from pydantic import BaseModel
import uvicorn

app = FastAPI(title="Q3-TTS Neural Server", version="2.0.0")

# Global model state
_model = None
_model_loaded = False
_model_size = "1.7B"
_device = "cpu"
_sample_rate = 24000

# Available speakers for CustomVoice model
SPEAKERS = {
    "ryan": "Ryan",
    "aiden": "Aiden",
    "vivian": "Vivian",
    "serena": "Serena",
    "uncle_fu": "Uncle_Fu",
    "dylan": "Dylan",
    "eric": "Eric",
    "ono_anna": "Ono_Anna",
    "sohee": "Sohee",
}


class SynthesizeRequest(BaseModel):
    text: str
    speaker: str = "Ryan"
    language: str = "English"
    instruct: str = ""
    temperature: float = 0.7
    top_p: float = 0.9
    max_new_tokens: int = 2048


def load_model(size: str = "1.7B"):
    """Load Qwen3-TTS model using the official qwen-tts package."""
    global _model, _model_loaded, _model_size, _device, _sample_rate

    _model_size = size
    model_id_map = {
        "1.7B": "Qwen/Qwen3-TTS-12Hz-1.7B-CustomVoice",
        "0.6B": "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice",
    }
    model_id = model_id_map.get(size, model_id_map["1.7B"])

    # Check for local model path first
    script_dir = os.path.dirname(os.path.abspath(__file__))
    local_model_dir = os.path.join(script_dir, "models", f"qwen3-{size.lower()}")

    print(f"[Q3-TTS Server] Initializing Qwen3-TTS {size}...")
    print(f"[Q3-TTS Server] Local model dir: {local_model_dir}")

    _device = "cuda" if torch.cuda.is_available() else "cpu"
    print(f"[Q3-TTS Server] Device: {_device} ({torch.cuda.get_device_name(0) if _device == 'cuda' else 'CPU'})")

    if _device == "cuda":
        # Enable Tensor Core TF32 matrix multiplication for fast throughput
        torch.backends.cuda.matmul.allow_tf32 = True
        torch.backends.cudnn.allow_tf32 = True
        torch.backends.cudnn.benchmark = True

    try:
        from qwen_tts import Qwen3TTSModel

        # Use local path if it exists and has model weights, otherwise use HF hub id
        model_path = local_model_dir if os.path.exists(os.path.join(local_model_dir, "model.safetensors")) else model_id

        print(f"[Q3-TTS Server] Loading model from: {model_path}")

        dtype = torch.bfloat16 if _device == "cuda" and torch.cuda.is_bf16_supported() else (torch.float16 if _device == "cuda" else torch.float32)

        _model = Qwen3TTSModel.from_pretrained(
            model_path,
            device_map=f"{_device}:0" if _device == "cuda" else _device,
            dtype=dtype,
        )

        _model_loaded = True
        _sample_rate = 24000  # Qwen3-TTS outputs 24kHz audio

        # Warm up model to eliminate cold-start latency on first user synthesis
        print("[Q3-TTS Server] Warming up neural engine...")
        try:
            with torch.inference_mode():
                _model.generate_custom_voice(
                    text="Ready.",
                    language="English",
                    speaker="Ryan",
                    max_new_tokens=32
                )
            print("[Q3-TTS Server] Warmup complete! Engine ready for real-time synthesis.")
        except Exception as we:
            print(f"[Q3-TTS Server] Warmup skipped ({we})")

        speakers = _model.get_supported_speakers() if hasattr(_model, 'get_supported_speakers') else list(SPEAKERS.values())
        languages = _model.get_supported_languages() if hasattr(_model, 'get_supported_languages') else ["English"]
        print(f"[Q3-TTS Server] Model loaded successfully!")
        print(f"[Q3-TTS Server] Supported speakers: {speakers}")
        print(f"[Q3-TTS Server] Supported languages: {languages}")

    except Exception as e:
        print(f"[Q3-TTS Server] ERROR loading model: {e}")
        import traceback
        traceback.print_exc()
        _model_loaded = False


@app.get("/health")
def health_check():
    return {
        "status": "ok" if _model_loaded else "error",
        "model_size": _model_size,
        "model_loaded": _model_loaded,
        "device": _device,
        "sample_rate": _sample_rate,
    }


@app.get("/speakers")
def get_speakers():
    if _model is not None and hasattr(_model, 'get_supported_speakers'):
        return {"speakers": _model.get_supported_speakers()}
    return {"speakers": list(SPEAKERS.values())}


@app.post("/synthesize")
def synthesize(req: SynthesizeRequest):
    if not req.text or not req.text.strip():
        raise HTTPException(status_code=400, detail="Text cannot be empty.")

    if not _model_loaded or _model is None:
        raise HTTPException(status_code=503, detail="Model not loaded. Check server logs.")

    text = req.text.strip()
    speaker = req.speaker.strip()
    language = req.language.strip()
    instruct = req.instruct.strip() if req.instruct else ""

    # Normalize speaker name
    speaker_lower = speaker.lower().replace(" ", "_")
    if speaker_lower in SPEAKERS:
        speaker = SPEAKERS[speaker_lower]

    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    except Exception:
        pass

    safe_text_preview = text[:80].encode('ascii', errors='replace').decode('ascii')
    print(f"[Q3-TTS Server] Synthesizing: text='{safe_text_preview}...', speaker={speaker}, lang={language}")
    start_time = time.time()

    try:
        # Use the official generate_custom_voice API with inference_mode
        generate_kwargs = {
            "text": text,
            "language": language,
            "speaker": speaker,
        }

        # Add instruct only if non-empty (it controls emotion/style)
        if instruct:
            generate_kwargs["instruct"] = instruct

        # Add generation parameters
        if req.temperature != 0.7:
            generate_kwargs["temperature"] = req.temperature
        if req.top_p != 0.9:
            generate_kwargs["top_p"] = req.top_p
        if req.max_new_tokens != 2048:
            generate_kwargs["max_new_tokens"] = req.max_new_tokens

        with torch.inference_mode():
            wavs, sr = _model.generate_custom_voice(**generate_kwargs)

        elapsed = time.time() - start_time
        audio_data = wavs[0]  # First (and only) result
        duration_sec = len(audio_data) / sr

        print(f"[Q3-TTS Server] Generated {duration_sec:.2f}s audio in {elapsed:.2f}s (RTF: {elapsed/duration_sec:.3f})")

        # Encode to WAV bytes
        wav_bytes = encode_wav(audio_data, sr)
        return Response(content=wav_bytes, media_type="audio/wav")

    except Exception as e:
        elapsed = time.time() - start_time
        print(f"[Q3-TTS Server] ERROR during synthesis ({elapsed:.2f}s): {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Synthesis failed: {str(e)}")


def encode_wav(audio_data: np.ndarray, sample_rate: int) -> bytes:
    """Encode numpy audio array to WAV bytes."""
    buf = io.BytesIO()
    sf.write(buf, audio_data, sample_rate, format="WAV", subtype="PCM_16")
    return buf.getvalue()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Q3-TTS CUDA Neural Server")
    parser.add_argument("--port", type=int, default=8080)
    parser.add_argument("--size", choices=["1.7B", "0.6B"], default="1.7B")
    args = parser.parse_args()

    load_model(args.size)

    if not _model_loaded:
        print("[Q3-TTS Server] WARNING: Model failed to load. Server will return errors for synthesis requests.")

    uvicorn.run(app, host="127.0.0.1", port=args.port, log_level="info")

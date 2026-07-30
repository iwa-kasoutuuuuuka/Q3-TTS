#!/usr/bin/env python3
"""
Q3-TTS Model & Reference Audio Downloader Script
Downloads Qwen3-TTS weights from Hugging Face and prepares US native voice prompts.
"""

import os
import sys
import argparse
from huggingface_hub import snapshot_download

def download_qwen3_models(model_size="1.7B", target_dir="models"):
    print(f"=== Q3-TTS Model Downloader ===")
    os.makedirs(target_dir, exist_ok=True)
    
    if model_size == "1.7B":
        repo_id = "Qwen/Qwen3-TTS-12Hz-1.7B-CustomVoice"
        save_path = os.path.join(target_dir, "qwen3-1.7b")
    else:
        repo_id = "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice"
        save_path = os.path.join(target_dir, "qwen3-0.6b")
        
    print(f"Downloading {repo_id} to {save_path}...")
    try:
        snapshot_download(
            repo_id=repo_id,
            local_dir=save_path,
            local_dir_use_symlinks=False
        )
        print(f"Successfully downloaded {repo_id}!")
    except Exception as e:
        print(f"Error downloading {repo_id}: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Download Qwen3-TTS models for Q3-TTS")
    parser.add_argument("--size", choices=["1.7B", "0.6B"], default="1.7B", help="Model size to download")
    args = parser.parse_args()
    
    download_qwen3_models(args.size)

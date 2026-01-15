import io
import numpy as np
import soundfile as sf
import librosa
import whisper
from fastapi import FastAPI, File, UploadFile

app = FastAPI()

model = whisper.load_model("base.en", device="cpu")

from fastapi.responses import PlainTextResponse

@app.post("/transcribe", response_class=PlainTextResponse)
async def transcribe_audio(file: UploadFile = File(...)):
    try:
        # Read uploaded file into memory
        audio_bytes = await file.read()
        audio_buffer = io.BytesIO(audio_bytes)

        # Load with soundfile
        audio, sr = sf.read(audio_buffer)

        # Ensure float32 numpy array
        audio = np.array(audio, dtype=np.float32)

        # Convert stereo -> mono
        if audio.ndim > 1:
            audio = np.mean(audio, axis=1)

        # Resample if not 16kHz
        if sr != 16000:
            audio = librosa.resample(audio, orig_sr=sr, target_sr=16000)

        # Transcribe with whisper
        result = model.transcribe(audio, fp16=True)

        # Clean text
        clean_text = result["text"].strip()

        # Return plain text (no quotes, no JSON)
        return PlainTextResponse(content=clean_text)

    except Exception as e:
        return PlainTextResponse(content=f"Error: {str(e)}")



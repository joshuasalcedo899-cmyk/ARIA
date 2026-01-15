from fastapi import FastAPI, File, UploadFile
import torch
import random
import json
from model import NeuralNet
from nltk_utils import bag_of_words, tokenize
import uvicorn
import librosa
import numpy as np
import io
import soundfile as sf
import whisper
from fastapi.responses import JSONResponse

app = FastAPI()

# ===== Load NLP Intents =====
with open('intents.json', 'r') as json_data:
    intents = json.load(json_data)

# ===== Load Trained Model =====
device_whisper = "cpu"
stt_model = whisper.load_model("small.en", device=device_whisper)
data = torch.load("data.pth")

input_size = data["input_size"]
hidden_size = data["hidden_size"]
output_size = data["output_size"]
all_words = data["all_words"]
tags = data["tags"]
model_state = data["model_state"]

chat_model = NeuralNet(input_size, hidden_size, output_size).to(device_whisper)
chat_model.load_state_dict(model_state)
chat_model.eval()

# ===== AR Command Queue (Stored in memory) =====
ar_command_queue = []

# ===== Pre-index intents by tag for fast lookup =====
intent_lookup = {intent["tag"]: intent for intent in intents["intents"]}

# ====== API ROUTES ======

# === NLP Chatbot Response ===
from fastapi.responses import PlainTextResponse


from fastapi.responses import JSONResponse

@app.post("/voice-chat")
async def transcribe_and_chat(file: UploadFile = File(...)):
    try:
        # --- Step 1: Read uploaded audio ---
        audio_bytes = await file.read()
        audio_buffer = io.BytesIO(audio_bytes)
        audio, sr = sf.read(audio_buffer)
        audio = np.array(audio, dtype=np.float32)
        if audio.ndim > 1:
            audio = np.mean(audio, axis=1)
        if sr != 16000:
            audio = librosa.resample(audio, orig_sr=sr, target_sr=16000)

        # --- Step 2: Speech-to-Text ---
        stt_result = stt_model.transcribe(audio, fp16=False)
        clean_text = stt_result["text"].strip()

        # --- Step 3: Chatbot ---
        sentence = tokenize(clean_text)
        X = bag_of_words(sentence, all_words)
        X = X.reshape(1, X.shape[0])
        X = torch.from_numpy(X).to(device_whisper)

        output = chat_model(X)
        _, predicted = torch.max(output, dim=1)
        tag = tags[predicted.item()]

        probs = torch.softmax(output, dim=1)
        prob = probs[0][predicted.item()]

        # Fast O(1) intent lookup
        intent = intent_lookup.get(tag)
        if intent and intent.get("responses"):
            if prob.item() > 0.90:
                response = random.choice(intent["responses"])
            else:
                # Fallback to unknown or clarification
                unknown_intent = intent_lookup.get("unknown")
                response = random.choice(unknown_intent["responses"]) if unknown_intent else "I'm sorry, I didn't catch that."
        else:
            response = "I'm sorry, I didn't catch that."

        # Optional: log for debugging
        # print(f"User said: {clean_text} | Tag: {tag} | Prob: {prob.item():.2f}")

        return JSONResponse(content={
            "tag": tag,
            "response": response
        })

    except Exception as e:
        return JSONResponse(content={"error": str(e)})


# === Get All Intents ===
@app.get("/intents/")
def get_intents():
    return intents

# === Add New Intent ===
@app.post("/intents/")
def add_intent(new_intent: dict):
    intents["intents"].append(new_intent)
    with open("intents.json", "w") as f:
        json.dump(intents, f, indent=2)
    return {"message": "Intent added successfully."}

# === Post AR Command (From Admin App) ===
@app.post("/post-ar-command")
def post_ar_command(command: dict):
    ar_command_queue.append(command)
    return {"message": "AR command queued successfully."}

# === Get AR Commands (Unity polls this) ===
@app.get("/get-ar-commands")
def get_ar_commands():
    return {"commands": ar_command_queue}

# === Clear AR Command Queue ===
@app.post("/clear-ar-commands")
def clear_ar_commands():
    ar_command_queue.clear()
    return {"message": "AR command queue cleared."}

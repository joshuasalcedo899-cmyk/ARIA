# Use a lightweight Python image
FROM python:3.9-slim

# Set the working directory
WORKDIR /app

# Install system dependencies for audio processing
RUN apt-get update && apt-get install -y libsndfile1 ffmpeg

# Create a Hugging Face model cache directory (Fixes PermissionError)
RUN mkdir -p /app/hf_cache
ENV TRANSFORMERS_CACHE="/app/hf_cache"

# Copy all files to the container
COPY . .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Expose API port
EXPOSE 7860

# Start FastAPI server
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "7860"]

import requests
import json
import time

# 🔧 CONFIGURE THESE
DUCKDNS_DOMAIN = "aria-domain.duckdns.org"  # just the subdomain, not full URL
DUCKDNS_TOKEN = "61d905be-b488-4b6b-b4c5-6187c951e998"

def get_ngrok_url():
    try:
        response = requests.get("http://127.0.0.1:4040/api/tunnels")
        tunnels = response.json()["tunnels"]
        for t in tunnels:
            if t["public_url"].startswith("https"):
                return t["public_url"]
    except Exception as e:
        print("Error getting ngrok URL:", e)
    return None

def update_duckdns(url):
    update_url = f"https://www.duckdns.org/update?domains={DUCKDNS_DOMAIN}&token={DUCKDNS_TOKEN}&txt={url}&verbose=true"
    r = requests.get(update_url)
    print("DuckDNS update:", r.text)

if __name__ == "__main__":
    while True:
        ngrok_url = get_ngrok_url()
        if ngrok_url:
            print("Current ngrok URL:", ngrok_url)
            update_duckdns(ngrok_url)
        else:
            print("Waiting for ngrok to start...")
        time.sleep(60)  # check every 60 seconds

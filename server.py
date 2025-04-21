# myserver.py
from flask import Flask, request, jsonify
from NLP import process_input, hf_login

app = Flask(__name__)

@app.route("/echo", methods=["POST"])
def echo():
    data = request.get_json()
    return jsonify(data)

@app.route("/add", methods=["POST"])
def add():
    data = request.get_json()
    result = data["a"] + data["b"]
    return jsonify({"result": result})

@app.route("/analyze", methods=["POST"])
def analyze():
    data = request.get_json()
    input_text = data.get("text", "")
    result = process_input(input_text)
    return jsonify({"result": result})

@app.route("/login", methods=["POST"])
def login():
    data = request.get_json()
    input_text = data.get("text", "")
    result = hf_login(input_text)
    return jsonify({"result": result})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5005)
